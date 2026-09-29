using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
namespace CitiesIIAgentBridge {
    public sealed partial class Mod {
        private JObject NetworkToolsCommand(string action, JObject args) {
            RequireCity();
            if (action != "state") {
                RequireControl();
                if (ConstructionAccess.Active != null || (string)batch?["status"] == "running")
                    throw new InvalidOperationException("another_operation_active");
            }
            var assembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(a => a.GetName().Name == "NetworkTools");
            var type = assembly?.GetType("NetworkTools.Automation.BridgeApi", false);
            var method = type?.GetMethod("InvokeV1", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(string), typeof(string) }, null);
            if (method == null || method.ReturnType != typeof(string))
                throw new InvalidOperationException("networktools_debug_adapter_v1_unavailable");
            try { return JObject.Parse((string)method.Invoke(null, new object[] { action, args.ToString(Newtonsoft.Json.Formatting.None) })); }
            catch (TargetInvocationException e) { throw new InvalidOperationException(e.InnerException?.Message ?? "networktools_adapter_failed"); }
        }
    }
}
