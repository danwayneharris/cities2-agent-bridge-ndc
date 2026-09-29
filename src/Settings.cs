using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;

namespace CitiesIIAgentBridge
{
    [FileLocation("CitiesIIAgentBridge")]
    public sealed class BridgeSettings : ModSetting
    {
        public BridgeSettings(IMod mod) : base(mod) { SetDefaults(); }

        [SettingsUISection("Main", "Control")]
        public bool AllowControl { get; set; }

        [SettingsUISection("Main", "Control")]
        public bool RememberControl { get; set; }

        public override void SetDefaults() { AllowControl = false; RememberControl = false; }
    }

    public sealed class LocaleEN : IDictionarySource
    {
        private readonly BridgeSettings settings;
        public LocaleEN(BridgeSettings settings) { this.settings = settings; }
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { settings.GetSettingsLocaleID(), "Cities II Agent Bridge" },
                { settings.GetOptionTabLocaleID("Main"), "Bridge" },
                { settings.GetOptionGroupLocaleID("Control"), "Local control" },
                { settings.GetOptionLabelLocaleID(nameof(BridgeSettings.RememberControl)), "Remember local bridge controls between loads and restarts" },
                { settings.GetOptionDescLocaleID(nameof(BridgeSettings.RememberControl)), "Opt in to restoring your Allow local bridge controls setting in every city, including existing saves. Default off. Turning Allow off is saved too. STOP disables controls and saves them off; removing STOP alone does not enable them." },
                { settings.GetOptionLabelLocaleID(nameof(BridgeSettings.AllowControl)), "Allow local bridge controls" },
                { settings.GetOptionDescLocaleID(nameof(BridgeSettings.AllowControl)), "Allow construction, demolition, zoning, tile purchases, budgets, taxes, saves, and camera/simulation commands. Analysis commands pause the city. Bounded simulation steps pause when they finish or time out. Construction uses native placement checks and spending limits. Off after loading a city unless remembering controls is enabled. STOP always overrides permission. With controls off, pause the city manually before inspection." }
            };
        }
        public void Unload() { }
    }
}


