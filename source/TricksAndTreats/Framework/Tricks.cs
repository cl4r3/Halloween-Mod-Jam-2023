using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using xTile.Dimensions;
using xTile.ObjectModel;
using static TricksAndTreats.ModEntry;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace TricksAndTreats
{
    public class Tricks
    {
        static IModHelper Helper;
        static IMonitor Monitor;
        static Random r;

        internal static void Initialize(IMod ModInstance)
        {
            Helper = ModInstance.Helper;
            Monitor = ModInstance.Monitor;
            r = new();

            Helper.Events.Content.AssetRequested += RetextureHouse;
            Helper.Events.GameLoop.SaveLoaded += CheckTricksters;
            Helper.Events.GameLoop.DayStarted += BeforeDayStuff;
            Helper.Events.GameLoop.DayEnding += DayEnd;
            Helper.Events.Input.ButtonPressed += OpenHalloweenChest;
            Helper.Events.Multiplayer.PeerConnected += (object sender, PeerConnectedEventArgs e) => { ReloadHouseExteriorsMaybe(); };
        }

        internal static void OpenHalloweenChest(object sender, ButtonPressedEventArgs e)
        {
            if (!(Game1.currentSeason == "fall" && Game1.dayOfMonth == 27 && Game1.currentLocation.NameOrUniqueName == "Temp" && Game1.CurrentEvent.isFestival))
                return;
            if (e.Button.IsActionButton() && e.Cursor.GrabTile.X == 33 && e.Cursor.GrabTile.Y == 13)
            {
                if (Game1.currentLocation.Objects.ContainsKey(new Vector2(33, 13)))
                {
                    if (!Game1.player.modData.TryGetValue(StolenKey, out string val))
                    {
                        Log.Debug("TaT: Player hasn't had any items stolen");
                        return;
                    }
                    Game1.currentLocation.localSound("openChest");
                    List<Item> items = new();
                    if (!Game1.player.modData.ContainsKey("reached_yet") || Game1.player.modData["reached_yet"] != "true")
                    {
                        items.Add(new StardewValley.Object(373, 1));
                        var logged = val.Split('\\');
                        foreach (string item in logged)
                        {
                            var idnstack = item.Split(' ');
                            StardewValley.Object obj = new(int.Parse(idnstack[0]), int.Parse(idnstack[1]));
                            items.Add(obj);
                        }
                        Game1.activeClickableMenu = new ItemGrabMenu(items).setEssential(essential: true);
                        (Game1.activeClickableMenu as ItemGrabMenu).source = 3;
                        Game1.player.completelyStopAnimatingOrDoingAction();
                        Game1.player.modData["reached_yet"] = "true";
                        if (!Game1.IsMultiplayer)
                            Game1.currentLocation.Objects.Remove(new Vector2(33, 13));
                        else
                            Game1.currentLocation.localSound("doorCreakReverse");
                    }
                }
            }
        }

        [EventPriority(EventPriority.Low)]
        private static void CheckTricksters(object sender, SaveLoadedEventArgs e)
        {
            foreach (KeyValuePair<string, Celebrant> entry in NPCData)
            {
                if (entry.Value.Roles.Contains("trickster"))
                {
                    NPC npc = Game1.getCharacterFromName(entry.Key);

                    // technically taken care of in util function, but needed now
                    if (!npc.Dialogue.ContainsKey("hated_treat"))
                    {
                        npc.Dialogue.Add("hated_treat", Helper.Translation.Get("generic.hated_treat"));
                    }

                    foreach (string trick in entry.Value.PreferredTricks)
                    {
                        if (trick == "all")
                            continue;
                        if (!npc.Dialogue.ContainsKey("before_" + trick))
                        {
                            npc.Dialogue.Add("before_" + trick, Helper.Translation.Get("generic.before_" + trick));
                        }
                        npc.Dialogue["before_" + trick] = npc.Dialogue["hated_treat"] + "#$b#" + npc.Dialogue["before_" + trick];
                    }
                }
            }
        }

        [EventPriority(EventPriority.Low)]
        private static void BeforeDayStuff(object sender, DayStartedEventArgs e)
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

        private static void DayEnd(object sender, DayEndingEventArgs e)
        {
            if (Game1.currentSeason == "fall" && Game1.dayOfMonth == 27)
            {
                // reset nickname if changed
                Game1.player.Name = Game1.player.displayName;

                int score = int.Parse(Game1.player.modData[ScoreKey]);
                Log.Trace($"TaT: Total treat score for {Game1.player.Name} is {score}.");
                if (score < NPCData.Keys.Count)
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
        private static void RetextureHouse(object sender, AssetRequestedEventArgs e)
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
            var tricks = NPCData[npc.Name].PreferredTricks;
            string trick;
            if (tricks.Contains("all") || tricks.Length > 1)
            {
                trick = ValidTricks[r.Next(ValidTricks.Length)];
            }
            else if (tricks.Length == 1)
            {
                trick = tricks[0];
            }
            else
            {
                trick = tricks[r.Next(tricks.Length)];
            }
            Utils.Speak(npc, "before_" + trick);

            Game1.afterDialogues = delegate
            { 
                string after_trick = "after_" + trick;
                switch (trick)
                {
                    case "egg":
                        if (!EggSteal(farmer))
                            after_trick = "cannot_trick";
                        break;
                    case "paint":
                        PaintSkin(farmer);
                        break;
                    case "maze":
                        MazeWarp(farmer, npc);
                        break;
                    case "nickname":
                        ChangeNickname(farmer);
                        break;
                    default:
                        Log.Error("No trick found for NPC " + npc.Name);
                        break;
                }
                DelayedAction.functionAfterDelay(
                    () =>
                    {
                        Game1.player.freezePause = 1000;
                        Game1.player.canMove = false;
                        Utils.Speak(npc, after_trick);
                        Game1.player.canMove = true;
                    },
                    1000 // delay in milliseconds
                );
            };
        }

        internal static void ThrowCobwebs(Farmer farmer)
        {
            Game1.player.addedSpeed = (Game1.player.Speed + Game1.player.addedSpeed)/2; 
        }

        internal static void ChangeNickname(Farmer farmer)
        {
            var nicknames = Helper.Translation.Get("tricks.dumb_names").ToString().Split(',');
            Game1.player.Name = nicknames[r.Next(nicknames.Length)];
        }

        internal static void MysteryTreat(Farmer farmer)
        {
            for (int i = 0; i < farmer.MaxItems; i++)
            {
                Item item = farmer.Items[i];
                if (item is not null && TreatData.ContainsKey(item.Name))
                {
                    // One mystery candy for each flavor 
                    //farmer.Items[i] = new StardewValley.Object(mystery_candy, farmer.Items[i].Stack);
                }
            }
        }

        internal static void MazeWarp(Farmer farmer, NPC warper)
        {
            GameLocation where = null;
            string[] start = null;
            string[] end = null;
            bool use_default = false;
            try
            {
                where = Game1.getLocationFromName("Custom_TaT_Maze-" + warper.Name);
                start = where.Map.Properties["MazeStart"].ToString().Split(" ");
                end = where.Map.Properties["MazeEnd"].ToString().Split(" ");
            }
            catch (Exception e)
            {
                use_default = true;
                Log.Error("TaT: Error while setting up maze. Will use default maze instead.");
                Log.Error(e.Message);
            }

            if (use_default || where is null || start is null || start.Length != 3 || end is null || end.Length != 2)
            {
                where = Game1.getLocationFromName("Custom_TaT_Maze");
                start = where.Map.Properties["MazeStart"].ToString().Split(" ");
                end = where.Map.Properties["MazeEnd"].ToString().Split(" ");
            }

            if (where.Map.Properties.ContainsKey("Warp"))
                where.Map.Properties.Remove("Warp");
            where.Map.Properties.Add("Warp", $"{end[0]} {end[1]} {farmer.currentLocation.Name} {farmer.getTileX()} {farmer.getTileY()}");
            where.updateWarps();

            Game1.warpFarmer(where.Name, int.Parse(start[0]), int.Parse(start[1]), int.Parse(start[2]));
        }
            

        internal static void PaintSkin(Farmer farmer)
        {
            farmer.modData.Add(PaintKey, farmer.skinColor.ToString());
            farmer.changeSkinColor(r.Next(17, 23), true);
            farmer.currentLocation.localSound("slimedead");
        }

        internal static bool EggSteal(Farmer farmer)
        {
            int dud_item = Helper.ModRegistry.IsLoaded("ch20youk.TaTPelicanTown.CP") ? JA.GetObjectId("TaT.rotten-egg") : 176; // 176 is Egg
            List<int> stealables = new();
            for (int i = 0; i < farmer.MaxItems; i++)
            {
                Item item = farmer.Items[i];
                //Log.Debug("TaT: Now checking stealability of item at location " + i);
                if (item is not null && item is not Tool && !TreatData.ContainsKey(item.Name) &&
                    item.ParentSheetIndex != dud_item && Utility.IsNormalObjectAtParentSheetIndex(item, item.ParentSheetIndex))
                {
                    //Log.Debug($"TaT: Adding {item.Name} at index {i} to stealables");
                    stealables.Add(i);
                }
            }
            if (stealables.Count == 0)
                return false;

            int idx = stealables[r.Next(stealables.Count)];
            string egg_val = farmer.Items[idx].ParentSheetIndex.ToString() + " " + farmer.Items[idx].Stack;

            if (!farmer.modData.ContainsKey(StolenKey))
                farmer.modData.Add(StolenKey, egg_val);
            else
                farmer.modData[StolenKey] = farmer.modData[StolenKey] + "\\" + egg_val;

            farmer.Items[idx] = new StardewValley.Object(dud_item, 1);
            farmer.currentLocation.localSound("shwip");
            stealables.Clear();
            return true;
        }
    }
}