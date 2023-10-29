using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using SpaceCore.Events;
using static TricksAndTreats.ModEntry;

namespace TricksAndTreats
{
    public class Tricks
    {
        static IModHelper Helper;
        static IMonitor Monitor;

        internal static void Initialize(IMod ModInstance)
        {
            Helper = ModInstance.Helper;
            Monitor = ModInstance.Monitor;

            Helper.Events.Content.AssetRequested += OnAssetRequested;
            Helper.Events.GameLoop.SaveLoaded += CheckTricksters;
            Helper.Events.GameLoop.DayStarted += OnDayStart;
            Helper.Events.GameLoop.DayEnding += OnDayEnd;
            Helper.Events.Multiplayer.PeerConnected += (object sender, PeerConnectedEventArgs e) => { ReloadHouseExteriorsMaybe(); };
        }

        [EventPriority(EventPriority.Low)]
        private static void CheckTricksters(object sender, SaveLoadedEventArgs e)
        {
            foreach (KeyValuePair<string, Celebrant> entry in NPCData)
            {
                if (entry.Value.Roles.Contains("trickster"))
                {
                    NPC npc = Game1.getCharacterFromName(entry.Key);
                    if (!npc.Dialogue.ContainsKey("hated_treat"))
                    {
                        npc.Dialogue.Add("hated_treat", Helper.Translation.Get("generic.hated_treat"));
                    }
                    if (!npc.Dialogue.ContainsKey("before_trick"))
                    {
                        npc.Dialogue.Add("before_trick", Helper.Translation.Get("generic.before_trick"));
                    }
                    npc.Dialogue["before_trick"] = npc.Dialogue["hated_treat"] + "#$b#" + npc.Dialogue["before_trick"];
                }
            }
        }

        private static void OnDayEnd(object sender, DayEndingEventArgs e)
        {
            if (Game1.currentSeason == "fall" && Game1.dayOfMonth == 27)
            {
                int score = int.Parse(Game1.player.modData[ScoreKey]);
                Log.Trace($"TaT: Total treat score for {Game1.player.Name} is {score}.");
                if (score < 10)
                    Game1.player.mailReceived.Add(HouseFlag);
                Game1.player.modData.Remove(ScoreKey);
            }
            else if (Game1.currentSeason == "fall" && Game1.dayOfMonth == 28)
            {
                if (Game1.player.mailReceived.Contains(HouseFlag))
                    Game1.player.mailReceived.Remove(HouseFlag);
            }
        }

        [EventPriority(EventPriority.Low)]
        private static void OnDayStart(object sender, DayStartedEventArgs e)
        {
            ReloadHouseExteriorsMaybe();

            Farmer farmer = Game1.player;
            if (Game1.currentSeason == "fall" && Game1.dayOfMonth == 28)
            {
                if (farmer.modData.ContainsKey(PaintKey))
                {
                    farmer.changeSkinColor(int.Parse(farmer.modData[PaintKey]), true);
                    farmer.modData.Remove(PaintKey);
                }
                if (farmer.mailReceived.Contains(HouseFlag))
                    farmer.activeDialogueEvents.Add(HouseFlag, 1);
            }
        }

