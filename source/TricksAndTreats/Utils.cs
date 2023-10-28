using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Menus;
using StardewModdingAPI;
using static TricksAndTreats.ModEntry;

namespace TricksAndTreats
{
    internal static class Log
    {
        internal static void Error(string msg) => ModMonitor.Log(msg, StardewModdingAPI.LogLevel.Error);
        internal static void Alert(string msg) => ModMonitor.Log(msg, StardewModdingAPI.LogLevel.Alert);
        internal static void Warn(string msg) => ModMonitor.Log(msg, StardewModdingAPI.LogLevel.Warn);
        internal static void Info(string msg) => ModMonitor.Log(msg, StardewModdingAPI.LogLevel.Info);
        internal static void Debug(string msg) => ModMonitor.Log(msg, StardewModdingAPI.LogLevel.Debug);
        internal static void Trace(string msg) => ModMonitor.Log(msg, StardewModdingAPI.LogLevel.Trace);
        internal static void Verbose(string msg) => ModMonitor.VerboseLog(msg);

    }

    internal class Utils
    {
        public static void ClearAndPushDialogue(NPC npc, string key, int item_id = -1)
        {
            string gift = "";
            if (item_id != -1)
            {
                gift = $" [{item_id}]";
            }
            npc.CurrentDialogue.Clear();
            if (!string.IsNullOrWhiteSpace(key) && npc.Dialogue.TryGetValue(key, out string dialogue))
                npc.CurrentDialogue.Push(new Dialogue(dialogue + gift, npc));
            else
                npc.CurrentDialogue.Push(new Dialogue(Helper.Translation.Get("generic." + key + gift), npc));
            Game1.drawDialogue(npc);
        }

        internal static void ValidateNPCData()
        {
            foreach (KeyValuePair<string, Celebrant> entry in NPCData)
            {
                if (Game1.getCharacterFromName(entry.Key, false, false) is null)
                {
                    Log.Warn($"Entry {entry.Key} in Trick-or-Treat NPC Data does not appear to be a valid NPC.");
                    NPCData.Remove(entry.Key);
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
                    NPCData[entry.Key].TreatsToGive = Array.Empty<string>().Append("TaT.candy-corn").ToArray();
                }

                if (entry.Value.PreferredTricks is not null)
                {
                    if (!NPCData[entry.Key].Roles.Contains("trickster") && entry.Value.PreferredTricks.Length > 0)
                    {
                        Log.Warn($"NPC {entry.Key} has preferred tricks listed even though they do not have the role \"trickster\", meaning they do not pull tricks.");
                    }
                    else
                    {
                        var tricks = Array.ConvertAll(entry.Value.PreferredTricks, d => d.ToLower());
                        if (tricks.Except(ValidTricks).ToArray().Length > 0)
                        {
                            Log.Warn($"NPC {entry.Key} has invalid trick type listed: " + tricks.Except(ValidTricks).ToList());
                        }
                        NPCData[entry.Key].PreferredTricks = tricks;
                    }
                }
                else if (NPCData[entry.Key].Roles.Contains("trickster"))
                {
                    NPCData[entry.Key].PreferredTricks = Array.Empty<string>().Append("all").ToArray();
                }
            }
        }

        internal static void ValidateCostumeData()
        {
            foreach (KeyValuePair<string, Costume> entry in CostumeData)
            {
                int count = 0;
                if (entry.Value is null)
                {
                    Log.Warn($"Could not find any data for costume set {entry.Key}.");
                    CostumeData.Remove(entry.Key);
                    return;
                }
                if (entry.Value.Hat is not null && entry.Value.Hat.Length > 0)
                {
                    CostumeData[entry.Key].HatId = JA.GetHatId(entry.Value.Hat);
                    if (CostumeData[entry.Key].HatId is null)
                    {
                        Log.Warn($"Could not find hat named {entry.Value.Hat} for costume set {entry.Key}.");
                        CostumeData.Remove(entry.Key);
                        continue;
                    }
                    count++;
                }
                if (entry.Value.Top is not null && entry.Value.Top.Length > 0)
                {
                    CostumeData[entry.Key].TopId = JA.GetClothingId(entry.Value.Hat);
                    if (CostumeData[entry.Key].TopId is null)
                    {
                        Log.Warn($"Could not find top named {entry.Value.Top} for costume set {entry.Key}.");
                        CostumeData.Remove(entry.Key);
                        continue;
                    }
                    count++;
                }
                if (entry.Value.Bot is not null && entry.Value.Bot.Length > 0)
                {
                    CostumeData[entry.Key].BotId = JA.GetClothingId(entry.Value.Bot);
                    if (CostumeData[entry.Key].BotId is null)
                    {
                        Log.Warn($"Could not find top named {entry.Value.Bot} for costume set {entry.Key}.");
                        CostumeData.Remove(entry.Key);
                        continue;
                    }
                    count++;
                }
                CostumeData[entry.Key].NumPieces = count;
            }
        }

        internal static void ValidateTreatData()
        {
            foreach (string name in TreatData.Keys)
            {
                TreatData[name].ObjectId = JA.GetObjectId(name);
                if (name is null)
                {
                    Log.Warn($"Could not find treat {name} among valid objects.");
                    TreatData.Remove(name);
                }
            }
        }
    }

}
