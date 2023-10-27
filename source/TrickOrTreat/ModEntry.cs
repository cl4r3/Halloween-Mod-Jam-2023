using SpaceCore.Events;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TrickOrTreat
{
    public class ModEntry : Mod
    {
        internal static IMonitor ModMonitor { get; set; }
        internal new static IModHelper Helper { get; set; }
        internal static IContentPatcherApi CP;
        internal static IJsonAssetsApi JA;

        private const string AssetPath = "Mods/ch20youk.ToTData";
        private const string NPCsExt = ".NPCs";
        private const string CostumesExt = ".Costumes";
        private const string TreatsExt = ".Treats";

        public static string[] ValidRoles = { "candygiver", "candytaker", "prankster", "observer", };
        public static string[] ValidPranks = { "egg", "paint", "ALL", };
        //public static string[] ValidFlavors = { "sweet", "sour", "salty", "gourmet", "candy", "healthy", "joja", "fatty", };

        internal static Dictionary<string, Celebrant> NPCData;
        internal static Dictionary<string, Costume> CostumeData;
        internal static Dictionary<string, Treat> TreatData;

        public override void Entry(IModHelper helper)
        {
            ModMonitor = Monitor;
            Helper = helper;

            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Content.AssetReady += OnAssetReady;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            GiveTreat.Initialize(this);
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
                e.LoadFromModFile<Dictionary<string, Treat>>("assets/TreatData.json", AssetLoadPriority.Medium);
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

            NPCData = Game1.content.Load<Dictionary<string, Celebrant>>(AssetPath + NPCsExt);
            CostumeData = Game1.content.Load<Dictionary<string, Costume>>(AssetPath + CostumesExt);
            TreatData = Game1.content.Load<Dictionary<string, Treat>>(AssetPath + TreatsExt);

            ValidateNPCData();
            ValidateCostumeData();
            ValidateTreatData();
        }

        private static void ValidateNPCData()
        {
            foreach (KeyValuePair<string, Celebrant> entry in NPCData)
            {
                if (Game1.getCharacterFromName(entry.Key) is null)
                {
                    Log.Warn($"Entry {entry.Key} in Trick-or-Treat NPC Data does not appear to be a valid NPC.");
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
                    NPCData[entry.Key].TreatsToGive = Array.Empty<string>();
                }

                if (entry.Value.PreferredPranks is not null)
                {
                    if (!NPCData[entry.Key].Roles.Contains("prankster") && entry.Value.PreferredPranks.Length > 0)
                    {
                        Log.Warn($"NPC {entry.Key} has preferred pranks listed even though they do not have the role \"prankster\", meaning they do not pull pranks.");
                    }
                    else
                    {
                        var pranks = Array.ConvertAll(entry.Value.PreferredPranks, d => d.ToLower());
                        if (pranks.Except(ValidPranks).ToArray().Length > 0)
                        {
                            Log.Warn($"NPC {entry.Key} has invalid prank type listed: " + pranks.Except(ValidPranks).ToList());
                        }
                        NPCData[entry.Key].PreferredPranks = pranks;
                    }
                }
                else if (NPCData[entry.Key].Roles.Contains("prankster"))
                {
                    NPCData[entry.Key].PreferredPranks = Array.Empty<string>();
                }
            }
        }

        private static void ValidateCostumeData()
        {
            foreach (KeyValuePair<string, Costume> entry in CostumeData)
            {
                int count = 0;
#nullable enable
                int? hat_id;
                int? shirt_id;
                int? pants_id;
                int? shoes_id;
#nullable disable
                if (entry.Value.Hat is not null && entry.Value.Hat.Length > 0)
                {
                    hat_id = JA.GetHatId(entry.Value.Hat);
                    if (hat_id is null)
                    {
                        Log.Warn($"Could not find hat named {entry.Value.Hat} for costume set {entry.Key}.");
                    }
                    else {
                        CostumeData[entry.Key].HatId = hat_id;
                        count++;
                    }
                }
                if (entry.Value.Top is not null && entry.Value.Top.Length > 0)
                {
                    shirt_id = JA.GetClothingId(entry.Value.Hat);
                    if (shirt_id is null)
                    {
                        Log.Warn($"Could not find top named {entry.Value.Top} for costume set {entry.Key}.");
                    }
                    else
                    {
                        CostumeData[entry.Key].TopId = shirt_id;
                        count++;
                    }
                }
                if (entry.Value.Bottom is not null && entry.Value.Bottom.Length > 0)
                {
                    pants_id = JA.GetClothingId(entry.Value.Bottom);
                    if (pants_id is null)
                    {
                        Log.Warn($"Could not find top named {entry.Value.Bottom} for costume set {entry.Key}.");
                    }
                    else
                    {
                        CostumeData[entry.Key].BottomId = pants_id;
                        count++;
                    }
                }
                if (entry.Value.Shoes is not null && entry.Value.Shoes.Length > 0)
                {
                    shoes_id = JA.GetObjectId(entry.Value.Shoes);
                    if (shoes_id is null)
                    {
                        Log.Warn($"Could not find shoes named {entry.Value.Shoes} for costume set {entry.Key}.");
                    }
                    else
                    {
                        CostumeData[entry.Key].ShoesId = shoes_id;
                        count++;
                    }
                }
                if (entry.Value.NumPieces != count)
                {
                    Log.Warn($"Found {count} pieces in costume set {entry.Key}, but {entry.Value.NumPieces} pieces were specified.");
                }
            }
        }

        private static void ValidateTreatData()
        {
            foreach(string name in TreatData.Keys)
            {
                TreatData[name].ObjectId = JA.GetObjectId(name);
                if (name is null)
                    Log.Warn($"Could not find treat {name} among valid objects.");
            }
        }
    }
}
