using System;
using System.IO;
using System.Security.Cryptography;
using Game.Simulation;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;

namespace CitiesIIAgentBridge
{
    public sealed partial class Mod
    {
        private static JObject CaptureGeometryTerrain(TerrainHeightData data)
        {
            Action<Entity> ignore = e => { };
            return new JObject {
                ["storage"] = "TerrainHeightData-ushort-arrays-v1",
                ["heights"] = CaptureGeometryHeightArray(data.heights),
                ["downscaledHeights"] = CaptureGeometryHeightArray(data.downscaledHeights),
                ["resolution"] = CaptureGeometryValue(data.resolution, ignore, 0),
                ["downScaledResolution"] = CaptureGeometryValue(data.downScaledResolution, ignore, 0),
                ["scale"] = CaptureGeometryValue(data.scale, ignore, 0),
                ["offset"] = CaptureGeometryValue(data.offset, ignore, 0),
                ["hasBackdrop"] = data.hasBackdrop,
                ["sampling"] = "Original TerrainUtils.SampleHeight; full arrays, no radius approximation"
            };
        }

        private static JObject CaptureGeometryHeightArray(NativeArray<ushort> values)
        {
            if (!values.IsCreated) return new JObject { ["created"] = false, ["length"] = 0 };
            if (values.Length > 67108864) throw new InvalidOperationException("terrain_array_limit_67108864");
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CitiesIIAgentBridge", "geometry-research", "terrain");
            Directory.CreateDirectory(directory);
            string pending = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".pending");
            // Fixed managed chunk: no second full-array allocation and no native disposal.
            var buffer = new byte[65536];
            using (var stream = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
                int used = 0;
                for (int i = 0; i < values.Length; i++) {
                    ushort sample = values[i];
                    buffer[used++] = (byte)sample; buffer[used++] = (byte)(sample >> 8);
                    if (used == buffer.Length) { stream.Write(buffer, 0, used); used = 0; }
                }
                if (used != 0) stream.Write(buffer, 0, used);
            }
            string checksum;
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(pending)) checksum = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
            string destination = Path.Combine(directory, checksum + ".u16le");
            if (File.Exists(destination)) {
                using (var sha = SHA256.Create())
                using (var stream = File.OpenRead(destination))
                    if (BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "") != checksum)
                        throw new InvalidOperationException("terrain_archive_hash_conflict");
                File.Delete(pending); // Only the unique file this call just created.
            } else File.Move(pending, destination);
            return new JObject { ["created"] = true, ["length"] = values.Length,
                ["encoding"] = "uint16-little-endian", ["sha256"] = checksum, ["path"] = destination };
        }
    }
}
