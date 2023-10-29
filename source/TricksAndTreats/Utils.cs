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
        public static void Speak(NPC npc, string key, bool clear = true)
        {
            if (clear)
                npc.CurrentDialogue.Clear();
            if (!string.IsNullOrWhiteSpace(key) && npc.Dialogue.TryGetValue(key, out string dialogue))
                npc.CurrentDialogue.Push(new Dialogue(dialogue, npc));
            else
                npc.CurrentDialogue.Push(new Dialogue(Helper.Translation.Get("generic." + key), npc));
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
                    continue;
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
                    var treat = Helper.ModRegistry.IsLoaded("ch20youk.TaTPelicanTown.CP") ? "TaT.candy-corn" : "Maple Bar";
                    NPCData[entry.Key].TreatsToGive = Array.Empty<string>().Append(treat).ToArray();
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
                    continue;
                }
                if (entry.Value.Hat is not null && entry.Value.Hat.Length > 0)
                    count++;
                else { CostumeData[entry.Key].Hat = ""; } 
                if (entry.Value.Top is not null && entry.Value.Top.Length > 0)
                    count++;
                else { CostumeData[entry.Key].Top = ""; }
                if (entry.Value.Bottom is not null && entry.Value.Bottom.Length > 0)
                    count++;
                else { CostumeData[entry.Key].Bottom = ""; }

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
