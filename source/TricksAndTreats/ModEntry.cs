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
        internal static ModConfig Config;

        internal static readonly string AssetPath = "Mods/TricksAndTreats";
        internal static readonly string JAPath = Path.Combine("assets", "JsonAssets");
        internal static readonly string NPCsExt = ".NPCs";
        internal static readonly string CostumesExt = ".Costumes";
        internal static readonly string TreatsExt = ".Treats";

        internal const string HouseFlag = "TaT.LargeScaleTrick";
        internal const string HouseCT = "house_pranked";
        internal const string CostumeCT = "costume_react-";
        internal const string TreatCT = "give_candy";

        internal const string KeyPrefix = "TaT.";
        internal const string PaintKey = KeyPrefix + "previous-skin";
        internal const string StolenKey = KeyPrefix + "stolen-items";
        internal const string ScoreKey = KeyPrefix + "treat-score";
        internal const string CostumeKey = KeyPrefix + "costume-set";
        internal const string ChestKey = KeyPrefix + "reached-chest";
        internal const string MysteryKey = KeyPrefix + "original-treat";
        internal const string CobwebKey = KeyPrefix + "cobwebbed";

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
            Config = Helper.ReadConfig<ModConfig>();

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

            GMCM = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            RegisterGMCM();

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

            ValidateNPCData();
            ValidateCostumeData();
            ValidateTreatData();
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
                allowedValues: new string[] { "minmult", "minval", "none" });
            GMCM.AddNumberOption(mod: ModManifest,
                name: () => i18n.Get("config.customminmult.name"),
                tooltip: () => i18n.Get("config.customminmult.description"),
                getValue: () => Config.CustomMinMult,
                setValue: (float value) => Config.CustomMinMult = value,
                min: 0.0f);
            GMCM.AddNumberOption(mod: ModManifest,
                name: () => i18n.Get("config.customminval.name"),
                tooltip: () => i18n.Get("config.customminval.description"),
                getValue: () => Config.CustomMinVal,
                setValue: (int value) => Config.CustomMinVal = value,
                min: 0);
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

        internal static void ValidateNPCData()
        {
            foreach (KeyValuePair<string, Celebrant> entry in NPCData)
            {
                // Check that NPC exists
                if (Game1.getCharacterFromName(entry.Key, false, false) is null)
                {
                    Log.Trace($"TaT: Entry {entry.Key} in Trick-or-Treat NPC Data does not appear to be a valid NPC.");
                    NPCData.Remove(entry.Key);
                    continue;
                }

                // Check roles
                var roles = Array.ConvertAll(entry.Value.Roles, d => d.ToLower());
                if (roles.Except(ValidRoles).ToArray().Length > 0)
                {
                    Log.Warn($"NPC {entry.Key} has an invalid Trick-or-Treat role listed: " + roles.Except(ValidRoles).ToList());
                }
                NPCData[entry.Key].Roles = roles;

                // Check candygiver role
                if (entry.Value.TreatsToGive is not null && entry.Value.TreatsToGive.Length > 0)
                {
                    if (!NPCData[entry.Key].Roles.Contains("candygiver"))
                    {
                        Log.Warn($"NPC {entry.Key} has treats to give listed even though they do not have the role \"candygiver\", meaning they do not give candy.");
                    }
                }
                else if (NPCData[entry.Key].Roles.Contains("candygiver"))
                {
                    var treat = Helper.ModRegistry.IsLoaded("ch20youk.TaTPelicanTown.CP") ? "TaT.candy-corn" : "Maple Bar";
                    NPCData[entry.Key].TreatsToGive = Array.Empty<string>().Append(treat).ToArray();
                }

                // Check trickster role (and tricks)
                if (entry.Value.PreferredTricks is not null)
                {
                    if (!NPCData[entry.Key].Roles.Contains("trickster") && entry.Value.PreferredTricks.Length > 0)
                    {
                        Log.Warn($"NPC {entry.Key} has preferred tricks listed even though they do not have the role \"trickster\", meaning they do not pull tricks.");
                    }
                    else
                    {
                        var tricks = Array.ConvertAll(entry.Value.PreferredTricks, d => d.ToLower());
                        if (tricks.Contains("all"))
                            tricks = Config.SmallTricks.Keys.ToArray();
                        else
                            tricks = tricks.Distinct().ToArray();
                        var invalid_tricks = tricks.Where(x => !Config.SmallTricks.Keys.Contains(x)).ToArray();
                        if (invalid_tricks.Length > 0)
                        {
                            Log.Warn($"NPC {entry.Key} has invalid trick type listed: " + invalid_tricks.ToList());
                        }
                        NPCData[entry.Key].PreferredTricks = tricks.Except(invalid_tricks).ToArray();
                    }
                }
                else if (NPCData[entry.Key].Roles.Contains("trickster"))
                {
                    Log.Trace($"NPC {entry.Key} has no preferred tricks listed... Setting to all enabled tricks.");
                    NPCData[entry.Key].PreferredTricks = Config.SmallTricks.Keys.ToArray().Where((string val) => { return Config.SmallTricks[val]; }).ToArray(); ;
                }
            }
        }

        internal static void ValidateCostumeData()
        {
            foreach (KeyValuePair<string, Costume> entry in CostumeData)
            {
                int count = 0;
                if (entry.Value is null)
                {
                    Log.Warn($"Could not find any data for costume set {entry.Key}.");
                    CostumeData.Remove(entry.Key);
                    continue;
                }
                if (entry.Value.Hat is not null && entry.Value.Hat.Length > 0 && JA.GetHatId(entry.Value.Hat) >= 0)
                    count++;
                else { CostumeData[entry.Key].Hat = ""; }
                if (entry.Value.Top is not null && entry.Value.Top.Length > 0 && (JA.GetClothingId(entry.Value.Top) >= 0 || ClothingInfo.ContainsKey(entry.Value.Top)))
                    count++;
                else { CostumeData[entry.Key].Top = ""; }
                if (entry.Value.Bottom is not null && entry.Value.Bottom.Length > 0 && (JA.GetClothingId(entry.Value.Bottom) >= 0 || ClothingInfo.ContainsKey(entry.Value.Bottom)))
                    count++;
                else { CostumeData[entry.Key].Bottom = ""; }

                if (count < 2)
                {
                    Log.Warn($"TaT: Removed costume set {entry.Key} with hat {entry.Value.Hat}, top {entry.Value.Top}, bottom {entry.Value.Bottom}");
                    CostumeData.Remove(entry.Key);
                    continue;
                }
                Log.Trace($"TaT: Registered costume set {entry.Key} with hat {entry.Value.Hat}, top {entry.Value.Top}, bottom {entry.Value.Bottom}");
                CostumeData[entry.Key].NumPieces = count;
            }
        }

        internal static void ValidateTreatData()
        {
            foreach (string name in TreatData.Keys)
            {
                if (string.IsNullOrEmpty(name))
                {
                    Log.Warn($"TaT: Treat was null or empty.");
                    TreatData.Remove(name);
                }
                var ja_id = JA.GetObjectId(name);
                TreatData[name].ObjectId = ja_id != -1 ? ja_id : FoodInfo[name];
                if (TreatData[name].ObjectId is null || TreatData[name].ObjectId < 0)
                {
                    Log.Warn($"TaT: No valid object ID found for treat {name}.");
                    TreatData.Remove(name);
                }
            }
        }
    }
}
