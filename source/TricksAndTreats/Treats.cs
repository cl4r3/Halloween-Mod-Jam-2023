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

        internal static void Initialize(IMod ModInstance)
        {
            Helper = ModInstance.Helper;
            Monitor = ModInstance.Monitor;

            Helper.Events.GameLoop.DayStarted += CheckCandyCT;
            Helper.Events.GameLoop.SaveLoaded += CheckCandyGivers;
            SpaceEvents.BeforeGiftGiven += TreatForNPC;
        }

        private static void CheckCandyCT(object sender, DayStartedEventArgs e)
        {
            if (Game1.currentSeason == "fall" && Game1.dayOfMonth == 27)
            {
                Game1.player.activeDialogueEvents.Add(TreatCT, 1);
            }
            else if (Game1.player.activeDialogueEvents.ContainsKey(TreatCT))
            {
                    Game1.player.activeDialogueEvents.Remove(TreatCT);
            }
        }

        [EventPriority(EventPriority.Low)]
        private static void CheckCandyGivers(object sender, SaveLoadedEventArgs e)
        {
            foreach (KeyValuePair<string, Celebrant> entry in NPCData)
            {
                if (entry.Value.Roles.Contains("candygiver"))
                {
                    NPC npc = Game1.getCharacterFromName(entry.Key);
                    if (!npc.Dialogue.ContainsKey(TreatCT))
                    {
                        npc.Dialogue.Add(TreatCT, Helper.Translation.Get("generic.give_candy"));
                    }
                    var TreatsToGive = entry.Value.TreatsToGive;
                    Random random = new();
                    int gift = JA.GetObjectId(TreatsToGive[random.Next(TreatsToGive.Length)]);
                    //Log.Debug($"NPC {entry.Value} will give treat ID {gift}.");
                    npc.Dialogue[TreatCT] = npc.Dialogue[TreatCT] + $" [{gift}]";
                }
            }
        }

        private static void TreatForNPC(object sender, EventArgsBeforeReceiveObject e)
        {
            if (!TreatData.ContainsKey(e.Gift.Name))
                return;

            e.Cancel = true;

            if (!NPCData.ContainsKey(e.Npc.Name))
                return;

            if (!(Game1.currentSeason == "fall" && Game1.dayOfMonth == 27))
            {
                if (TreatData[e.Gift.Name].HalloweenOnly)
                {
                    Game1.activeClickableMenu = new DialogueBox(Helper.Translation.Get("info.not_halloween"));
                    return;
                }
            }

            if (!NPCData[e.Npc.Name].Roles.Contains("candytaker"))
            {
                Utils.Speak(e.Npc, "not_candytaker", clear: false);
                return;
            }

            StardewValley.Object gift = e.Gift;
            Farmer gifter = Game1.player;
            NPC giftee = e.Npc;

            if ((bool)NPCData[giftee.Name].ReceivedGift)
            {
                Game1.activeClickableMenu = new DialogueBox(Helper.Translation.Get("info.already_given"));
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
                    response_key = "loved_treat";
                    break;
                case NPC.gift_taste_dislike:
                case NPC.gift_taste_hate:
                    score -= 2;
                    gifter.changeFriendship(hated_pts, giftee);
                    response_key = "hated_treat";
                    if (NPCData[giftee.Name].Roles.Contains("trickster"))
                        play_trick = true;
                    break;
                default:
                    score += 1;
                    gifter.changeFriendship(neutral_pts, giftee);
                    response_key = "neutral_treat";
                    break;
            }
            gifter.modData[ScoreKey] = score.ToString();
            NPCData[giftee.Name].ReceivedGift = true;
            gifter.reduceActiveItemByOne();
            gifter.currentLocation.localSound("give_gift");
            if (play_trick)
                Tricks.NPCTrick(giftee);
            else
                Utils.Speak(giftee, response_key, clear: false);
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
