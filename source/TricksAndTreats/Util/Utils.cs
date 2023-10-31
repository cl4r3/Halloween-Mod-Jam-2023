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
    }

}
