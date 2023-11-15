using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore.Events;
using StardewModdingAPI;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace TricksAndTreats
{
    public class ModEntry : Mod
    {
        internal static IMonitor ModMonitor { get; set; }
        internal new static IModHelper Helper { get; set; }
        internal static IJsonAssetsApi JA;
        internal static IGenericModConfigMenuApi GMCM;
        internal static IContentPatcherApi CP;
        internal static ModConfig Config;
        private ConfigMenu ConfigMenu;

        internal static readonly string AssetPath = "Mods/TricksAndTreats";
        internal static readonly string JAPath = Path.Combine("assets", "JsonAssets");
        internal static readonly string NPCsExt = ".NPCs";
        internal static readonly string CostumesExt = ".Costumes";
        internal static readonly string TreatsExt = ".Treats";

        internal const string HouseFlag = "TaT.LargeScaleTrick";
        internal const string HouseCT = "house_pranked";
        internal const string CostumeCT = "costume_react-";
        internal const string TreatCT = "give_candy";

        internal const string ModPrefix = "TaT.";
        internal const string PaintKey = ModPrefix + "previous-skin";
        internal const string StolenKey = ModPrefix + "stolen-items";
        internal const string ScoreKey = ModPrefix + "treat-score";
        internal const string CostumeKey = ModPrefix + "costume-set";
        internal const string ChestKey = ModPrefix + "reached-chest";
        internal const string MysteryKey = ModPrefix + "original-treat";
        internal const string CobwebKey = ModPrefix + "cobwebbed";

        public static string[] ValidRoles = { "candygiver", "candytaker", "trickster", "observer", };
        //public static string[] ValidFlavors = { "sweet", "sour", "salty", "hot", "gourmet", "candy", "healthy", "joja", "fatty", };

        internal static Dictionary<string, int> ClothingInfo;
        internal static Dictionary<string, int> FoodInfo;

        internal static Dictionary<string, Celebrant> NPCData;
        internal static Dictionary<string, Costume> CostumeData;
        internal static Dictionary<string, Treat> TreatData;

        public override void Entry(IModHelper helper)
        {
            ModMonitor = Monitor;
            Helper = helper;
            ConfigMenu = new ConfigMenu(this);
            ConsoleCommands.Register(this);

            //helper.Events.Display.RenderingWorld += OnRenderingWorld;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Specialized.LoadStageChanged += OnLoadStageChanged;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Content.AssetReady += OnAssetReady;
            helper.Events.GameLoop.TimeChanged += OnTimeChange;

            Tricks.Initialize(this);
            Treats.Initialize(this);
            Costumes.Initialize(this);
        }

        /*
        private static void OnRenderingWorld(object sender, RenderingWorldEventArgs e)
        {
            Game1.graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
            Game1.graphics.ApplyChanges();
            Game1.graphics.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.Stencil, Color.Transparent, 0, 0);
        }
        */

        private void OnGameLaunched(object sender, EventArgs e)
        {
            ClothingInfo = Helper.Data.ReadJsonFile<Dictionary<string, int>>(PathUtilities.NormalizePath("assets/clothing.json"));
            FoodInfo = Helper.Data.ReadJsonFile<Dictionary<string, int>>(PathUtilities.NormalizePath("assets/food.json"));

            JA = Helper.ModRegistry.GetApi<IJsonAssetsApi>("spacechase0.JsonAssets");
            if (JA == null)
            {
                Log.Error("Json Assets API not found. Please check that JSON Assets is correctly installed.");
                return;
            }
            JA.LoadAssets(Path.Combine(Helper.DirectoryPath, JAPath), Helper.Translation);

            Config = Helper.ReadConfig<ModConfig>();
            GMCM = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            ConfigMenu.RegisterGMCM();

            CP = Helper.ModRegistry.GetApi<IContentPatcherApi>("Pathoschild.ContentPatcher");
            if (CP == null)
            {
                Log.Error("Content Patcher API not found. Please check that Content Patcher is correctly installed.");
                return;
            }
            ConfigMenu.RegisterTokens();

            HarmonyPatches.Patch(id: ModManifest.UniqueID);
        }

        private void OnLoadStageChanged(object sender, LoadStageChangedEventArgs e)
        {
            if (e.NewStage == LoadStage.CreatedInitialLocations || e.NewStage == LoadStage.SaveAddedLocations)
                Game1.locations.Add(new GameLocation(Helper.ModContent.GetInternalAssetName("assets/Maze.tmx").BaseName, "Custom_TaT_Maze"));
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

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo(AssetPath + NPCsExt))
            {
                e.LoadFrom(() => new Dictionary<string, Celebrant>(), AssetLoadPriority.Exclusive);
            }
            else if (e.Name.IsEquivalentTo(AssetPath + CostumesExt))
            {
                e.LoadFrom(() => new Dictionary<string, Costume>(), AssetLoadPriority.Exclusive);
            }
            else if (e.Name.IsEquivalentTo(AssetPath + TreatsExt))
            {
                e.LoadFrom(() => new Dictionary<string, Treat>(), AssetLoadPriority.Exclusive);
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

        private void OnTimeChange(object sender, TimeChangedEventArgs e)
        {
            if (Game1.currentSeason == "fall" && Game1.dayOfMonth == 27)
            {
                if (Game1.timeOfDay < 2100)
                {
                    Game1.whereIsTodaysFest = null;
                }
                else if (Game1.timeOfDay >= 2100)
                {
                    Game1.whereIsTodaysFest = "Town";
                }
                if (Game1.timeOfDay == 2100)
                {
                    if (Game1.player.currentLocation.Name == "Town")
                    {
                        Game1.activeClickableMenu = new DialogueBox(Helper.Translation.Get("info.festival_prep"));
                        Game1.warpFarmer("BusStop", 34, 23, 3);
                    }
                }
            }
        }
    }
}
