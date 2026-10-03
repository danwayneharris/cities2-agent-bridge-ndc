#if GEOMETRY_RESEARCH_SCHEDULE_TRACE
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Mathematics;

namespace CitiesIIAgentBridge
{
    public sealed partial class Mod
    {
        // Called only at completed scheduling boundaries while borrowed map storage
        // is alive. Copy logical bucket links; do not serialize pointer addresses.
        private static unsafe JObject CaptureGeometryHeightMap(NativeParallelHashMap<int2, float4> map)
        {
            if (!map.IsCreated || map.Capacity < 0 || map.Capacity > 16384)
                throw new InvalidOperationException("height_map_capacity_limit");
            var data = map.GetUnsafeBucketData();
            int mask = data.bucketCapacityMask;
            if (mask < 0 || mask >= 32768 || ((mask + 1) & mask) != 0)
                throw new InvalidOperationException("height_map_bucket_mask_invalid");
            var heads = new JArray(); var slots = new JArray(); var seen = new HashSet<int>();
            var buckets = (int*)data.buckets; var next = (int*)data.next;
            var keys = (int2*)data.keys; var values = (float4*)data.values;
            for (int bucket = 0; bucket <= mask; bucket++) {
                heads.Add(buckets[bucket]);
                int index = buckets[bucket]; int steps = 0;
                while (index != -1) {
                    if (index < 0 || index >= map.Capacity || ++steps > map.Capacity)
                        throw new InvalidOperationException("height_map_chain_invalid");
                    if (seen.Add(index)) {
                        int2 key = keys[index]; float4 value = values[index];
                        bool found = map.TryGetValue(key, out var lookup);
                        slots.Add(new JObject {
                            ["index"] = index, ["key"] = new JArray(key.x, key.y),
                            ["value"] = CaptureGeometryValue(value, e => { }, 0), ["next"] = next[index],
                            ["managedHash"] = key.GetHashCode(), ["managedLookupFound"] = found,
                            ["managedLookupValue"] = found ? CaptureGeometryValue(lookup, e => { }, 0) : null
                        });
                    }
                    index = next[index];
                }
            }
            var pairs = new JArray();
            foreach (var pair in map) pairs.Add(new JObject {
                ["key"] = new JArray(pair.Key.x, pair.Key.y),
                ["value"] = new JArray(pair.Value.x, pair.Value.y, pair.Value.z, pair.Value.w)
            });
            return new JObject {
                ["storage"] = "NativeParallelHashMap<int2,float4>", ["entries"] = pairs,
                ["lookupStorageVersion"] = 1, ["capacity"] = map.Capacity,
                ["bucketCapacityMask"] = mask, ["bucketHeads"] = heads, ["slots"] = slots,
                ["lookupProbeScope"] = "Managed TryGetValue on completed native map; not Burst worker locals"
            };
        }
    }
}
#endif
