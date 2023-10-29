using HarmonyLib;
using SpaceCore.Events;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TricksAndTreats
{
    public class ModEntry : Mod
    {
        internal static IMonitor ModMonitor { get; set; }
        internal new static IModHelper Helper { get; set; }
        internal static IContentPatcherApi CP;
        internal static IJsonAssetsApi JA;

        private const string AssetPath = "Mods/ch20youk.TaTData";
        private const string NPCsExt = ".NPCs";
        private const string CostumesExt = ".Costumes";
        private const string TreatsExt = ".Treats";

        internal const string HouseFlag = "TaT.VillageTrick";
        internal const string HouseCT = "house_pranked";
        internal const string CostumeCT = "costume_react-";
        internal const string TreatCT = "give_candy";

        internal const string PaintKey = "TaT.previous-skin";
        internal const string EggKey = "TaT.stolen-item";
        internal const string ScoreKey = "TaT.treat-score";
        internal const string CostumeKey = "TaT.costume-set";
        internal const string GiftedKey = "TaT.regular-gift-attempts";

        internal static string[] ValidRoles = { "candygiver", "candytaker", "trickster", "observer", };
        internal static string[] ValidTricks = { "egg", "paint", "all", };
        //public static string[] ValidFlavors = { "sweet", "sour", "salty", "hot", "gourmet", "candy", "healthy", "joja", "fatty", };

        internal static Dictionary<string, Celebrant> NPCData;
        internal static Dictionary<string, Costume> CostumeData;
        internal static Dictionary<string, Treat> TreatData;

        public override void Entry(IModHelper helper)
        {
            ModMonitor = Monitor;
            Helper = helper;
            var harmony = new Harmony(ModManifest.UniqueID);

            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Content.AssetReady += OnAssetReady;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.TimeChanged += OnTimeChange;

            Tricks.Initialize(this);
            Treats.Initialize(this);
            Costumes.Initialize(this);
        }

        private void OnTimeChange(object sender, TimeChangedEventArgs e)
        {
            if (Game1.currentSeason == "fall" && Game1.dayOfMonth == 27)
            {
                if (Game1.timeOfDay < 2200)
                {
                    Game1.whereIsTodaysFest = null;
                }
                else if(Game1.timeOfDay >= 2200)
                {
                    Game1.whereIsTodaysFest = "Town";
                }
                if (Game1.timeOfDay == 2200)
                {
                    if (Game1.player.currentLocation.Name == "Town")
                        Game1.warpFarmer("BusStop", 34, 23, 3);
                }
            }
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo(AssetPath + NPCsExt))
            {
                e.LoadFromModFile<Dictionary<string, Celebrant>>("assets/EmptyJson.json", AssetLoadPriority.Medium);
            }
            else if (e.Name.IsEquivalentTo(AssetPath + CostumesExt))
            {
                e.LoadFromModFile<Dictionary<string, Costume>>("assets/EmptyJson.json", AssetLoadPriority.Medium);
            }
            else if (e.Name.IsEquivalentTo(AssetPath + TreatsExt))
            {
                e.LoadFromModFile<Dictionary<string, Treat>>("assets/EmptyJson.json", AssetLoadPriority.Medium);
            }
        }

        private void OnAssetReady(object sender, AssetReadyEventArgs e)
        {
            if (e.Name.IsEquivalentTo(AssetPath + NPCsExt))
            {
                NPCData = Game1.content.Load<Dictionary<string, Celebrant>>(AssetPath + NPCsExt);
            }
            else if (e.Name.IsEquivalentTo(AssetPath + CostumesExt))
            {
                CostumeData = Game1.content.Load<Dictionary<string, Costume>>(AssetPath + CostumesExt);
            }
            else if (e.Name.IsEquivalentTo(AssetPath + TreatsExt))
            {
                TreatData = Game1.content.Load<Dictionary<string, Treat>>(AssetPath + TreatsExt);
            }
        }

        private void OnGameLaunched(object sender, EventArgs e)
        {
            CP = Helper.ModRegistry.GetApi<IContentPatcherApi>("Pathoschild.ContentPatcher");
            if (CP == null)
            {
                Log.Error("Content Patcher API not found. Please check that Content Patcher is correctly installed.");
            }
            JA = Helper.ModRegistry.GetApi<IJsonAssetsApi>("spacechase0.JsonAssets");
            if (JA == null)
            {
                Log.Error("Json Assets API not found. Please check that JSON Assets is correctly installed.");
            }   
        }

        [EventPriority(EventPriority.High)]
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            NPCData = Game1.content.Load<Dictionary<string, Celebrant>>(AssetPath + NPCsExt);
            CostumeData = Game1.content.Load<Dictionary<string, Costume>>(AssetPath + CostumesExt);
            TreatData = Game1.content.Load<Dictionary<string, Treat>>(AssetPath + TreatsExt);

            Utils.ValidateNPCData();
            Utils.ValidateCostumeData();
            Utils.ValidateTreatData();
        }
    }
}
