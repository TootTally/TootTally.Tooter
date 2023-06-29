using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using TootTally.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;

namespace TootTally.Tooter
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin, ITootTallyModule
    {
        public static Plugin Instance;
        public string Name { get => PluginInfo.PLUGIN_NAME; set => Name = value; }
        public bool IsConfigInitialized { get; set; }
        public ConfigEntry<bool> ModuleConfigEnabled { get; set; }

        public ManualLogSource GetLogger => Logger;

        public void LogInfo(string msg) => Logger.LogInfo(msg);
        public void LogError(string msg) => Logger.LogError(msg);
        public List<string> songFolderNames = new List<string>
        {
            "Late_Night_Jazz_nerfed",
            "Let_Be_Yourself",
            "Love_Flip_nerfed",
            "Love_Has_No_End_nerfed",
            "Memories_Of_You",
            "Path_Of_Discovery_nerfed",
        };

        private void Awake()
        {
            if (Instance != null) return;
            Instance = this;

            ModuleConfigEnabled = TootTally.Plugin.Instance.Config.Bind("Modules", "Tooter", true, "Enable TootTally's Dating Module");
            TootTally.Plugin.AddModule(this);
        }

        public void Update() { }

        public void LoadModule()
        {
            TooterManager.OnModuleLoad();
            TooterAssetsManager.LoadAssets();
            Harmony.CreateAndPatchAll(typeof(TooterManager), PluginInfo.PLUGIN_GUID);
            LogInfo($"Module loaded!");
        }

        public void UnloadModule()
        {
            Harmony.UnpatchID(PluginInfo.PLUGIN_GUID);
            LogInfo($"Module unloaded!");
        }

    }
}
