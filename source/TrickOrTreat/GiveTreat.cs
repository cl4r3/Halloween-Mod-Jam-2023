using System;
using System.Linq;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Menus;
using StardewModdingAPI;
using SpaceCore.Events;

namespace TrickOrTreat
{
    internal static class GiveTreat
    {
        public static int score;
        public const int loved_pts = 160;
        public const int neutral_pts = 90;
        public const int hated_pts = -80;

        static IModHelper Helper;
        static IMonitor Monitor;

        internal static Dictionary<string, Celebrant> NPCData = ModEntry.NPCData;
        internal static Dictionary<string, Treat> TreatData = ModEntry.TreatData;

        internal static void Initialize(IMod ModInstance)
        {
            Helper = ModInstance.Helper;
            Monitor = ModInstance.Monitor;
            score = 0;

            SpaceEvents.BeforeGiftGiven += ReceiveTreat;
        }

        private static void ReceiveTreat(object sender, EventArgsBeforeReceiveObject e)
        {
            if (!(Game1.currentSeason == "fall" && Game1.dayOfMonth == 27) && e.Gift.HasContextTag("halloween_treat"))
            {
                Game1.activeClickableMenu = new DialogueBox(Helper.Translation.Get("not_halloween"));
                return;
            }

            StardewValley.Object gift = e.Gift;
            Farmer gifter = Game1.player;
            NPC giftee = e.Npc;

            if (!NPCData.ContainsKey(giftee.Name))
            {
                Log.Info($"NPC {giftee.Name} has no Trick-or-Treat data.");
                return;
            }

            if (giftee.modData.ContainsKey("ToT.given_treat") && giftee.modData["ToT.given_treat"] == "true")
            {
                Game1.activeClickableMenu = new DialogueBox(Helper.Translation.Get("already_given"));
                return;
            }

            string response_key = "";
            bool pull_prank = false;
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
                    if (NPCData[giftee.Name].Roles.Contains("prankster"))
                        pull_prank = true;
                    break;
                default:
                    score++;
                    gifter.changeFriendship(neutral_pts, giftee);
                    response_key = giftee.Dialogue.ContainsKey("neutral_treat") ? "neutral_treat" : "generic.neutral_treat";
                    break;
            }
            e.Cancel = true;
            giftee.modData.Add("ToT.given_treat", "true");
            gifter.reduceActiveItemByOne();
            gifter.currentLocation.localSound("give_gift");
            Dialogue dialogue;
            if (response_key.Split(".")[0] == "generic" || string.IsNullOrWhiteSpace(response_key))
            {
                dialogue = new Dialogue(Helper.Translation.Get(response_key), giftee);
            }
            else
            {
                dialogue = new Dialogue(response_key, giftee);
            }
            Functions.ClearAndPushDialogue(giftee, dialogue);

            if (pull_prank)
                InstantPrank(giftee);
        }

        private static bool CanSteal(Item item)
        {
            return item is not null && item is not Tool && !item.HasContextTag("halloween_treat") && Utility.IsNormalObjectAtParentSheetIndex(item, item.ParentSheetIndex);
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

        private static void InstantPrank(NPC npc)
        {
            Farmer farmer = Game1.player;
            Random random = new();

            var before_prank = npc.Dialogue.ContainsKey("before_prank") ? new Dialogue("before_prank", npc) : new Dialogue(Helper.Translation.Get("generic.before_prank"), npc);
            Functions.ClearAndPushDialogue(npc, before_prank);

            Game1.afterDialogues = delegate
            {
                var after_prank = npc.Dialogue.ContainsKey("after_prank") ? new Dialogue("after_prank", npc) : new Dialogue(Helper.Translation.Get("generic.after_prank"), npc);
                if (npc.Gender > 0)
                {
                    farmer.changeSkinColor(random.Next(17, 23), true);
                    farmer.currentLocation.localSound("slimedead");
                }
                else
                {
                    for (int i = 0; i < farmer.MaxItems; i++)
                    {
                        int idx = random.Next(farmer.MaxItems);
                        Item current_item = farmer.Items[idx];
                        if (CanSteal(current_item))
                        {
                            farmer.Items.RemoveAt(idx);
                            var dud_item = new StardewValley.Object(ModEntry.JA.GetObjectId("ToT.rotten-egg"), 1);
                            farmer.Items.Insert(idx, dud_item);
                            farmer.currentLocation.localSound("slimedead");
                            break;
                        }
                        if (i == farmer.MaxItems-1)
                            after_prank = new Dialogue(Helper.Translation.Get("generic.cannot_prank"), npc);
                    }
                }
                DelayedAction.functionAfterDelay(
                    () =>
                    {
                        Functions.ClearAndPushDialogue(npc, after_prank);
                    },
                    1000 // delay in milliseconds
                );
            };
        }
    }
}
