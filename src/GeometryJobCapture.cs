using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CitiesIIAgentBridge
{
    public sealed partial class Mod
    {
        // DEBUGGER-ONLY: call in a valid managed GeometrySystem job frame while ALL
        // other workers are suspended. Never calls EntityManager or completes jobs.
        // File output is unique and all native handles die with this synchronous call.
        public static string CaptureGeometryEntityJob(object job, int index, string phase, string operationId)
        {
            ValidateGeometryJob(job, phase);
            var field = job.GetType().GetField("m_Entities");
            if (field == null) throw new ArgumentException("entity_job_required");
            var entities = (NativeArray<Entity>)field.GetValue(job);
            if (!entities.IsCreated || index < 0 || index >= entities.Length) throw new ArgumentException("invalid_job_index");
            var report = CaptureGeometryJobState(job, new[] { entities[index] }, null, phase, operationId);
            report["executeIndex"] = index;
            return SaveGeometryDiagnostic(report, operationId);
        }

        public static string CaptureGeometryChunkJob(object job, ArchetypeChunk chunk, string phase, string operationId)
        {
            ValidateGeometryJob(job, phase);
            var field = job.GetType().GetField("m_EntityType");
            if (field == null) throw new ArgumentException("chunk_job_required");
            var entities = chunk.GetNativeArray((EntityTypeHandle)field.GetValue(job));
            var roots = new Entity[entities.Length];
            for (int i = 0; i < entities.Length; i++) roots[i] = entities[i];
            var report = CaptureGeometryJobState(job, roots, chunk, phase, operationId);
            return SaveGeometryDiagnostic(report, operationId);
        }

        public static string CaptureGeometryLocal(object value, string label, string operationId)
        {
            if (value == null) throw new ArgumentException("local_value_required");
            var report = GeometryDiagnosticEnvelope("native-job-local", operationId);
            report["label"] = label;
            report["type"] = value.GetType().FullName;
            try {
                report["value"] = CaptureGeometryContainer(value, e => { });
                report["complete"] = true;
            } catch (Exception error) {
                report["complete"] = false; report["error"] = GeometryCaptureError(error);
            }
            return SaveGeometryDiagnostic(report, operationId);
        }

        public static string CaptureGeometryLocals(System.Collections.IDictionary locals, string operationId)
        {
            if (locals == null || locals.Count == 0 || locals.Count > 128) throw new ArgumentException("locals_required_max_128");
            var report = GeometryDiagnosticEnvelope("native-job-locals", operationId);
            var values = new JObject(); var errors = new JArray();
            foreach (System.Collections.DictionaryEntry entry in locals) {
                if (!(entry.Key is string name) || string.IsNullOrWhiteSpace(name)) throw new ArgumentException("local_name_required");
                try {
                    if (entry.Value == null) throw new ArgumentException("null_local_value");
                    values[name] = new JObject { ["type"] = entry.Value.GetType().FullName,
                        ["value"] = CaptureGeometryContainer(entry.Value, e => { }) };
                } catch (Exception error) {
                    values[name] = new JObject { ["presence"] = "notCaptured", ["error"] = GeometryCaptureError(error) };
                    errors.Add(name);
                }
            }
            report["locals"] = values; report["errors"] = errors; report["complete"] = errors.Count == 0;
            return SaveGeometryDiagnostic(report, operationId);
        }

        private static void ValidateGeometryJob(object job, string phase)
        {
            if (job == null || job.GetType().DeclaringType != typeof(Game.Net.GeometrySystem)
                || !new[] { "InitializeNodeGeometryJob", "CalculateEdgeGeometryJob", "FlattenNodeGeometryJob", "FinishEdgeGeometryJob" }.Contains(job.GetType().Name))
                throw new ArgumentException("unsupported_geometry_job");
            if (phase != "entry" && phase != "exit" && phase != "intermediate") throw new ArgumentException("explicit_phase_required");
        }

        private static JObject CaptureGeometryJobState(object job, Entity[] roots, ArchetypeChunk? chunk, string phase, string operationId)
        {
            var report = GeometryDiagnosticEnvelope("native-geometry-job", operationId);
            report["job"] = job.GetType().FullName; report["phase"] = phase;
            report["roots"] = new JArray(roots.Select(e => new JArray(e.Index, e.Version)));
            var queue = new Queue<Entity>(); var seen = new HashSet<Entity>();
            Action<Entity> enqueue = e => { if (e != Entity.Null && seen.Add(e)) queue.Enqueue(e); };
            foreach (var root in roots) enqueue(root);
            var errors = new JArray(); var jobFields = new JObject(); var rows = new JArray();
            report["fields"] = jobFields; report["entities"] = rows;
            var lookups = new List<FieldInfo>();
            foreach (var field in job.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                var type = field.FieldType;
                try {
                    if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(ComponentLookup<>)
                        || type.GetGenericTypeDefinition() == typeof(BufferLookup<>))) {
                        lookups.Add(field);
                        jobFields[field.Name] = new JObject { ["storage"] = type.GetGenericTypeDefinition().FullName,
                            ["component"] = type.GetGenericArguments()[0].FullName };
                    } else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ComponentTypeHandle<>)) {
                        if (!chunk.HasValue) throw new InvalidOperationException("chunk_input_required");
                        var method = typeof(Mod).GetMethod(nameof(CaptureGeometryChunkField), BindingFlags.NonPublic | BindingFlags.Static)
                            .MakeGenericMethod(type.GetGenericArguments());
                        jobFields[field.Name] = (JToken)method.Invoke(null, new object[] { chunk.Value, field.GetValue(job), enqueue });
                    } else if (type == typeof(EntityTypeHandle)) {
                        jobFields[field.Name] = new JObject { ["storage"] = "EntityTypeHandle", ["identities"] = report["roots"].DeepClone() };
                    } else {
                        // Actual entity-array query membership is retained without expanding
                        // every unrelated query entity into the local dependency closure.
                        Action<Entity> visit = field.Name == "m_Entities" ? e => { } : enqueue;
                        jobFields[field.Name] = CaptureGeometryContainer(field.GetValue(job), visit);
                    }
                } catch (Exception error) {
                    jobFields[field.Name] = new JObject { ["presence"] = "notCaptured", ["error"] = GeometryCaptureError(error) };
                    errors.Add("job_field:" + field.Name);
                }
            }
            var timer = System.Diagnostics.Stopwatch.StartNew();
            while (queue.Count > 0 && rows.Count < 2048 && timer.Elapsed.TotalSeconds < 15) {
                var entity = queue.Dequeue(); var cells = new JObject();
                rows.Add(new JObject { ["id"] = new JArray(entity.Index, entity.Version), ["components"] = cells });
                foreach (var field in lookups) {
                    var type = field.FieldType.GetGenericArguments()[0];
                    try {
                        string methodName = field.FieldType.GetGenericTypeDefinition() == typeof(ComponentLookup<>)
                            ? nameof(CaptureGeometryLookup) : nameof(CaptureGeometryBufferLookup);
                        var method = typeof(Mod).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(type);
                        var cell = (JObject)method.Invoke(null, new object[] { field.GetValue(job), entity, enqueue });
                        if ((string)cell["presence"] == "notCaptured") errors.Add("unavailable_entity:" + entity);
                        cells[type.FullName] = cell;
                    } catch (Exception error) {
                        cells[type.FullName] = new JObject { ["presence"] = "notCaptured", ["error"] = GeometryCaptureError(error) };
                        errors.Add("lookup:" + entity + "/" + type.FullName);
                    }
                }
            }
            if (queue.Count > 0) errors.Add("closure_limit_or_time_budget");
            report["frontier"] = new JArray(queue.Select(e => new JArray(e.Index, e.Version)));
            report["errors"] = errors; report["complete"] = errors.Count == 0;
            report["scope"] = "Existing job lookups, handles and explicit roots under debugger suspension; no job completion, no EntityManager, no scheduler reproduction";
            report["instrumentation"] = "Chunk array access through writable handles may mark change versions; stop/resume changes scheduling; caller must retain breakpoint/frame evidence";
            return report;
        }

        private static JObject CaptureGeometryLookup<T>(ComponentLookup<T> lookup, Entity e, Action<Entity> enqueue)
            where T : unmanaged, IComponentData
        {
            bool present = lookup.TryGetComponent(e, out T value, out bool exists);
            return new JObject { ["presence"] = !exists ? "notCaptured" : present ? "present" : "absent",
                ["exists"] = exists, ["value"] = present ? CaptureGeometryValue(value, enqueue, 0) : null };
        }

        private static JObject CaptureGeometryBufferLookup<T>(BufferLookup<T> lookup, Entity e, Action<Entity> enqueue)
            where T : unmanaged, IBufferElementData
        {
            bool present = lookup.TryGetBuffer(e, out DynamicBuffer<T> buffer, out bool exists);
            var row = new JObject { ["presence"] = !exists ? "notCaptured" : present ? "present" : "absent",
                ["exists"] = exists, ["buffer"] = true };
            if (present) {
                if (buffer.Length > 16384) throw new InvalidOperationException("buffer_limit");
                var values = new JArray();
                for (int i = 0; i < buffer.Length; i++) values.Add(CaptureGeometryValue(buffer[i], enqueue, 0));
                row["value"] = values;
            }
            return row;
        }

        private static JObject CaptureGeometryChunkField<T>(ArchetypeChunk chunk, ComponentTypeHandle<T> handle, Action<Entity> enqueue)
            where T : unmanaged, IComponentData
        {
            if (!chunk.Has(ref handle)) return new JObject { ["presence"] = "absent", ["component"] = typeof(T).FullName };
            var array = chunk.GetNativeArray(ref handle);
            return new JObject { ["presence"] = "present", ["component"] = typeof(T).FullName,
                ["values"] = CaptureGeometryArray(array, enqueue) };
        }

        private static JToken CaptureGeometryContainer(object value, Action<Entity> enqueue)
        {
            var type = value.GetType();
            if (value is Game.Simulation.TerrainHeightData terrain) return CaptureGeometryTerrain(terrain);
            if (value is NativeParallelHashMap<int2, float4> map) {
                if (!map.IsCreated) throw new InvalidOperationException("height_map_not_created");
                var pairs = new JArray();
                foreach (var pair in map) {
                    if (pairs.Count >= 16384) throw new InvalidOperationException("map_limit");
                    pairs.Add(new JObject { ["key"] = new JArray(pair.Key.x, pair.Key.y),
                        ["value"] = new JArray(pair.Value.x, pair.Value.y, pair.Value.z, pair.Value.w) });
                }
                return new JObject { ["storage"] = "NativeParallelHashMap<int2,float4>", ["entries"] = pairs };
            }
            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(NativeArray<>) || type.GetGenericTypeDefinition() == typeof(NativeList<>))) {
                string name = type.GetGenericTypeDefinition() == typeof(NativeArray<>) ? nameof(CaptureGeometryArray) : nameof(CaptureGeometryList);
                var method = typeof(Mod).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(type.GetGenericArguments());
                return (JToken)method.Invoke(null, new object[] { value, enqueue });
            }
            // ParallelWriter and other unsupported native pointers are rejected.
            return CaptureGeometryValue(value, enqueue, 0);
        }

        private static JArray CaptureGeometryArray<T>(NativeArray<T> array, Action<Entity> enqueue) where T : struct
        {
            if (!array.IsCreated || array.Length > 16384) throw new InvalidOperationException("array_uncreated_or_limit");
            var result = new JArray();
            for (int i = 0; i < array.Length; i++) result.Add(CaptureGeometryValue(array[i], enqueue, 0));
            return result;
        }

        private static JArray CaptureGeometryList<T>(NativeList<T> list, Action<Entity> enqueue) where T : unmanaged
        {
            if (!list.IsCreated || list.Length > 16384) throw new InvalidOperationException("list_uncreated_or_limit");
            var result = new JArray();
            for (int i = 0; i < list.Length; i++) result.Add(CaptureGeometryValue(list[i], enqueue, 0));
            return result;
        }

        private static string GeometryCaptureError(Exception error)
        {
            while (error is TargetInvocationException && error.InnerException != null) error = error.InnerException;
            return error.GetType().Name + ": " + error.Message;
        }

        private static JObject GeometryDiagnosticEnvelope(string kind, string operationId) => new JObject {
            ["schemaVersion"] = 1, ["kind"] = kind, ["snapshotId"] = Guid.NewGuid().ToString("N"),
            ["operationId"] = operationId, ["utc"] = DateTime.UtcNow.ToString("O"),
            ["managedThread"] = System.Threading.Thread.CurrentThread.ManagedThreadId,
            ["gameModuleVersionId"] = typeof(Game.Net.GeometrySystem).Module.ModuleVersionId.ToString()
        };

        private static string SaveGeometryDiagnostic(JObject report, string operationId)
        {
            if (string.IsNullOrWhiteSpace(operationId) || operationId.Length > 200) throw new ArgumentException("operation_id_required_max_200");
            string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CitiesIIAgentBridge", "geometry-research");
            Directory.CreateDirectory(root);
            string path = Path.Combine(root, (string)report["snapshotId"] + ".json");
            report["captureFinishedUtc"] = DateTime.UtcNow.ToString("O");
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream)) writer.Write(report.ToString());
            return path;
        }
    }
}
