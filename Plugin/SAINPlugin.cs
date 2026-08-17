using BepInEx;
using BepInEx.Configuration;
using EFT;
using SAIN.Editor;
using SAIN.Helpers;
using SAIN.Patches.Components;
using SAIN.Patches.Hearing;
using SAIN.Plugin;
using SAIN.Preset;
using SAIN.Preset.GlobalSettings;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static SAIN.AssemblyInfoClass;

namespace SAIN
{
    [BepInPlugin(SAINGUID, SAINName, SAINVersion)]
    [BepInDependency(BigBrainGUID, BigBrainVersion)]
    //[BepInDependency(SPTGUID, SPTVersion)]
    [BepInProcess(EscapeFromTarkov)]
    [BepInIncompatibility("com.dvize.BushNoESP")]
    [BepInIncompatibility("com.dvize.NoGrenadeESP")]
    public class SAINPlugin : BaseUnityPlugin
    {
        public static DebugSettings DebugSettings => LoadedPreset.GlobalSettings.General.Debug;
        public static bool DebugMode => DebugSettings.Logs.GlobalDebugMode;
        public static bool ProfilingMode => DebugSettings.Logs.GlobalProfilingToggle;
        public static bool DrawDebugGizmos => DebugSettings.Gizmos.DrawDebugGizmos;
        public static PresetEditorDefaults EditorDefaults => PresetHandler.EditorDefaults;

        public static ECombatDecision ForceSoloDecision = ECombatDecision.None;

        public static ESquadDecision ForceSquadDecision = ESquadDecision.None;

        public static ESelfActionType ForceSelfDecision = ESelfActionType.None;

        public void Awake()
        {
            ResetStartupLog();
            WriteStartupLog("Awake entered.");
            try
            {
                BindConfigs();
                WriteStartupLog("Config bound.");

                /*
                if (!VersionChecker.CheckEftVersion(Logger, Info, Config))
                {
                    throw new Exception("Invalid EFT Version");
                }
                */

                PresetHandler.Init();
                WriteStartupLog("PresetHandler initialized.");
                InitPatches();
                WriteStartupLog("Harmony patches enabled.");
                BigBrainHandler.Init();
                WriteStartupLog("BigBrain handler initialized.");
                Vector.Init();
                WriteStartupLog("SAIN startup complete.");
            }
            catch (Exception ex)
            {
                WriteStartupLog($"SAIN startup failed: {ex}");
                Logger.LogError(ex);
                throw;
            }
        }

        private static void ResetStartupLog()
        {
            try
            {
                string pluginFolder = Path.GetDirectoryName(typeof(SAINPlugin).Assembly.Location);
                File.WriteAllText(Path.Combine(pluginFolder, "SAIN-startup.log"), string.Empty);
            }
            catch
            {
            }
        }

        public static void WriteStartupLog(string message)
        {
            try
            {
                string pluginFolder = Path.GetDirectoryName(typeof(SAINPlugin).Assembly.Location);
                File.AppendAllText(Path.Combine(pluginFolder, "SAIN-startup.log"), $"[{DateTime.Now:O}] {message}{Environment.NewLine}");
            }
            catch
            {
            }
        }

        private void BindConfigs()
        {
            string category = "SAIN Editor";
            OpenEditorButton = Config.Bind(category, "Open Editor", false, "Opens the Editor on press");
            OpenEditorConfigEntry = Config.Bind(category, "Open Editor Shortcut", new KeyboardShortcut(KeyCode.F6), "The keyboard shortcut that toggles editor");
        }

        public static ConfigEntry<bool> OpenEditorButton { get; private set; }

        public static ConfigEntry<KeyboardShortcut> OpenEditorConfigEntry { get; private set; }

        private List<ModulePatch> SainPatches => [

            new Patches.Components.WorldTickPatch(),
            new Patches.Components.PlayerLateUpdatePatch(),
            new Patches.Components.AddBotComponentPatch(),
            new Patches.Components.ActivateBotComponentPatch(),
            new Patches.Components.AddGameWorldPatch(),
            new Patches.Components.GetBotController(),
            new Patches.Components.DisableBotUpdateByUnityPatch(),

            new Patches.Vision.GlobalLookSettingsPatch(),
            new Patches.Vision.NoAIESPPatch(),
            new Patches.Vision.CheckFlashlightPatch(),
            new Patches.Vision.DisableLookUpdatePatch(),

            new Patches.Shoot.Aim.DisableMalfunctionPatch(),
            new Patches.Shoot.Aim.PlayerHitReactionDisablePatch(),
            new Patches.Shoot.Aim.BotSteeringPitchLimitPatch(),
            new Patches.Shoot.Aim.AimOffsetPatch(),
            new Patches.Shoot.Aim.AimTimePatch(),
            new Patches.Shoot.Aim.SmoothTurnPatch(),

            new Patches.Movement.MovementContextIsAIPatch(),
            new Patches.Movement.CanBeSnappedPatch(),
            new Patches.Movement.GlobalShootSettingsPatch(),
            new Patches.Movement.StopShootCauseAnimatorPatch(),
            new Patches.Movement.PoseStaminaPatch(),
            new Patches.Movement.AimStaminaPatch(),

            new Patches.Hearing.OnMakingShotPatch(),
            new Patches.Hearing.RegisterShotPatch(),
            new Patches.Hearing.OnWeaponModifiedPatch(),
            new Patches.Hearing.HearingSensorPatch(),
            new Patches.Hearing.BulletImpactPatch(),
            new Patches.Hearing.GrenadeCollisionPatch(),

            new Patches.Talk.PlayerHurtPatch(),
            new Patches.Talk.PlayerTalkPatch(),
            new Patches.Talk.BotTalkPatch(),
            new Patches.Talk.BotTalkManualUpdatePatch(),

            new Patches.Shoot.Grenades.SetGrenadePatch(),
            new Patches.Shoot.Grenades.ResetGrenadePatch(),
            new Patches.Shoot.RateOfFire.BotShootPatch(),
        ];

        private void InitPatches()
        {
            foreach (var patch in SainPatches)
            {
                patch.Enable();
            }
        }

        public static SAINPresetClass LoadedPreset => PresetHandler.LoadedPreset;

        public void Update()
        {
            ModDetection.ManualUpdate();
            SAINEditor.ManualUpdate();
            DebugGizmos.ManualUpdate();
        }

        public void Start() => SAINEditor.Init();

        public void LateUpdate() => SAINEditor.LateUpdate();

        public void OnGUI() => SAINEditor.OnGUI();
    }
}
