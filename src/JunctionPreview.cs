using System;
using Game.Common;
using Game.Net;
using Game.Tools;
using Game.Simulation;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
namespace CitiesIIAgentBridge {
    public sealed partial class Mod {
        private static JToken JunctionTemp(EntityManager em, Entity entity) {
            if (!em.HasComponent<Temp>(entity)) return JValue.CreateNull();
            var temp = em.GetComponentData<Temp>(entity);
            return new JObject { ["original"] = NativeBuild.Id(temp.m_Original), ["flags"] = temp.m_Flags.ToString() };
        }
        private JObject JunctionPreviewEdges(EntityManager em, Entity original) {
            var rows = new JArray(); var errors = new JArray();
            var originals = new System.Collections.Generic.HashSet<Entity>();
            if (em.HasBuffer<ConnectedEdge>(original)) {
                var incident = em.GetBuffer<ConnectedEdge>(original, true);
                if (incident.Length > 64) errors.Add("incident_edge_limit_64");
                for (int i = 0; i < incident.Length && i < 64; i++) originals.Add(incident[i].m_Edge);
            } else errors.Add("missing_original_connected_edges");
            using (var query = em.CreateEntityQuery(new EntityQueryDesc {
                All = new[] { ComponentType.ReadOnly<Edge>(), ComponentType.ReadOnly<Temp>() },
                None = new[] { ComponentType.ReadOnly<Deleted>() }
            })) {
                if (query.CalculateEntityCount() > 4096) {
                    errors.Add("preview_edge_limit_4096");
                } else using (var entities = query.ToEntityArray(Allocator.Temp)) {
                    foreach (var entity in entities) {
                        var temp = em.GetComponentData<Temp>(entity);
                        var edge = em.GetComponentData<Edge>(entity);
                        bool start = JunctionPreviewEndpointMatches(em, edge.m_Start, original);
                        bool end = JunctionPreviewEndpointMatches(em, edge.m_End, original);
                        if (!originals.Contains(temp.m_Original) && !start && !end) continue;
                        var row = NativeBuild.Id(entity);
                        row["temp"] = JunctionTemp(em, entity);
                        row["startNode"] = JunctionEndpoint(em, edge.m_Start, true);
                        row["endNode"] = JunctionEndpoint(em, edge.m_End, true);
                        row["startTemp"] = JunctionLive(em, edge.m_Start, true) ? JunctionTemp(em, edge.m_Start) : JValue.CreateNull();
                        row["endTemp"] = JunctionLive(em, edge.m_End, true) ? JunctionTemp(em, edge.m_End) : JValue.CreateNull();
                        row["matchesOriginalEdge"] = originals.Contains(temp.m_Original);
                        row["startMatchesJunction"] = start; row["endMatchesJunction"] = end;
                        row["curve"] = JunctionCurve(em, entity);
                        row["updated"] = em.HasComponent<Updated>(entity);
                        JunctionInputs(em, entity, row, errors);
                        var lanes = new JArray(); row["subLanes"] = lanes;
                        if (!em.HasBuffer<SubLane>(entity)) errors.Add("missing_preview_sublanes:" + entity.Index);
                        else {
                            var buffer = em.GetBuffer<SubLane>(entity, true);
                            if (buffer.Length > 4096) errors.Add("preview_sublane_limit_4096");
                            for (int i = 0; i < buffer.Length && i < 4096; i++) lanes.Add(NativeBuild.Id(buffer[i].m_SubLane));
                        }
                        rows.Add(row);
                    }
                }
            }
            var expected = new JArray(); foreach (var edge in originals) expected.Add(NativeBuild.Id(edge));
            return new JObject { ["expectedOriginalEdges"] = expected, ["edges"] = rows, ["complete"] = errors.Count == 0, ["errors"] = errors };
        }
        private static bool JunctionPreviewEndpointMatches(EntityManager em, Entity endpoint, Entity original) {
            if (endpoint == original) return true;
            return JunctionLive(em, endpoint, true) && em.HasComponent<Temp>(endpoint)
                && em.GetComponentData<Temp>(endpoint).m_Original == original;
        }
        private static string JunctionIdentity(JToken id) => (int)id["index"] + ":" + (int)id["version"];
        private JObject JunctionPreview(JObject args) {
            var world = RequireCity();
            var simulation = world.GetExistingSystemManaged<SimulationSystem>();
            if (simulation == null || simulation.selectedSpeed != 0)
                throw new InvalidOperationException("pause_game_before_junction_snapshot");
            var em = world.EntityManager;
            var original = new Entity { Index = RequiredInt(args, "index"), Version = RequiredInt(args, "version") };
            if (!JunctionLive(em, original) || !em.HasComponent<Node>(original))
                throw new ArgumentException("live_original_node_required");
            var matches = new JArray();
            Entity candidate = Entity.Null;
            using (var query = em.CreateEntityQuery(new EntityQueryDesc {
                All = new[] { ComponentType.ReadOnly<Node>(), ComponentType.ReadOnly<Temp>() },
                None = new[] { ComponentType.ReadOnly<Deleted>() }
            })) {
                if (query.CalculateEntityCount() > 4096) throw new InvalidOperationException("preview_node_limit_4096");
                using (var nodes = query.ToEntityArray(Allocator.Temp)) {
                    foreach (var node in nodes) {
                        if (em.GetComponentData<Temp>(node).m_Original != original) continue;
                        matches.Add(NativeBuild.Id(node)); candidate = node;
                    }
                }
            }
            var result = new JObject { ["original"] = NativeBuild.Id(original), ["matches"] = matches,
                ["status"] = matches.Count == 1 ? "observed" : matches.Count == 0 ? "missing" : "ambiguous",
                ["validationReady"] = false,
                ["meaning"] = "Observation only: no tool revision association or completed-rebuild guarantee. Never use to authorize Apply." };
            if (matches.Count == 1) result["snapshot"] = JunctionSnapshot(NativeBuild.Id(candidate), true);
            result["relatedPreviewEdges"] = JunctionPreviewEdges(em, original);
            var related = (JObject)result["relatedPreviewEdges"];
            var expected = new System.Collections.Generic.List<string>();
            foreach (var id in (JArray)related["expectedOriginalEdges"]) expected.Add(JunctionIdentity(id));
            var rows = new System.Collections.Generic.List<string[]>();
            foreach (var edge in (JArray)related["edges"])
                rows.Add(new[] { JunctionIdentity(edge["temp"]["original"]), JunctionIdentity(edge["startNode"]), JunctionIdentity(edge["endNode"]) });
            var resolution = PreviewJunctionResolver.Resolve(expected.ToArray(), rows.ToArray(), (bool)related["complete"]);
            result["topologyResolution"] = new JObject { ["status"] = resolution.Status, ["candidates"] = new JArray(resolution.Candidates) };
            if (resolution.Status == "resolved") {
                var parts = resolution.Candidates[0].Split(':');
                var resolved = new Entity { Index = Int32.Parse(parts[0]), Version = Int32.Parse(parts[1]) };
                if (JunctionLive(em, resolved, true) && em.HasComponent<Temp>(resolved) && em.HasComponent<Node>(resolved))
                    result["connectedSnapshot"] = JunctionSnapshot(NativeBuild.Id(resolved), true);
                else result["topologyResolution"]["status"] = "unavailable";
            }
            return result;
        }
    }
}
