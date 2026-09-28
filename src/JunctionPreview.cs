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
            return result;
        }
    }
}
