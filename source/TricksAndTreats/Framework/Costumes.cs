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

            Farmer farmer = Game1.player;

            string hat = farmer.hat.Value is null ? "" : farmer.hat.Value.Name;
            string top = farmer.shirtItem.Value is null ? "" : farmer.shirtItem.Value.Name;
            string bot = farmer.pantsItem.Value is null ? "": farmer.pantsItem.Value.Name;

            string[] clothes = { "", "", "" };
            foreach (KeyValuePair<string, Costume> entry in CostumeData)
            {
                if (hat == entry.Value.Hat)
                    clothes.SetValue(entry.Key, 0);
                if (top == entry.Value.Top)
                    clothes.SetValue(entry.Key, 1);
                if (bot == entry.Value.Bottom)
                    clothes.SetValue(entry.Key, 2);
            }

            string[] costumes_only = Array.Empty<string>();
            foreach (string i in clothes)
            {
                if (CostumeData.ContainsKey(i))
                    costumes_only = costumes_only.Append(i).ToArray();
            }

            var groups = costumes_only.GroupBy(v => v);
            string costume = null;
            foreach (var group in groups)
            {
                if (CostumeData[group.Key].NumPieces == group.Count())
                    costume = group.Key;
            }

            if (costume is not null)
            {
                Game1.player.modData[CostumeKey] = costume;
                Log.Debug("TaT: Currently wearing costume " + costume);
                //Game1.player.currentLocation.localSound("yoba");
                // TODO: Check for current costume and remove before adding new one
                Game1.player.activeDialogueEvents.Add(CostumeCT + costume.ToLower().Replace(' ', '_'), 1);
                // TODO: Check that TreatCT is not already added before removing (and make sure not to give candy multiple times)
                Game1.player.activeDialogueEvents.Add(TreatCT, 1);
            }
            else
            {
                Log.Debug("TaT: Currently not wearing costume");
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
