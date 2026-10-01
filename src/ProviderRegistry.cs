using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CitiesIIAgentBridge {
    // Pure reflection/JSON boundary. No game or third-party-mod assembly references.
    public sealed class ProviderRegistry {
        public const string EndpointType = "CitiesBridge.ProviderV1";
        private sealed class Entry {
            public JObject Description;
            public MethodInfo Invoke;
            public string Revision;
        }
        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        private readonly HashSet<string> duplicates = new HashSet<string>(StringComparer.Ordinal);
        private readonly JArray errors = new JArray();
        private readonly HashSet<Assembly> seen = new HashSet<Assembly>();
        private static bool Name(string value) => value != null && Regex.IsMatch(value, "^[a-z][a-z0-9_.-]{0,63}$");
        public void Discover(IEnumerable<Assembly> assemblies) {
            foreach (var assembly in assemblies) {
                if (seen.Contains(assembly)) continue;
                seen.Add(assembly);
                try {
                    var type = assembly.GetType(EndpointType, false);
                    if (type != null) Register(type);
                } catch (Exception e) {
                    errors.Add(new JObject { ["assembly"] = assembly.GetName().Name, ["error"] = e.GetBaseException().Message });
                }
            }
        }
        // Public for isolated contract tests. Runtime only supplies the opt-in endpoint type.
        public void Register(Type type) {
            if (entries.Count >= 64) throw new InvalidOperationException("provider_limit");
            var describe = type.GetMethod("DescribeV1", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            var invoke = type.GetMethod("InvokeV1", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(string), typeof(string), typeof(string) }, null);
            if (describe?.ReturnType != typeof(string) || invoke?.ReturnType != typeof(string))
                throw new InvalidOperationException("invalid_provider_signature");
            string text = (string)describe.Invoke(null, null);
            if (text == null || text.Length > 65536) throw new InvalidOperationException("provider_descriptor_limit");
            var d = JObject.Parse(text);
            string id = (string)d["id"];
            if (!Name(id) || d["protocol"]?.Type != JTokenType.Integer || (int)d["protocol"] != 1
                || d["version"]?.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)d["version"]))
                throw new InvalidOperationException("invalid_provider_descriptor");
            if (!(d["commands"] is JArray commands) || commands.Count == 0 || commands.Count > 128)
                throw new InvalidOperationException("invalid_provider_commands");
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var token in commands) {
                if (!(token is JObject c) || !Name((string)c["name"]) || !names.Add((string)c["name"])
                    || c["description"]?.Type != JTokenType.String
                    || c["readOnly"]?.Type != JTokenType.Boolean
                    || !(c["inputSchema"] is JObject input) || (string)input["type"] != "object"
                    || !(c["outputSchema"] is JObject output) || (string)output["type"] != "object")
                    throw new InvalidOperationException("invalid_provider_command");
            }
            if (entries.ContainsKey(id) || duplicates.Contains(id)) {
                entries.Remove(id); duplicates.Add(id);
                errors.Add(new JObject { ["provider"] = id, ["error"] = "duplicate_provider_id" });
                return;
            }
            string revision;
            using (var hash = SHA256.Create()) revision = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(d.ToString(Formatting.None)))).Replace("-", "").ToLowerInvariant();
            entries.Add(id, new Entry { Description = d, Invoke = invoke, Revision = revision });
        }
        public JObject List() {
            var providers = new JArray();
            foreach (var pair in entries.OrderBy(p => p.Key, StringComparer.Ordinal)) {
                var d = (JObject)pair.Value.Description.DeepClone();
                d["revision"] = pair.Value.Revision;
                d["availability"] = "discovered_not_runtime_verified";
                providers.Add(d);
            }
            return new JObject { ["protocol"] = 1, ["providers"] = providers, ["complete"] = errors.Count == 0, ["errors"] = errors.DeepClone() };
        }
        public JObject Invoke(JObject envelope, JObject context, Action requireRead, Action requireWrite) {
            string id = (string)envelope["provider"], name = (string)envelope["command"];
            if (id == null || !entries.TryGetValue(id, out var entry)) throw new InvalidOperationException("provider_unavailable");
            if ((string)envelope["revision"] != entry.Revision) throw new InvalidOperationException("stale_provider_revision");
            var command = ((JArray)entry.Description["commands"]).OfType<JObject>().SingleOrDefault(c => (string)c["name"] == name);
            if (command == null) throw new InvalidOperationException("provider_command_unavailable");
            if (!(envelope["args"] is JObject args)) throw new ArgumentException("provider_args_object_required");
            if ((bool)command["readOnly"]) requireRead(); else requireWrite();
            // Descriptor hints do not replace the provider's authoritative input/domain validation.
            try {
                string result = (string)entry.Invoke.Invoke(null, new object[] { name, args.ToString(Formatting.None), context.ToString(Formatting.None) });
                if (result == null || result.Length > 1048576) throw new InvalidOperationException("provider_result_limit");
                return JObject.Parse(result);
            } catch (TargetInvocationException e) {
                throw new InvalidOperationException("provider_failed: " + e.GetBaseException().Message);
            }
        }
    }
}
