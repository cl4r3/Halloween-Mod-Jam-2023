using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using static TricksAndTreats.ModEntry;

namespace TricksAndTreats
{
    internal class Costumes
    {
        static IModHelper Helper;
        static IMonitor Monitor;

        internal static void Initialize(IMod ModInstance, Harmony harmony)
        {
            Helper = ModInstance.Helper;
            Monitor = ModInstance.Monitor;

            Helper.Events.GameLoop.SaveLoaded += (object sender, SaveLoadedEventArgs e) => { CheckForCostumeSet(); };

            harmony.Patch(
                original: AccessTools.Method(typeof(Farmer), "changeHat"),
                postfix: new HarmonyMethod(typeof(Costumes), nameof(changeClothes))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Farmer), "changeShirt"),
                postfix: new HarmonyMethod(typeof(Costumes), nameof(changeClothes))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Farmer), "changePantStyle"),
                postfix: new HarmonyMethod(typeof(Costumes), nameof(changeClothes))
            );
        }

        private static void changeClothes(Farmer __instance, int __0)
        {
            if (CheckForCostumeSet())
            {
                var costume = IsWearingSet(__instance);
                if (costume is not null)
                {
                    Log.Debug("TaT: Costume set is " + costume);
                    __instance.activeDialogueEvents.Add(CostumeCT + costume, 1);
                }
                else
                {
                    var costume_ct = __instance.activeDialogueEvents.Keys.ToList().Find(ct => ct.StartsWith(CostumeCT));
                    if (costume_ct is not null)
                        __instance.activeDialogueEvents.Remove(costume_ct);
                }
            }
        }

         [EventPriority(EventPriority.Low)]
        internal static bool CheckForCostumeSet()
        {
            if (!Context.IsWorldReady)
                return false;

            Farmer farmer = Game1.player;

            if (!(Game1.currentSeason == "fall" || Game1.dayOfMonth == 27))
            {
                if (farmer.modData.ContainsKey(CostumeKey))
                    farmer.modData.Remove(CostumeKey);
                return false;
            }

            if (!farmer.modData.ContainsKey(CostumeKey))
                farmer.modData.Add(CostumeKey, "//");

            int hat = farmer.hat.Value.which.Value;
            int top = farmer.shirt.Value;
            int bot = farmer.pants.Value;
            var clothes = farmer.modData[CostumeKey].Split('/');

            foreach (KeyValuePair<string, Costume> entry in CostumeData)
            {
                if (hat == entry.Value.HatId)
                    clothes.SetValue(entry.Value, 0);
                if (top == entry.Value.TopId)
                    clothes.SetValue(entry.Value, 1);
                if (bot == entry.Value.BotId)
                    clothes.SetValue(entry.Value, 2);
            }

            farmer.modData[CostumeKey] = String.Join("/", clothes);
            return true;
        }

        internal static string IsWearingSet(Farmer farmer)
        {
            var clothes = farmer.modData[CostumeKey].Split('/');
            if (clothes[0] == clothes[1] && clothes[1] == clothes[2] && CostumeData[clothes[0]].NumPieces == 3)
                return clothes[0];
            if (clothes[0] == clothes[1] || clothes[0] == clothes[2] && CostumeData[clothes[0]].NumPieces == 2)
                return clothes[0];
            if (clothes[0] == clothes[1] || clothes[1] == clothes[2] && CostumeData[clothes[1]].NumPieces == 2)
                return clothes[1];
            if (!String.IsNullOrWhiteSpace(clothes[0]) && CostumeData[clothes[0]].NumPieces == 1)
                return clothes[0];
            if (!String.IsNullOrWhiteSpace(clothes[1]) && CostumeData[clothes[1]].NumPieces == 1)
                return clothes[1];
            if (!String.IsNullOrWhiteSpace(clothes[2]) && CostumeData[clothes[2]].NumPieces == 1)
                return clothes[2];
            return null;
        }
    }

}
