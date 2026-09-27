using System;
using System.Collections.Generic;
using Game.Common;
using Game.Net;
using PathNode = Game.Pathfind.PathNode;
using Game.Simulation;
using Game.Tools;
using Newtonsoft.Json.Linq;
using Unity.Entities;

namespace CitiesIIAgentBridge
{
    public sealed partial class Mod
    {
        // A bounded observation of native data. Never pauses, schedules jobs, or edits entities.
        private JObject JunctionSnapshot(JObject args)
        {
            var world = RequireCity();
            var simulation = world.GetExistingSystemManaged<SimulationSystem>();
            if (simulation == null || simulation.selectedSpeed != 0)
                throw new InvalidOperationException("pause_game_before_junction_snapshot");
            var em = world.EntityManager;
            var node = new Entity { Index = RequiredInt(args, "index"), Version = RequiredInt(args, "version") };
            if (!JunctionLive(em, node) || !em.HasComponent<Node>(node))
                throw new ArgumentException("live_network_node_required");
            var errors = new JArray();
            var owners = new JArray();
            var lanes = new JArray();
            var identities = new List<PathNode>();
            var visitedLanes = new HashSet<Entity>();
            var visitedOwners = new HashSet<Entity>();
            var queue = new List<Entity> { node };
            var incident = new JArray();
            if (!em.HasBuffer<ConnectedEdge>(node)) errors.Add("missing_connected_edge_buffer");
            else
            {
                var buffer = em.GetBuffer<ConnectedEdge>(node, true);
                if (buffer.Length > 64) errors.Add("incident_edge_limit_64");
                for (int i = 0; i < buffer.Length && i < 64; i++)
                {
                    var edge = buffer[i].m_Edge;
                    incident.Add(NativeBuild.Id(edge));
                    queue.Add(edge);
                }
            }
            foreach (var owner in queue)
            {
                if (!visitedOwners.Add(owner)) continue;
                var row = NativeBuild.Id(owner);
                owners.Add(row);
                row["live"] = JunctionLive(em, owner);
                if (!JunctionLive(em, owner)) { errors.Add("unavailable_owner:" + owner.Index + ":" + owner.Version); continue; }
                row["updated"] = em.HasComponent<Updated>(owner);
                row["created"] = em.HasComponent<Created>(owner);
                if (em.HasComponent<Node>(owner)) row["position"] = Vector(em.GetComponentData<Node>(owner).m_Position);
                if (em.HasComponent<Edge>(owner))
                {
                    var edge = em.GetComponentData<Edge>(owner);
                    row["startNode"] = JunctionEndpoint(em, edge.m_Start);
                    row["endNode"] = JunctionEndpoint(em, edge.m_End);
                    if (!JunctionLive(em, edge.m_Start) || !em.HasComponent<Node>(edge.m_Start)
                        || !JunctionLive(em, edge.m_End) || !em.HasComponent<Node>(edge.m_End)) errors.Add("unavailable_edge_endpoint:" + owner.Index);
                    if (edge.m_Start != node && edge.m_End != node) errors.Add("incident_edge_not_attached:" + owner.Index);
                }
                else if (owner != node) errors.Add("missing_edge_component:" + owner.Index);
                row["curve"] = JunctionCurve(em, owner);
                if (owner != node && !em.HasComponent<Curve>(owner)) errors.Add("missing_edge_curve:" + owner.Index);
                var references = new JArray();
                row["subLanes"] = references;
                if (!em.HasBuffer<SubLane>(owner)) { errors.Add("missing_sublane_buffer:" + owner.Index); continue; }
                var sublanes = em.GetBuffer<SubLane>(owner, true);
                for (int i = 0; i < sublanes.Length; i++)
                {
                    if (lanes.Count >= 4096) { errors.Add("lane_limit_4096"); break; }
                    var sub = sublanes[i];
                    var entity = sub.m_SubLane;
                    references.Add(new JObject { ["entity"] = NativeBuild.Id(entity), ["pathMethods"] = sub.m_PathMethods.ToString() });
                    if (!visitedLanes.Add(entity)) continue;
                    var laneRow = NativeBuild.Id(entity);
                    lanes.Add(laneRow);
                    laneRow["live"] = JunctionLive(em, entity);
                    if (!JunctionLive(em, entity) || !em.HasComponent<Lane>(entity))
                    { errors.Add("unavailable_lane:" + entity.Index); continue; }
                    laneRow["updated"] = em.HasComponent<Updated>(entity);
                    laneRow["created"] = em.HasComponent<Created>(entity);
                    var lane = em.GetComponentData<Lane>(entity);
                    laneRow["start"] = JunctionPathNode(lane.m_StartNode, identities);
                    laneRow["middle"] = JunctionPathNode(lane.m_MiddleNode, identities);
                    laneRow["end"] = JunctionPathNode(lane.m_EndNode, identities);
                    laneRow["curve"] = JunctionCurve(em, entity);
                    if (!em.HasComponent<Curve>(entity)) errors.Add("missing_lane_curve:" + entity.Index);
                    if (em.HasComponent<Owner>(entity)) laneRow["owner"] = NativeBuild.Id(em.GetComponentData<Owner>(entity).m_Owner);
                    if (em.HasComponent<TrackLane>(entity))
                    {
                        var track = em.GetComponentData<TrackLane>(entity);
                        laneRow["track"] = new JObject { ["flags"] = track.m_Flags.ToString(), ["speedLimit"] = track.m_SpeedLimit,
                            ["curviness"] = track.m_Curviness, ["accessRestriction"] = NativeBuild.Id(track.m_AccessRestriction) };
                    }
                    if (em.HasComponent<CarLane>(entity))
                    {
                        var car = em.GetComponentData<CarLane>(entity);
                        laneRow["car"] = new JObject { ["flags"] = car.m_Flags.ToString(), ["speedLimit"] = car.m_SpeedLimit,
                            ["curviness"] = car.m_Curviness, ["accessRestriction"] = NativeBuild.Id(car.m_AccessRestriction) };
                    }
                }
            }
            return new JObject {
                ["schemaVersion"] = 1, ["snapshotId"] = Guid.NewGuid().ToString("N"), ["citySession"] = citySession,
                ["capturedUtc"] = DateTime.UtcNow.ToString("O"), ["simulationFrame"] = simulation.frameIndex,
                ["junction"] = NativeBuild.Id(node), ["incidentEdges"] = incident, ["owners"] = owners, ["lanes"] = lanes,
                ["complete"] = errors.Count == 0, ["errors"] = errors,
                ["meaning"] = "Native local lane data, not proof of vehicle routing or completed rebuild. Matching equalityId values mean PathNode.Equals within this snapshot only. Owner indices lack entity versions."
            };
        }
        private static bool JunctionLive(EntityManager em, Entity entity) => entity != Entity.Null && em.Exists(entity)
            && !em.HasComponent<Deleted>(entity) && !em.HasComponent<Temp>(entity);
        private static JObject JunctionEndpoint(EntityManager em, Entity entity)
        {
            var result = NativeBuild.Id(entity);
            result["live"] = JunctionLive(em, entity);
            if (JunctionLive(em, entity) && em.HasComponent<Node>(entity)) result["position"] = Vector(em.GetComponentData<Node>(entity).m_Position);
            return result;
        }
        private static JToken JunctionCurve(EntityManager em, Entity entity)
        {
            if (!em.HasComponent<Curve>(entity)) return JValue.CreateNull();
            var curve = em.GetComponentData<Curve>(entity).m_Bezier;
            return new JArray(Vector(curve.a), Vector(curve.b), Vector(curve.c), Vector(curve.d));
        }
        private static JObject JunctionPathNode(PathNode node, List<PathNode> identities)
        {
            int id = identities.IndexOf(node);
            if (id < 0) { id = identities.Count; identities.Add(node); }
            return new JObject { ["equalityId"] = id, ["ownerIndex"] = node.GetOwnerIndex(),
                ["laneIndex"] = node.GetLaneIndex(), ["curvePosition"] = node.GetCurvePos(), ["secondary"] = node.IsSecondary() };
        }
    }
}