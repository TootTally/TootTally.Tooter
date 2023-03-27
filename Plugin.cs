using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using TootTally.Utils;

namespace TootTally.Tooter
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin, ITootTallyModule
    {
        public static Plugin Instance;
        public string Name { get => PluginInfo.PLUGIN_NAME; set => Name = value; }
        public bool IsConfigInitialized { get; set; }
        public ConfigEntry<bool> ModuleConfigEnabled { get; set; }

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

            string targetMapsPath = Path.Combine(Paths.BepInExRootPath, "CustomSongs");
            string sourceMapsPath = Path.Combine(Path.GetDirectoryName(Plugin.Instance.Info.Location), "CustomSongs");
            if (Directory.Exists(sourceMapsPath))
            {
                LogInfo("CustomSongs folder found. Attempting to move songs from\n     " + sourceMapsPath + " to\n     " + targetMapsPath);
                songFolderNames.ForEach(path =>
                {
                    if (!Directory.Exists(Path.Combine(targetMapsPath, path)))
                    {
                        Directory.Move(Path.Combine(sourceMapsPath, path), Path.Combine(targetMapsPath, path));
                        LogInfo($"Song {path} moved to custom songs folder");
                    }
                    else
                        LogInfo($"Song {path} already exists");
                });
                Directory.Delete(sourceMapsPath, true);
            }

            ModuleConfigEnabled = TootTally.Plugin.Instance.Config.Bind("Modules", "Tooter", true, "Enable TootTally's Dating Module");
            OptionalTrombSettings.Add(TootTally.Plugin.Instance.moduleSettings, ModuleConfigEnabled);
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
