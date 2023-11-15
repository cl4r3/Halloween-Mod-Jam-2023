using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TricksAndTreats.ModEntry;

namespace TricksAndTreats
{
    public class ModConfig
    {
        public string ScoreCalcMethod { get; set; } = "minmult";
        public int CustomMinVal { get; set; } = 0;
        public float CustomMinMult { get; set; } = 1.5f;  
        public bool AllowTPing { get; set; } = true;
        public bool AllowEgging { get; set; } = true;
        public Dictionary<string, bool> SmallTricks = new Dictionary<string, bool>(){
            //{ "cobweb", true },
            { "paint", true },
            { "maze", true },
            { "mystery", true },
            { "nickname", true },
            { "steal", true },
        }; 
    }

    class ConfigMenu
    {
        private readonly IModHelper Helper;
        private readonly IManifest ModManifest;

        public ConfigMenu(IMod mod)
        {
            Helper = mod.Helper;
            ModManifest = mod.ModManifest;
        }

        public void RegisterTokens()
        {
            if (CP is null)
            {
                Log.Alert("Content Patcher is not installed - PPAF requires CP to run. Please install CP and restart your game.");
                return;
            }
            CP.RegisterToken(ModManifest, "AllowTPing", () => new[] { Config.AllowTPing.ToString() });
            CP.RegisterToken(ModManifest, "AllowEgging", () => new[] { Config.AllowEgging.ToString() });
        }

        public void RegisterGMCM()
        {
            if (GMCM == null)
                return;

            var i18n = Helper.Translation;
            GMCM.Register(ModManifest, () => Config = new ModConfig(), () => Helper.WriteConfig(Config));

            GMCM.AddSectionTitle(ModManifest,
                text: () => i18n.Get("config.bigpranks.name"),
                tooltip: () => i18n.Get("config.bigpranks.description"));
            GMCM.AddTextOption(mod: ModManifest,
                name: () => i18n.Get("config.scorecalcmethod.name"),
                tooltip: () => i18n.Get("config.scorecalcmethod.description"),
                getValue: () => Config.ScoreCalcMethod,
                setValue: (string value) => Config.ScoreCalcMethod = value,
                allowedValues: new string[] { "minval", "minmult", "none" });
            GMCM.AddNumberOption(mod: ModManifest,
                name: () => i18n.Get("config.customminval.name"),
                tooltip: () => i18n.Get("config.customminval.description"),
                getValue: () => Config.CustomMinVal,
                setValue: (int value) => Config.CustomMinVal = value,
                min: 0);
            GMCM.AddNumberOption(mod: ModManifest,
                name: () => i18n.Get("config.customminmult.name"),
                tooltip: () => i18n.Get("config.customminmult.description"),
                getValue: () => Config.CustomMinMult,
                setValue: (float value) => Config.CustomMinMult = value,
                min: 0.0f);
            GMCM.AddBoolOption(mod: ModManifest,
                name: () => i18n.Get("config.allowtping.name"),
                tooltip: () => i18n.Get("config.allowtping.description"),
                getValue: () => Config.AllowTPing,
                setValue: (bool value) => Config.AllowTPing = value);
            GMCM.AddBoolOption(ModManifest,
                name: () => i18n.Get("config.allowegging.name"),
                tooltip: () => i18n.Get("config.allowegging.description"),
                getValue: () => Config.AllowEgging,
                setValue: (bool value) => Config.AllowEgging = value);

            GMCM.AddSectionTitle(ModManifest,
                text: () => i18n.Get("config.smallpranks.name"),
                tooltip: () => i18n.Get("config.smallpranks.description"));
            foreach (string trick in Config.SmallTricks.Keys)
            {
                GMCM.AddBoolOption(ModManifest,
                    name: () => i18n.Get($"config.allow{trick}.name"),
                    tooltip: () => i18n.Get($"config.allow{trick}.description"),
                    getValue: () => Config.SmallTricks[trick],
                    setValue: (bool value) => Config.SmallTricks[trick] = value);
            }
        }
    }
}
