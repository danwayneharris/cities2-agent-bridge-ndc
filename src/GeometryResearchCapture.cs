using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Simulation;
using Newtonsoft.Json.Linq;
using Unity.Entities;

namespace CitiesIIAgentBridge
{
    public sealed partial class Mod
    {
        // A diagnostic dependency closure, not a native job-boundary snapshot.
        private JObject GeometryResearchCapture(JObject args)
        {
            var world = RequireCity();
            var simulation = world.GetExistingSystemManaged<SimulationSystem>();
            if (simulation == null || simulation.selectedSpeed != 0)
                throw new InvalidOperationException("pause_game_before_geometry_capture");
            var roots = args["roots"] as JArray;
            if (roots == null || roots.Count == 0 || roots.Count > 128)
                throw new ArgumentException("roots_required_max_128");
            int limit = (int?)args["maxEntities"] ?? 2048;
            if (limit < 1 || limit > 8192) throw new ArgumentException("maxEntities_range_1_8192");
            var em = world.EntityManager;
            var queue = new Queue<Entity>();
            var seen = new HashSet<Entity>();
            Action<Entity> enqueue = e => { if (e != Entity.Null && seen.Add(e)) queue.Enqueue(e); };
            foreach (JObject root in roots) {
                var entity = new Entity { Index = RequiredInt(root, "index"), Version = RequiredInt(root, "version") };
                if (!em.Exists(entity)) throw new ArgumentException("stale_root:" + entity);
                enqueue(entity);
            }
            var names = GeometryCaptureTypes;
            var types = names.Select(n => typeof(Game.Net.Node).Assembly.GetType(n)).ToArray();
            var errors = new JArray(); var records = new JArray(); var schemas = new JObject();
            for (int i = 0; i < names.Length; i++) {
                if (types[i] == null) { errors.Add("type_not_resolved:" + names[i]); continue; }
                schemas[names[i]] = new JArray(types[i].GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .OrderBy(f => f.Name).Select(f => new JObject { ["name"] = f.Name, ["type"] = f.FieldType.FullName }));
            }
            int frame = UnityEngine.Time.frameCount;
            string started = DateTime.UtcNow.ToString("O");
            // Drain existing jobs once; this main-thread command schedules no new jobs.
            em.CompleteAllTrackedJobs();
            var timer = System.Diagnostics.Stopwatch.StartNew();
            while (queue.Count > 0 && records.Count < limit) {
                if (timer.Elapsed.TotalSeconds > 15) { errors.Add("capture_time_limit_15_seconds"); break; }
                var entity = queue.Dequeue();
                var row = new JObject { ["id"] = new JArray(entity.Index, entity.Version), ["exists"] = em.Exists(entity) };
                records.Add(row);
                if (!em.Exists(entity)) { errors.Add("unavailable_reference:" + entity); continue; }
                var cells = new JObject(); row["components"] = cells;
                for (int i = 0; i < names.Length; i++) {
                    var type = types[i];
                    if (type == null) continue;
                    try {
                        if (!em.HasComponent(entity, ComponentType.ReadOnly(type))) {
                            cells[names[i]] = new JObject { ["presence"] = "absent" }; continue;
                        }
                        bool buffer = typeof(IBufferElementData).IsAssignableFrom(type);
                        var method = typeof(Mod).GetMethod(buffer ? nameof(CaptureGeometryBuffer) : nameof(CaptureGeometryComponent),
                            BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(type);
                        cells[names[i]] = new JObject { ["presence"] = "present", ["buffer"] = buffer,
                            ["value"] = (JToken)method.Invoke(null, new object[] { em, entity, enqueue }) };
                    } catch (Exception error) {
                        var cause = error is TargetInvocationException && error.InnerException != null ? error.InnerException : error;
                        cells[names[i]] = new JObject { ["presence"] = "notCaptured", ["error"] = cause.GetType().Name + ": " + cause.Message };
                        errors.Add("uncaptured:" + entity + "/" + names[i]);
                    }
                }
            }
            if (queue.Count > 0 && records.Count >= limit) errors.Add("entity_limit_reached");
            return new JObject {
                ["schemaVersion"] = 1, ["kind"] = "geometry-research-raw-closure",
                ["snapshotId"] = Guid.NewGuid().ToString("N"), ["citySession"] = citySession,
                ["startedUtc"] = started, ["finishedUtc"] = DateTime.UtcNow.ToString("O"),
                ["frameStart"] = frame, ["frameEnd"] = UnityEngine.Time.frameCount,
                ["world"] = world.Name, ["synchronization"] = "CompleteAllTrackedJobs then synchronous main-thread reads; no native job-boundary claim",
                ["gameAssembly"] = typeof(Game.Net.Node).Assembly.FullName,
                ["gameModuleVersionId"] = typeof(Game.Net.Node).Module.ModuleVersionId.ToString(),
                ["roots"] = roots.DeepClone(), ["types"] = schemas, ["entities"] = records,
                ["frontier"] = new JArray(queue.Select(e => new JArray(e.Index, e.Version))),
                ["complete"] = errors.Count == 0, ["errors"] = errors,
                ["scope"] = "Closure of entity references in selected type fields only; excludes terrain arrays, native query membership, job-local scratch and unlisted component types"
            };
        }

        private static JToken CaptureGeometryComponent<T>(EntityManager em, Entity e, Action<Entity> enqueue)
            where T : unmanaged, IComponentData => CaptureGeometryValue(em.GetComponentData<T>(e), enqueue, 0);

        private static JToken CaptureGeometryBuffer<T>(EntityManager em, Entity e, Action<Entity> enqueue)
            where T : unmanaged, IBufferElementData
        {
            var buffer = em.GetBuffer<T>(e, true);
            if (buffer.Length > 16384) throw new InvalidOperationException("buffer_limit_16384");
            var result = new JArray();
            for (int i = 0; i < buffer.Length; i++) result.Add(CaptureGeometryValue(buffer[i], enqueue, 0));
            return result;
        }

        private static JToken CaptureGeometryValue(object value, Action<Entity> enqueue, int depth)
        {
            if (depth > 16) throw new InvalidOperationException("field_depth_limit");
            if (value is Entity entity) { enqueue(entity); return new JArray(entity.Index, entity.Version); }
            var type = value.GetType();
            if (type.IsEnum) return JToken.FromObject(Convert.ChangeType(value, Enum.GetUnderlyingType(type)));
            if (type.IsPrimitive) {
                if (value is float f && (float.IsNaN(f) || float.IsInfinity(f))) throw new InvalidOperationException("nonfinite_float");
                if (value is double d && (double.IsNaN(d) || double.IsInfinity(d))) throw new InvalidOperationException("nonfinite_double");
                return JToken.FromObject(value);
            }
            if (!type.IsValueType || type.IsPointer || type == typeof(IntPtr) || type == typeof(UIntPtr)
                || type.FullName.StartsWith("Unity.Collections.Native"))
                throw new InvalidOperationException("unsupported_value_type:" + type.FullName);
            var fields = new JObject();
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).OrderBy(f => f.Name))
                fields[field.Name] = CaptureGeometryValue(field.GetValue(value), enqueue, depth + 1);
            return fields;
        }

        private static readonly string[] GeometryCaptureTypes = {
            "Game.Net.Node", "Game.Net.Edge", "Game.Net.Curve", "Game.Net.Elevation",
            "Game.Net.NodeGeometry", "Game.Net.EdgeGeometry", "Game.Net.StartNodeGeometry", "Game.Net.EndNodeGeometry",
            "Game.Net.Composition", "Game.Net.OutsideConnection", "Game.Net.ConnectedEdge", "Game.Net.SubNet",
            "Game.Objects.SubObject", "Game.Common.Owner", "Game.Common.Updated", "Game.Common.Created",
            "Game.Common.Deleted", "Game.Common.Hidden", "Game.Tools.Temp", "Game.Prefabs.PrefabRef",
            "Game.Prefabs.NetGeometryData", "Game.Prefabs.NetCompositionData", "Game.Prefabs.PlaceableNetData",
            "Game.Prefabs.PlaceableObjectData", "Game.Prefabs.ObjectGeometryData", "Game.Prefabs.NetLaneData",
            "Game.Prefabs.NetCompositionLane", "Game.Prefabs.NetCompositionCrosswalk", "Game.Prefabs.NetCompositionPiece"
        };
    }
}
