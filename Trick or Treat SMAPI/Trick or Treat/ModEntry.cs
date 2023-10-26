using System;
using StardewValley;
using StardewModdingAPI;
using SpaceCore.Events;

namespace TrickOrTreat
{
    public class ModEntry : Mod
    {
        internal static IMonitor ModMonitor { get; set; }
        internal new static IModHelper Helper { get; set; }
        internal static IJsonAssetsApi JA;

        public override void Entry(IModHelper helper)
        {
            ModMonitor = Monitor;
            Helper = helper;

            helper.Events.GameLoop.GameLaunched += RegisterAPI;

            GiveTreat.Initialize(this);
        }

        private static void RegisterAPI(object sender, EventArgs e)
        {
            JA = Helper.ModRegistry.GetApi<IJsonAssetsApi>("spacechase0.JsonAssets");
            if (JA == null)
            {
                Log.Warn("Json Assets API not found. This could lead to issues.");
            }
        }
    }
}
