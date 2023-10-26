using System;
using StardewValley;
using StardewValley.Menus;
using StardewModdingAPI;

namespace TrickOrTreat
{
    internal static class Log
    {
        internal static void Error(string msg) => ModEntry.ModMonitor.Log(msg, StardewModdingAPI.LogLevel.Error);
        internal static void Alert(string msg) => ModEntry.ModMonitor.Log(msg, StardewModdingAPI.LogLevel.Alert);
        internal static void Warn(string msg) => ModEntry.ModMonitor.Log(msg, StardewModdingAPI.LogLevel.Warn);
        internal static void Info(string msg) => ModEntry.ModMonitor.Log(msg, StardewModdingAPI.LogLevel.Info);
        internal static void Debug(string msg) => ModEntry.ModMonitor.Log(msg, StardewModdingAPI.LogLevel.Debug);
        internal static void Trace(string msg) => ModEntry.ModMonitor.Log(msg, StardewModdingAPI.LogLevel.Trace);
        internal static void Verbose(string msg) => ModEntry.ModMonitor.VerboseLog(msg);

    }

    // For multiplayer messages
    internal class BroadcastMsg
    {
        public string type;
        public string action;
        public string id;
    }

    internal class Utils
    {
        internal static void NonStackDialogue(NPC npc, Dialogue dialogue)
        {
            Game1.activeClickableMenu = new DialogueBox(dialogue);
            Game1.dialogueUp = true;
            if (!Game1.eventUp)
            {
                Game1.player.Halt();
                Game1.player.CanMove = false;
            }
            if (npc != null)
            {
                Game1.currentSpeaker = npc;
            }
        }
    }  

}
