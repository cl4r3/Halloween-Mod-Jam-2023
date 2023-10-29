using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Menus;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using static TricksAndTreats.ModEntry;

namespace TricksAndTreats
{
    internal class Costumes
    {
        static IModHelper Helper;
        static IMonitor Monitor;

        internal static void Initialize(IMod ModInstance)
        {
            Helper = ModInstance.Helper;
            Monitor = ModInstance.Monitor;

            Helper.Events.GameLoop.DayStarted += (object sender, DayStartedEventArgs e) => { CheckForCostume(); };
            Helper.Events.Display.MenuChanged += OnMenuChanged;
        }

        private static void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (!Context.IsWorldReady || e.OldMenu is null)
                return;

            //Log.Debug($"TaT: OldMenu is " + e.OldMenu.GetType());
            if (e.OldMenu is not GameMenu)
                return;

            CheckForCostume();
        }

        internal static void CheckForCostume()
        {
            if (!Context.IsWorldReady || !(Game1.currentSeason == "fall" && Game1.dayOfMonth == 27))
                return;

            Log.Debug($"TaT: in ClothingChange");
            Farmer farmer = Game1.player;

            //Utils.ValidateCostumeData();

            string hat = farmer.hat.Value is null ? "" : farmer.hat.Value.Name;
            string top = farmer.shirtItem.Value is null ? "" : farmer.shirtItem.Value.Name;
            string bot = farmer.pantsItem.Value is null ? "": farmer.pantsItem.Value.Name;

            string[] clothes = { " ", " ", " " };
            //Log.Debug($"TaT: Fresh JA pull says hat {JA.GetHatId(CostumeData["Alien"].Hat)}, shirt {JA.GetClothingId(CostumeData["Alien"].Top)}, pants {JA.GetClothingId(CostumeData["Alien"].Bot)}");
            //var clothes = farmer.modData[CostumeKey].Split('/');
            foreach (KeyValuePair<string, Costume> entry in CostumeData)
            {
                if (hat == entry.Value.Hat)
                    clothes.SetValue(entry.Key, 0);
                if (top == entry.Value.Top)
                    clothes.SetValue(entry.Key, 1);
                if (bot == entry.Value.Bottom)
                    clothes.SetValue(entry.Key, 2);
            }
            Log.Debug("TaT: clothes is now " + String.Join('/', clothes));
            string[] costumes_only = Array.Empty<string>();
            //Log.Debug("TaT: CostumeData contains " + clothes[0] + " is " + CostumeData.ContainsKey(clothes[0]));
            foreach (string i in clothes)
            {
                if (CostumeData.ContainsKey(i))
                    costumes_only = costumes_only.Append(i).ToArray();
            }
            Log.Debug("TaT: Length of costumes_only is " + costumes_only.Length);
            var groups = costumes_only.GroupBy(v => v);
            string costume = null;
            foreach (var group in groups)
            {
                Log.Debug("TaT: Clothing group " + group.Key + " " + group.Count());
                if (CostumeData[group.Key].NumPieces == group.Count())
                    costume = group.Key;
            }
            Log.Debug("TaT: Length of groups is " + groups.Count());
            if (costume is not null)
            {
                Game1.player.modData[CostumeKey] = costume;
                Log.Debug("TaT: Costume set to " + costume);
                //Game1.player.currentLocation.localSound("yoba");
                Game1.player.activeDialogueEvents.Add(CostumeCT + costume, 1);
                Game1.player.activeDialogueEvents.Add(TreatCT, 1);
            }
            else
            {
                var costume_ct = Game1.player.activeDialogueEvents.Keys.ToList().Find(ct => ct.StartsWith(CostumeCT));
                if (costume_ct is not null)
                {
                    if (Game1.player.modData.ContainsKey(CostumeKey))
                        Game1.player.modData.Remove(CostumeKey);
                    Game1.player.activeDialogueEvents.Remove(costume_ct);
                    Game1.player.activeDialogueEvents.Remove(TreatCT);
                }
                   
            }
        }
    }

}
