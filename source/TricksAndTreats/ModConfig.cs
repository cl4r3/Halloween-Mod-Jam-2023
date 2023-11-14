using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TricksAndTreats
{
    public class ModConfig
    {
        public string ScoreCalcMethod { get; set; } = "minmult";
        public float CustomMinMult { get; set; } = 1.5f;
        public int CustomMinVal { get; set; } = 0;
        public bool AllowTPing { get; set; } = true;
        public bool AllowEgging { get; set; } = true;
        public Dictionary<string, bool> SmallTricks = new Dictionary<string, bool>(){
            { "steal", true },
            { "paint", true },
            { "maze", true },
            { "nickname", true },
            { "mystery", true },
            //{ "cobweb", true },
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
    }
}
