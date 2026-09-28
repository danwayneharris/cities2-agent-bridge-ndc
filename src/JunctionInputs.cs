using Colossal.Mathematics;
using Game.Net;
using Game.Prefabs;
using Newtonsoft.Json.Linq;
using Unity.Entities;

namespace CitiesIIAgentBridge
{
    public sealed partial class Mod
    {
        private static JArray JunctionBezier(Bezier4x3 c) => new JArray(Vector(c.a), Vector(c.b), Vector(c.c), Vector(c.d));
        private static JObject JunctionSegment(Segment s) => new JObject {
            ["left"] = JunctionBezier(s.m_Left), ["right"] = JunctionBezier(s.m_Right),
            ["length"] = new JArray(s.m_Length.x, s.m_Length.y)
        };
        private static JToken JunctionPrefab(EntityManager em, Entity prefab, JArray errors)
        {
            var row = NativeBuild.Id(prefab);
            if (prefab == Entity.Null || !em.Exists(prefab)) { errors.Add("missing_prefab:" + prefab.Index); return row; }
            if (em.HasComponent<TrackLaneData>(prefab)) {
                var data = em.GetComponentData<TrackLaneData>(prefab);
                row["trackLaneData"] = new JObject {
                    ["maxCurviness"] = data.m_MaxCurviness, ["trackTypes"] = data.m_TrackTypes.ToString(),
                    ["fallbackPrefab"] = NativeBuild.Id(data.m_FallbackPrefab)
                };
            }
            if (em.HasComponent<NetLaneData>(prefab))
                row["netLaneFlags"] = em.GetComponentData<NetLaneData>(prefab).m_Flags.ToString();
            if (em.HasComponent<NetGeometryData>(prefab)) {
                var data = em.GetComponentData<NetGeometryData>(prefab);
                row["netGeometryData"] = new JObject { ["mergeLayers"] = data.m_MergeLayers.ToString(),
                    ["flags"] = data.m_Flags.ToString(), ["defaultWidth"] = data.m_DefaultWidth };
            }
            return row;
        }
        private static JToken JunctionComposition(EntityManager em, Entity entity, bool includeLanes, JArray errors)
        {
            var row = NativeBuild.Id(entity);
            if (!em.Exists(entity) || !em.HasComponent<NetCompositionData>(entity)) {
                errors.Add("missing_composition_data:" + entity.Index); return row;
            }
            var data = em.GetComponentData<NetCompositionData>(entity);
            row["width"] = data.m_Width;
            row["state"] = data.m_State.ToString();
            row["flags"] = new JObject { ["general"] = data.m_Flags.m_General.ToString(),
                ["left"] = data.m_Flags.m_Left.ToString(), ["right"] = data.m_Flags.m_Right.ToString() };
            if (!includeLanes) return row;
            var lanes = new JArray(); row["lanes"] = lanes;
            if (!em.HasBuffer<NetCompositionLane>(entity)) { errors.Add("missing_composition_lanes:" + entity.Index); return row; }
            var buffer = em.GetBuffer<NetCompositionLane>(entity, true);
            if (buffer.Length > 256) errors.Add("composition_lane_limit_256:" + entity.Index);
            for (int i = 0; i < buffer.Length && i < 256; i++) {
                var lane = buffer[i];
                var prefab = JunctionPrefab(em, lane.m_Lane, errors);
                if ((lane.m_Flags & LaneFlags.Track) != 0 && prefab["trackLaneData"] == null)
                    errors.Add("missing_composition_track_data:" + lane.m_Lane.Index);
                lanes.Add(new JObject { ["bufferIndex"] = i, ["index"] = lane.m_Index,
                    ["group"] = lane.m_Group, ["carriageway"] = lane.m_Carriageway,
                    ["position"] = Vector(lane.m_Position), ["flags"] = lane.m_Flags.ToString(), ["prefab"] = prefab });
            }
            return row;
        }
        private static void JunctionInputs(EntityManager em, Entity entity, JObject row, JArray errors)
        {
            if (em.HasComponent<PrefabRef>(entity))
                row["prefab"] = JunctionPrefab(em, em.GetComponentData<PrefabRef>(entity).m_Prefab, errors);
            else errors.Add("missing_prefab_ref:" + entity.Index);
            if (!em.HasComponent<Edge>(entity)) return;
            if (em.HasComponent<EdgeGeometry>(entity)) {
                var data = em.GetComponentData<EdgeGeometry>(entity);
                row["edgeGeometry"] = new JObject { ["start"] = JunctionSegment(data.m_Start), ["end"] = JunctionSegment(data.m_End) };
            } else errors.Add("missing_edge_geometry:" + entity.Index);
            if (em.HasComponent<Composition>(entity)) {
                var data = em.GetComponentData<Composition>(entity);
                row["composition"] = new JObject {
                    ["edge"] = JunctionComposition(em, data.m_Edge, true, errors),
                    ["startNode"] = JunctionComposition(em, data.m_StartNode, false, errors),
                    ["endNode"] = JunctionComposition(em, data.m_EndNode, false, errors)
                };
            } else errors.Add("missing_composition:" + entity.Index);
        }
    }
}