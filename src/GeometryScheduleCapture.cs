#if GEOMETRY_RESEARCH_SCHEDULE_TRACE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Game.Net;
using Game.Simulation;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace CitiesIIAgentBridge
{
    public sealed partial class Mod
    {
        private const string GeometryTraceOwner = "dan.networktools.offline-geometry-research";
        private const string GeometryTraceGameHash = "AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A";
        private static Harmony geometryTraceHarmony;
        private static string geometryTraceOperation;
        private static string geometryTraceCity;
        private static int geometryTraceRemaining;
        private static int geometryTracePass;
        private static int geometryTraceSequence;
        private static bool geometryTraceInsideUpdate;
        private static bool geometryTraceCycle;
        private static Func<bool> geometryTraceAllowed;
        private static JArray geometryTraceEvents = new JArray();
        private static string geometryTraceFault;

        private JObject BeginGeometryScheduleTrace(JObject args)
        {
            RequireControl();
            var world = RequireCity();
            var simulation = world.GetExistingSystemManaged<SimulationSystem>();
            if (simulation == null || simulation.selectedSpeed != 0) throw new InvalidOperationException("paused_city_required");
            if ((string)args["citySession"] != citySession) throw new InvalidOperationException("city_session_changed");
            if (Unity.Burst.BurstCompiler.IsEnabled) throw new InvalidOperationException("managed_geometry_launch_required");
            string operation = (string)args["operationId"];
            int passes = (int?)args["maxPasses"] ?? 1;
            if (string.IsNullOrWhiteSpace(operation) || operation.Length > 160 || passes < 1 || passes > 4)
                throw new ArgumentException("operation_id_and_maxPasses_1_4_required");
            if (geometryTraceRemaining > 0 || geometryTraceCycle) throw new InvalidOperationException("trace_already_armed");
            string checksum;
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(typeof(GeometrySystem).Assembly.Location))
                checksum = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
            if (checksum != GeometryTraceGameHash) throw new InvalidOperationException("game_hash_changed_revalidate_trace");
            if (geometryTraceHarmony == null) {
                var harmony = new Harmony(GeometryTraceOwner);
                try {
                    harmony.Patch(AccessTools.Method(typeof(GeometrySystem), "OnUpdate"),
                        prefix: new HarmonyMethod(typeof(Mod), nameof(GeometryTraceEnter)),
                        postfix: new HarmonyMethod(typeof(Mod), nameof(GeometryTraceExit)),
                        transpiler: new HarmonyMethod(typeof(Mod), nameof(GeometryTraceSchedules)));
                    geometryTraceHarmony = harmony;
                } catch { harmony.UnpatchAll(GeometryTraceOwner); throw; }
            }
            geometryTraceOperation = operation; geometryTraceCity = citySession;
            geometryTraceRemaining = passes; geometryTracePass = 0; geometryTraceSequence = 0;
            geometryTraceFault = null; geometryTraceEvents = new JArray();
            geometryTraceAllowed = () => !disposed && citySession == geometryTraceCity
                && ConstructionAccess.Allowed() && simulation.selectedSpeed == 0;
            return GeometryScheduleTraceStatus();
        }

        private static JObject GeometryScheduleTraceStatus() => new JObject {
            ["operationId"] = geometryTraceOperation, ["citySession"] = geometryTraceCity,
            ["patched"] = geometryTraceHarmony != null, ["remainingPasses"] = geometryTraceRemaining,
            ["passesStarted"] = geometryTracePass, ["insideCapturedPass"] = geometryTraceCycle,
            ["fault"] = geometryTraceFault, ["events"] = geometryTraceEvents.DeepClone(),
            ["gameSha256"] = GeometryTraceGameHash,
            ["instrumentation"] = "Original jobs and schedulers; explicit completion barriers before/after captured stages; managed launch"
        };

        private static JObject EndGeometryScheduleTrace()
        {
            if (geometryTraceInsideUpdate) throw new InvalidOperationException("cannot_unpatch_during_geometry_update");
            geometryTraceRemaining = 0;
            if (geometryTraceHarmony != null) geometryTraceHarmony.UnpatchAll(GeometryTraceOwner);
            geometryTraceHarmony = null;
            return GeometryScheduleTraceStatus();
        }

        private static void GeometryTraceEnter() { geometryTraceInsideUpdate = true; geometryTraceCycle = false; }
        private static void GeometryTraceExit()
        {
            if (geometryTraceCycle) geometryTraceEvents.Add(new JObject { ["pass"] = geometryTracePass, ["event"] = "native_update_scheduled" });
            geometryTraceCycle = false; geometryTraceInsideUpdate = false;
        }

        private static bool GeometryTraceWanted()
        {
            if (!geometryTraceInsideUpdate || geometryTraceFault != null) return false;
            if (geometryTraceAllowed == null || !geometryTraceAllowed()) { geometryTraceRemaining = 0; return false; }
            if (!geometryTraceCycle) {
                if (geometryTraceRemaining == 0) return false;
                geometryTraceCycle = true; geometryTraceRemaining--; geometryTracePass++;
            }
            return true;
        }

        private static IEnumerable<CodeInstruction> GeometryTraceSchedules(IEnumerable<CodeInstruction> instructions)
        {
            int replaced = 0;
            foreach (var instruction in instructions) {
                if (instruction.operand is MethodInfo method && method.IsGenericMethod) {
                    var types = method.GetGenericArguments();
                    string job = types[0].FullName;
                    if (method.DeclaringType == typeof(JobChunkExtensions) && method.Name == "ScheduleParallel"
                        && (job == "Game.Net.GeometrySystem+InitializeNodeGeometryJob" || job == "Game.Net.GeometrySystem+FlattenNodeGeometryJob")) {
                        instruction.operand = typeof(Mod).GetMethod(nameof(TraceScheduleGeometryChunk)).MakeGenericMethod(types);
                        replaced++;
                    } else if (method.DeclaringType == typeof(IJobParallelForDeferExtensions) && method.Name == "Schedule"
                        && types.Length == 2 && types[1] == typeof(Entity)
                        && (job == "Game.Net.GeometrySystem+CalculateEdgeGeometryJob" || job == "Game.Net.GeometrySystem+FinishEdgeGeometryJob"
                            || job == "Game.Net.GeometrySystem+CalculateNodeGeometryJob")) {
                        instruction.operand = typeof(Mod).GetMethod(nameof(TraceScheduleGeometryEdges)).MakeGenericMethod(types);
                        replaced++;
                    }
                }
                yield return instruction;
            }
            if (replaced != 5) throw new InvalidOperationException("geometry_schedule_call_sites_changed:" + replaced);
        }

        public static JobHandle TraceScheduleGeometryChunk<T>(T job, EntityQuery query, JobHandle dependency) where T : struct, IJobChunk
        {
            if (!GeometryTraceWanted()) return JobChunkExtensions.ScheduleParallel(job, query, dependency);
            dependency.Complete();
            // Query membership is observed once and retained through this data-only job.
            using (var chunks = query.ToArchetypeChunkArray(Allocator.Temp)) {
                CaptureGeometryChunkSchedule(job, chunks, "entry");
                var result = JobChunkExtensions.ScheduleParallel(job, query, dependency);
                result.Complete();
                CaptureGeometryChunkSchedule(job, chunks, "exit");
                return result;
            }
        }

        public static JobHandle TraceScheduleGeometryEdges<T, U>(T job, NativeList<U> list, int batch, JobHandle dependency)
            where T : struct, IJobParallelForDefer where U : unmanaged
        {
            if (!GeometryTraceWanted()) return IJobParallelForDeferExtensions.Schedule(job, list, batch, dependency);
            dependency.Complete();
            CaptureGeometryEdgeSchedule(job, list, "entry");
            var result = IJobParallelForDeferExtensions.Schedule(job, list, batch, dependency);
            result.Complete();
            CaptureGeometryEdgeSchedule(job, list, "exit");
            return result;
        }

        private static void CaptureGeometryChunkSchedule<T>(T job, NativeArray<ArchetypeChunk> chunks, string phase) where T : struct, IJobChunk
        {
            try {
                if (geometryTraceFault != null) return;
                var entityType = (EntityTypeHandle)typeof(T).GetField("m_EntityType").GetValue(job);
                if (chunks.Length > 128) throw new InvalidOperationException("chunk_capture_limit_128");
                for (int i = 0; i < chunks.Length; i++) {
                    var ids = chunks[i].GetNativeArray(entityType).ToArray();
                    var report = CaptureGeometryJobState(job, ids, chunks[i], phase, geometryTraceOperation);
                    report["chunkOrdinal"] = i; report["chunkCount"] = chunks.Length;
                    PublishGeometryScheduleCapture(report);
                }
            } catch (Exception error) { GeometryTraceCaptureFault(error); }
        }

        private static void CaptureGeometryEdgeSchedule<T, U>(T job, NativeList<U> list, string phase)
            where T : struct, IJobParallelForDefer where U : unmanaged
        {
            try {
                if (geometryTraceFault != null) return;
                if (typeof(U) != typeof(Entity)) throw new InvalidOperationException("entity_schedule_list_required");
                if (list.Length > 512) throw new InvalidOperationException("scheduled_edge_limit_512");
                object capturedJob = job;
                // Resolve deferred storage in the CAPTURE COPY only. The original job
                // still reaches Unity's original deferred scheduler unchanged.
                typeof(T).GetField("m_Entities").SetValue(capturedJob, list.AsArray());
                var ids = new Entity[list.Length];
                for (int i = 0; i < ids.Length; i++) ids[i] = (Entity)(object)list[i];
                var report = CaptureGeometryJobState(capturedJob, ids, null, phase, geometryTraceOperation);
                report["deferredCaptureArray"] = "Resolved from completed native schedule list in capture copy only";
                if (phase == "entry" && typeof(T).Name == "CalculateEdgeGeometryJob")
                    report["nativeOffsetProbe"] = CaptureGeometryOffsetProbe(capturedJob, ids);
                PublishGeometryScheduleCapture(report);
            } catch (Exception error) { GeometryTraceCaptureFault(error); }
        }

        private static JObject CaptureGeometryOffsetProbe(object job, Entity[] roots)
        {
            var method = job.GetType().GetMethod("CalculateOffsets", BindingFlags.Public | BindingFlags.Instance);
            if (method == null || roots.Length > 128) throw new InvalidOperationException("native_offset_probe_contract");
            var parameters = method.GetParameters(); var rows = new JArray();
            foreach (var entity in roots) {
                var args = new object[parameters.Length]; args[0] = entity;
                // Original helper reads the same completed world. It writes out-args,
                // not ECS components. This is an extra native computation, not a
                // claim that the original scheduled Execute has already run.
                method.Invoke(job, args);
                var outputs = new JObject();
                for (int i = 1; i < parameters.Length; i++) {
                    if (!parameters[i].IsOut) throw new InvalidOperationException("offset_parameter_contract_changed");
                    outputs[parameters[i].Name] = CaptureGeometryValue(args[i], e => { }, 0);
                }
                rows.Add(new JObject { ["entity"] = new JArray(entity.Index, entity.Version), ["outputs"] = outputs });
            }
            return new JObject { ["scope"] = "Original CalculateOffsets recomputed before scheduling, with completed dependencies; not worker locals",
                ["rows"] = rows, ["complete"] = true };
        }

        private static void PublishGeometryScheduleCapture(JObject report)
        {
            if (++geometryTraceSequence > 128) throw new InvalidOperationException("capture_file_limit_128");
            report["sequence"] = geometryTraceSequence; report["pass"] = geometryTracePass;
            report["citySession"] = geometryTraceCity; report["frame"] = UnityEngine.Time.frameCount;
            report["boundary"] = "Completed native dependency/job handle at original scheduling site";
            report["instrumentation"] = "Native stage ordering retained; completion barriers and chunk read handles may affect timing/change versions";
            string path = SaveGeometryDiagnostic(report, geometryTraceOperation);
            geometryTraceEvents.Add(new JObject { ["sequence"] = geometryTraceSequence, ["pass"] = geometryTracePass,
                ["job"] = report["job"], ["phase"] = report["phase"], ["complete"] = report["complete"], ["path"] = path });
        }

        private static void GeometryTraceCaptureFault(Exception error)
        {
            geometryTraceFault = GeometryCaptureError(error); geometryTraceRemaining = 0;
            geometryTraceEvents.Add(new JObject { ["pass"] = geometryTracePass, ["event"] = "capture_failed_native_execution_continues", ["error"] = geometryTraceFault });
        }
    }
}
#endif
