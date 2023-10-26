using System;
using StardewValley;
using StardewModdingAPI;
using SpaceCore.Events;

namespace TrickOrTreat
{
    internal static class GiveTreat
    {
        public static int score;

        static IModHelper Helper;
        static IMonitor Monitor;

        internal static void Initialize(IMod ModInstance)
        {
            Helper = ModInstance.Helper;
            Monitor = ModInstance.Monitor;
            score = 0;

            SpaceEvents.BeforeGiftGiven += ReceiveTreat;
        }

        private static void ReceiveTreat(object sender, EventArgsBeforeReceiveObject e)
        {
            if (!(Game1.currentSeason == "fall" && Game1.dayOfMonth == 27) || !e.Gift.HasContextTag("halloween_treat"))
                return;

            StardewValley.Object gift = e.Gift;
            Farmer gifter = Game1.player;
            NPC giftee = e.Npc;

            if (!giftee.modData.ContainsKey("ToT.given_treat") || giftee.modData["ToT.given_treat"] == "false")
            {
                string response_key = "";
                bool pull_prank = false;
                int gift_taste = giftee.getGiftTasteForThisItem(gift);
                switch (gift_taste)
                {
                    case NPC.gift_taste_like:
                    case NPC.gift_taste_love:
                        score += 2;
                        gifter.changeFriendship(160, giftee);
                        response_key = giftee.Dialogue.ContainsKey("loved_treat") ? "loved_treat" : "generic.loved_treat";
                        break;
                    case NPC.gift_taste_dislike:
                    case NPC.gift_taste_hate:
                        score -= 2;
                        pull_prank = true;
                        gifter.changeFriendship(-80, giftee);
                        response_key = giftee.Dialogue.ContainsKey("hated_treat") ? "hated_treat" : "generic.hated_treat";
                        break;
                    default:
                        score++;
                        gifter.changeFriendship(90, giftee);
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
                Utils.NonStackDialogue(giftee, dialogue);

                if (pull_prank)
                    InstantPrank(giftee);
            }
        }

        private static void InstantPrank(NPC npc)
        {
            Farmer farmer = Game1.player;
            Random random = new();

            var before_prank = npc.Dialogue.ContainsKey("before_prank") ? new Dialogue("before_prank", npc) : new Dialogue(Helper.Translation.Get("generic.before_prank"), npc);
            Utils.NonStackDialogue(npc, before_prank);

            if (npc.Gender > 0)
            {
                farmer.changeSkinColor(random.Next(17, 23), true);
            }
            else
            {
                bool item_found = false;
                while (!item_found)
                {
                    int idx = random.Next(farmer.MaxItems);
                    Item current_item = farmer.Items[idx];
                    if (current_item is null || current_item is Tool || !Utility.IsNormalObjectAtParentSheetIndex(current_item, current_item.ParentSheetIndex))
                        continue;
                    farmer.Items.RemoveAt(idx);
                    var dud_item = new StardewValley.Object(ModEntry.JA.GetObjectId("ToT.rotten-egg"), 1);
                    farmer.Items.Insert(idx, dud_item);
                    item_found = true;
                }
            }
            farmer.currentLocation.localSound("slimedead");
            var after_prank = npc.Dialogue.ContainsKey("after_prank") ? new Dialogue("after_prank", npc) : new Dialogue(Helper.Translation.Get("generic.after_prank"), npc);
            Utils.NonStackDialogue(npc, after_prank);
        }
    }
}
