using System;
using Game.Simulation;
using Newtonsoft.Json.Linq;
namespace CitiesIIAgentBridge {
    public sealed partial class Mod {
        private ProviderRegistry providers;
        private ProviderRegistry Providers() {
            if (providers == null) providers = new ProviderRegistry();
            providers.Discover(AppDomain.CurrentDomain.GetAssemblies());
            return providers;
        }
        private JObject ProviderCall(JObject args) {
            Action read = () => {
                var world = RequireCity();
                if (world.GetExistingSystemManaged<SimulationSystem>().selectedSpeed != 0)
                    throw new InvalidOperationException("pause_before_provider_call");
            };
            Action write = () => {
                read(); RequireControl();
                if (ConstructionAccess.Active != null || (string)batch?["status"] == "running")
                    throw new InvalidOperationException("another_operation_active");
            };
            return Providers().Invoke(args, new JObject { ["protocol"] = 1, ["citySession"] = citySession,
                ["bridgeSession"] = mailbox.Session }, read, write);
        }
    }
}
