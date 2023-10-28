using HarmonyLib;
using System;
using System.Linq;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Menus;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using SpaceCore.Events;
using static TricksAndTreats.ModEntry;

namespace TricksAndTreats
{
    internal static class Treats
    {
        public const int loved_pts = 160;
        public const int neutral_pts = 90;
        public const int hated_pts = -80;

        static IModHelper Helper;
        static IMonitor Monitor;

        internal static void Initialize(IMod ModInstance, Harmony harmony)
        {
            Helper = ModInstance.Helper;
            Monitor = ModInstance.Monitor;

            SpaceEvents.BeforeGiftGiven += TreatForNPC;

            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), "checkAction"),
                prefix: new HarmonyMethod(typeof(Treats), nameof(TreatFromNPC))
            );
        }

        private static bool TreatFromNPC(NPC __instance, GameLocation __0)
        {
            if (!(Game1.currentSeason == "fall" && Game1.dayOfMonth == 27))
                return true;
            if (!NPCData.ContainsKey(__instance.Name))
            {
                Log.Debug($"NPC {__instance.Name} has no TaT data.");
                return true;
            }
            if (NPCData[__instance.Name].Roles.Contains("candygiver") && !(bool)NPCData[__instance.Name].GaveGift)
            {
                var TreatsToGive = NPCData[__instance.Name].TreatsToGive;
                Random random = new();
                var gift = JA.GetObjectId(TreatsToGive[random.Next(TreatsToGive.Length)]);
                Utils.ClearAndPushDialogue(__instance, "give_candy", gift);
                NPCData[__instance.Name].GaveGift = true;
                return false;
            }
            return true;
        }

        private static void TreatForNPC(object sender, EventArgsBeforeReceiveObject e)
        {
            if (!(Game1.currentSeason == "fall" && Game1.dayOfMonth == 27) && e.Gift.HasContextTag("halloween_treat"))
            {
                Game1.activeClickableMenu = new DialogueBox(Helper.Translation.Get("not_halloween"));
                return;
            }

            e.Cancel = true;
            StardewValley.Object gift = e.Gift;
            Farmer gifter = Game1.player;
            NPC giftee = e.Npc;

            //Log.Debug("TaT: NPCData looks like this: " + NPCData.ToString());
            if (!NPCData.ContainsKey(giftee.Name))
            {
                Log.Debug($"NPC {giftee.Name} has no TaT data.");
                return;
            }

            if (!NPCData[giftee.Name].Roles.Contains("candytaker"))
            {
                Utils.ClearAndPushDialogue(giftee, "not_candytaker");
            }

            if ((bool)NPCData[giftee.Name].ReceivedGift)
            {
                Game1.activeClickableMenu = new DialogueBox(Helper.Translation.Get("already_given"));
                return;
            }

            int score = 0;
            if (!gifter.modData.ContainsKey(ScoreKey))
                gifter.modData.Add(ScoreKey, "0");
            else score = int.Parse(gifter.modData[ScoreKey]);
            string response_key;
            bool play_trick = false;
            int gift_taste = GetTreatTaste(giftee.Name, gift.Name);
            switch (gift_taste)
            {
                case NPC.gift_taste_like:
                case NPC.gift_taste_love:
                    score += 2;
                    gifter.changeFriendship(loved_pts, giftee);
                    response_key = giftee.Dialogue.ContainsKey("loved_treat") ? "loved_treat" : "generic.loved_treat";
                    break;
                case NPC.gift_taste_dislike:
                case NPC.gift_taste_hate:
                    score -= 2;
                    gifter.changeFriendship(hated_pts, giftee);
                    response_key = giftee.Dialogue.ContainsKey("hated_treat") ? "hated_treat" : "generic.hated_treat";
                    if (NPCData[giftee.Name].Roles.Contains("trickster"))
                        play_trick = true;
                    break;
                default:
                    score += 1;
                    gifter.changeFriendship(neutral_pts, giftee);
                    response_key = giftee.Dialogue.ContainsKey("neutral_treat") ? "neutral_treat" : "generic.neutral_treat";
                    break;
            }
            gifter.modData[ScoreKey] = score.ToString();
            NPCData[giftee.Name].ReceivedGift = true;
            gifter.reduceActiveItemByOne();
            gifter.currentLocation.localSound("give_gift");
            Utils.ClearAndPushDialogue(giftee, response_key);

            if (play_trick)
                Tricks.NPCTrick(giftee);
        }

        private static int GetTreatTaste(string npc, string item)
        {
            var my_data = NPCData[npc];
            if (my_data.LovedTreats.ToList().Contains(item))
                return NPC.gift_taste_love;
            if (my_data.NeutralTreats.ToList().Contains(item))
                return NPC.gift_taste_neutral;
            if (my_data.HatedTreats.ToList().Contains(item))
                return NPC.gift_taste_hate;

            if (TreatData[item].Universal is not null)
            {
                switch (TreatData[item].Universal.ToLower())
                {
                    case "love":
                        return NPC.gift_taste_love;
                    case "neutral":
                        return NPC.gift_taste_neutral;
                    case "hate":
                        return NPC.gift_taste_hate;
                }
            }

            foreach (string flavor in TreatData[item].Flavors.ToList())
            {
                if (my_data.LovedTreats.ToList().Contains(flavor))
                    return NPC.gift_taste_love;
                if (my_data.NeutralTreats.ToList().Contains(flavor))
                    return NPC.gift_taste_neutral;
                if (my_data.HatedTreats.ToList().Contains(flavor))
                    return NPC.gift_taste_hate;
            }

            return NPC.gift_taste_neutral;
        }
    }
}
