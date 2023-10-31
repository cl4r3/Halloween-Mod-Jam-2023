using HarmonyLib;
using SpaceCore.Events;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
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
                if (Game1.timeOfDay < 2100)
                {
                    Game1.whereIsTodaysFest = null;
                }
                else if(Game1.timeOfDay >= 2100)
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

            ValidateNPCData();
            ValidateCostumeData();
            ValidateTreatData();
        }

        internal static void ValidateNPCData()
        {
            foreach (KeyValuePair<string, Celebrant> entry in NPCData)
            {
                if (Game1.getCharacterFromName(entry.Key, false, false) is null)
                {
                    Log.Warn($"Entry {entry.Key} in Trick-or-Treat NPC Data does not appear to be a valid NPC.");
                    NPCData.Remove(entry.Key);
                    continue;
                }
                var roles = Array.ConvertAll(entry.Value.Roles, d => d.ToLower());
                if (roles.Except(ValidRoles).ToArray().Length > 0)
                {
                    Log.Warn($"NPC {entry.Key} has an invalid Trick-or-Treat role listed: " + roles.Except(ValidRoles).ToList());
                }
                NPCData[entry.Key].Roles = roles;

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

                if (entry.Value.PreferredTricks is not null)
                {
                    if (!NPCData[entry.Key].Roles.Contains("trickster") && entry.Value.PreferredTricks.Length > 0)
                    {
                        Log.Warn($"NPC {entry.Key} has preferred tricks listed even though they do not have the role \"trickster\", meaning they do not pull tricks.");
                    }
                    else
                    {
                        var tricks = Array.ConvertAll(entry.Value.PreferredTricks, d => d.ToLower());
                        if (tricks.Except(ValidTricks).ToArray().Length > 0)
                        {
                            Log.Warn($"NPC {entry.Key} has invalid trick type listed: " + tricks.Except(ValidTricks).ToList());
                        }
                        NPCData[entry.Key].PreferredTricks = tricks;
                    }
                }
                else if (NPCData[entry.Key].Roles.Contains("trickster"))
                {
                    NPCData[entry.Key].PreferredTricks = Array.Empty<string>().Append("all").ToArray();
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
                if (entry.Value.Hat is not null && entry.Value.Hat.Length > 0 && JA.GetHatId(entry.Value.Hat) != -1)
                    count++;
                else { CostumeData[entry.Key].Hat = ""; }
                if (entry.Value.Top is not null && entry.Value.Top.Length > 0 && JA.GetClothingId(entry.Value.Top) != -1)
                    count++;
                else { CostumeData[entry.Key].Top = ""; }
                if (entry.Value.Bottom is not null && entry.Value.Bottom.Length > 0 && JA.GetClothingId(entry.Value.Bottom) != -1)
                    count++;
                else { CostumeData[entry.Key].Bottom = ""; }

                if (count < 2)
                {
                    Log.Warn("TaT: Removed invalid costume set " + entry.Key);
                    CostumeData.Remove(entry.Key);
                }
                CostumeData[entry.Key].NumPieces = count;
            }
        }

        internal static void ValidateTreatData()
        {
            foreach (string name in TreatData.Keys)
            {
                TreatData[name].ObjectId = JA.GetObjectId(name);
                if (name is null)
                {
                    Log.Warn($"Could not find treat {name} among valid objects.");
                    TreatData.Remove(name);
                }
            }
        }
    }
}
