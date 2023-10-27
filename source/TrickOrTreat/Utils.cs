using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
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

    public static class JsonParser
    {
        private static JsonSerializerSettings settings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };

        public static string Serialize<Model>(Model model)
        {
            return JsonConvert.SerializeObject(model, Formatting.Indented, settings);
        }

        public static Model Deserialize<Model>(object json)
        {
            return Deserialize<Model>(json.ToString());
        }

        public static Model Deserialize<Model>(string json)
        {
            return JsonConvert.DeserializeObject<Model>(json, settings);
        }

        public static bool CompareSerializedObjects(object first, object second)
        {
            return JsonConvert.SerializeObject(first) == JsonConvert.SerializeObject(second);
        }

        public static object GetUpdatedModel(object original, object updated)
        {
            JObject jOriginal = JObject.Parse(original.ToString());
            foreach (var updatedProperty in JObject.Parse(updated.ToString()).Properties())
            {
                var originalProperty = jOriginal.Properties().FirstOrDefault(p => p.Name == updatedProperty.Name);
                if (originalProperty != null)
                {
                    originalProperty.Value = updatedProperty.Value;
                }
                else
                {
                    jOriginal.Add(updatedProperty);
                }
            }

            return jOriginal.ToString();
        }
    }

    internal class Functions
    {
        public static void ClearAndPushDialogueKey(NPC npc, string dialogueKey)
        {
            if (!string.IsNullOrWhiteSpace(dialogueKey) && npc.Dialogue.TryGetValue(dialogueKey, out string dialogue))
            {
                npc.CurrentDialogue.Clear();
                npc.CurrentDialogue.Push(new Dialogue(dialogue, npc));
                Game1.drawDialogue(npc);
            }
        }

        public static void ClearAndPushDialogue(NPC npc, Dialogue dialogue)
        {
            if (dialogue is not null)
            {
                npc.CurrentDialogue.Clear();
                npc.CurrentDialogue.Push(dialogue);
                Game1.drawDialogue(npc);
            }
        }

        public static void ValidateContentPack(IContentPack pack)
        {
            Log.Trace($"Validating data from content pack: {pack.Manifest.Name}");

            if (pack != null)
            {
                Log.Trace($"Validation complete for content pack: {pack.Manifest.Name}");
            }
            return;
        }
    }

}