        [EventPriority(EventPriority.Low)]
        private static void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (Game1.currentSeason == "fall" && Game1.dayOfMonth == 28)
            {
                if (Game1.MasterPlayer.mailReceived.Contains(HouseFlag)) 
                {
                    if (e.Name.IsEquivalentTo("Buildings/houses"))
                    {
                        Log.Debug("TaT: TPing main farmhouse...");
                        e.Edit(asset =>
                        {
                            var editor = asset.AsImage();
                            IRawTextureData sourceImage = Helper.ModContent.Load<IRawTextureData>("assets/houses.png");
                            editor.PatchImage(sourceImage, sourceArea: new Rectangle(0, 0, 272, 432), targetArea: new Rectangle(0, 0, 272, 432), patchMode: PatchMode.Overlay);
                        });
                    }
                }

                foreach(Building place in Game1.getFarm().buildings)
                {
                    //Log.Debug($"TaT: Building is {place.nameOfIndoors}, texture is {place.textureName()}");
                    if (e.Name.IsEquivalentTo(place.textureName()) && place.indoors.Value is Cabin)
                    {
                        FarmHouse home = (FarmHouse)place.indoors.Value;
                        if (home.owner is not null && home.owner.mailReceived.Contains(HouseFlag)) 
                        {
                            Log.Debug($"TaT: TPing {Game1.player.Name}'s cabin...");
                            string img = place.textureName().Split('\\')[1].Replace(' ', '-').ToLower() + ".png";
                            e.Edit(asset =>
                            {
                                var editor = asset.AsImage();
                                IRawTextureData sourceImage = Helper.ModContent.Load<IRawTextureData>("assets/" + img);
                                editor.PatchImage(sourceImage, sourceArea: new Rectangle(0, 0, 240, 112), targetArea: new Rectangle(0, 0, 240, 112), patchMode: PatchMode.Overlay);
                            });
                        }
                    }
                }
            }
        }

        internal static void ReloadHouseExteriorsMaybe()
        {
            if ((Game1.currentSeason == "winter" && Game1.dayOfMonth == 1) || (Game1.currentSeason == "fall" && Game1.dayOfMonth == 28))
            {
                Helper.GameContent.InvalidateCache("Buildings/houses");
                Helper.GameContent.InvalidateCache("Buildings/Log Cabin");
                Helper.GameContent.InvalidateCache("Buildings/Plank Cabin");
                Helper.GameContent.InvalidateCache("Buildings/Stone Cabin");
            }
        }

        internal static void NPCTrick(NPC npc)
        {
            Farmer farmer = Game1.player;
            Random random = new();

            Utils.Speak(npc, "before_trick");

            Game1.afterDialogues = delegate
            {
                string after_trick = "after_trick";
                var tricks = NPCData[npc.Name].PreferredTricks;
                string trick;
                if (tricks.Contains("all"))
                {
                    trick = ValidTricks[random.Next(ValidTricks.Length)];
                }
                else if (tricks.Length == 1)
                {
                    trick = tricks[0];
                }
                else
                {
                    trick = tricks[random.Next(tricks.Length)];
                }
                switch (trick)
                {
                    case "egg":
                        if (!EggSteal(farmer, random))
                            after_trick = "cannot_trick";
                        break;
                    case "paint":
                        PaintSkin(farmer, random);
                        break;
                    default:
                        Log.Error("No preferred trick found for NPC " + npc.Name);
                        break;
                }
                DelayedAction.functionAfterDelay(
                    () =>
                    {
                        Utils.Speak(npc, after_trick);
                    },
                    1000 // delay in milliseconds
                );
            };
        }

        internal static void PaintSkin(Farmer farmer, Random random)
        {
            farmer.modData.Add(PaintKey, farmer.skinColor.ToString());
            farmer.changeSkinColor(random.Next(17, 23), true);
            farmer.currentLocation.localSound("slimedead");
        }

        internal static bool EggSteal(Farmer farmer, Random random)
        {
            for (int i = 0; i < farmer.MaxItems; i++)
            {
                int idx = random.Next(farmer.MaxItems);
                Item item = farmer.Items[idx];
                if (item is not null && item is not Tool && !TreatData.ContainsKey(item.Name) && Utility.IsNormalObjectAtParentSheetIndex(item, item.ParentSheetIndex))
                {
                    farmer.modData.Add(EggKey, farmer.Items.ElementAt(idx).ParentSheetIndex.ToString());
                    farmer.Items.RemoveAt(idx);
                    int dud_item = JA.GetObjectId(Helper.ModRegistry.IsLoaded("ch20youk.TaTPelicanTown.CP") ? "TaT.rotten-egg" : "Egg");
                    farmer.Items.Insert(idx, new StardewValley.Object(dud_item, 1));
                    farmer.currentLocation.localSound("shwip");
                    return true;
                }
            }
            return false;
        }
    }
}