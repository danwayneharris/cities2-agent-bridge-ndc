using Game;
using Game.Modding;

namespace HelloBridgeExample {
    // This is a separate mod assembly, not part of the bridge.
    public sealed class Mod : IMod {
        public void OnLoad(UpdateSystem updateSystem) {
            CitiesBridge.ProviderV1.Loaded = true;
        }

        public void OnDispose() {
            CitiesBridge.ProviderV1.Loaded = false;
        }
    }
}
