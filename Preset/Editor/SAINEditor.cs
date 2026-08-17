using BepInEx;
using EFT.Console.Core;
using EFT.UI;
using SAIN.Editor.Util;
using SAIN.Plugin;
using SAIN.Preset;
using System;
using UnityEngine;
using static SAIN.Editor.RectLayout;
using static SAIN.Editor.SAINLayout;
using static SAIN.Editor.Sounds;
using ColorsClass = SAIN.Editor.Util.ColorsClass;

namespace SAIN.Editor
{
    public static class SAINEditor
    {
        static SAINEditor()
        {
            ConsoleScreen.Processor.RegisterCommand("saineditor", new Action(ToggleGUI));
        }

        public static void Init()
        {
            CursorSettings.InitCursor();
        }

        public static bool AdvancedBotConfigs => PresetHandler.EditorDefaults.AdvancedBotConfigs;

        [ConsoleCommand("Toggle SAIN GUI Editor")]
        private static void ToggleGUI()
        {
            if (DisplayingWindow)
            {
                CloseGUI();
            }
            else
            {
                OpenGUI();
            }
        }

        private static void OpenGUI()
        {
            DisplayingWindow = true;
            SAINPlugin.WriteStartupLog("Editor opened.");
        }

        private static void CloseGUI()
        {
            DisplayingWindow = false;
            SAINPlugin.OpenEditorButton.Value = false;
            SAINPlugin.OpenEditorButton.BoxedValue = false;
            _blockShortcutUntilReleased = true;
            GUIUtility.hotControl = 0;
            GUIUtility.keyboardControl = 0;
            SAINPlugin.WriteStartupLog("Editor closed.");
        }

        private static float CheckKeyLimiter;
        public static bool ShiftKeyPressed;
        public static bool CtrlKeyPressed;
        private static bool ToggleKeyPressed;
        private static bool EscapeKeyPressed;
        private static bool _blockShortcutUntilReleased;

        private static void CheckKeys()
        {
            if (CheckKeyLimiter < Time.time)
            {
                CheckKeyLimiter = Time.time + 0.1f;
                ShiftKeyPressed = Input.GetKey(KeyCode.LeftShift);
                CtrlKeyPressed = Input.GetKey(KeyCode.LeftControl);
                ToggleKeyPressed = Input.GetKeyDown(SAINPlugin.OpenEditorConfigEntry.Value.MainKey);
                EscapeKeyPressed = Input.GetKeyDown(KeyCode.Escape);
            }
        }

        public static void ManualUpdate()
        {
            if (DisplayingWindow)
            {
                CursorSettings.SetUnlockCursor(0, true);
            }
            else
            {
                CheckKeys();
            }

            if (_blockShortcutUntilReleased && !Input.GetKey(SAINPlugin.OpenEditorConfigEntry.Value.MainKey))
            {
                _blockShortcutUntilReleased = false;
            }

            bool shortcutRequested = SAINPlugin.OpenEditorConfigEntry.Value.IsDown() && !DisplayingWindow && !_blockShortcutUntilReleased;
            bool configRequested = SAINPlugin.OpenEditorButton.Value && !DisplayingWindow;

            if (shortcutRequested || configRequested)
            {
                if (SAINPlugin.OpenEditorButton.Value)
                {
                    SAINPlugin.OpenEditorButton.BoxedValue = false;
                    SAINPlugin.OpenEditorButton.Value = false;
                }
                OpenGUI();
            }
        }

        public static void LateUpdate()
        {
            if (DisplayingWindow) CursorSettings.SetUnlockCursor(0, true);
        }

        public static void OnGUI()
        {
            if (DisplayingWindow)
            {
                TraceMouseEvent("OnGUI");
                if (!CacheCreated)
                {
                    CacheCreated = true;
                    ColorsClass.CreateCache();
                    TexturesClass.CreateCache();
                    StylesClass.CreateCache();
                }

                MouseFunctions.OnGUI();
                CursorSettings.SetUnlockCursor(0, true);
                RectLayout.UpdateForScreen();
                MainWindow = GUI.ModalWindow(0, MainWindow, MainWindowFunc, "SAIN AI Settings Editor", GetStyle(Style.window));
                ConfigEditingTracker.Update();
                UnityInput.Current.ResetInputAxes();
            }
        }

        private static bool CacheCreated;

        private static void MainWindowFunc(int TWCWindowID)
        {
            GUI.FocusWindow(TWCWindowID);
            TraceMouseEvent("MainWindow");
            CheckKeys();
            if (ToggleKeyPressed || EscapeKeyPressed)
            {
                CloseGUI();
                return;
            }
            if (HandleTopBarMouseEarly())
            {
                return;
            }
            CreateDragBar();
            CreateTopBarOptions();
            EEditorTab selectedTab = EditTabsClass.TabSelectMenu(35f, 3f, 0.5f);
            float space = DragRect.height + EditTabsClass.TabMenuRect.height;
            Space(space);
            GUITabs.CreateTabs(selectedTab);
            MouseFunctions.OnGUI();
            DrawTooltip();
            ConsumeWindowMouseEvent();
        }

        private static bool HandleTopBarMouseEarly()
        {
            Event current = Event.current;
            if (current == null || current.button != 0)
            {
                return false;
            }

            bool mouseClick = current.rawType == EventType.MouseDown || current.type == EventType.MouseDown;
            if (!mouseClick)
            {
                return false;
            }

            Vector2 mouse = current.mousePosition;
            if (ExitRect.Contains(mouse))
            {
                SAINPlugin.WriteStartupLog($"Early topbar: close at {mouse}");
                CloseGUI();
                TryPlaySound(EUISoundType.MenuEscape);
                current.Use();
                return true;
            }

            if (SaveAllRect.Contains(mouse))
            {
                SAINPlugin.WriteStartupLog($"Early topbar: save at {mouse}");
                SAINPresetClass.ExportAll(SAINPlugin.LoadedPreset);
                TryPlaySound(EUISoundType.InsuranceInsured);
                current.Use();
                return true;
            }

            if (AdvRect.Contains(mouse))
            {
                SAINPlugin.WriteStartupLog($"Early topbar: advanced toggle at {mouse}");
                PresetHandler.EditorDefaults.AdvancedBotConfigs = !PresetHandler.EditorDefaults.AdvancedBotConfigs;
                PresetHandler.ExportEditorDefaults();
                TryPlaySound(EUISoundType.MenuEscape);
                current.Use();
                return true;
            }

            return false;
        }

        private static void CreateDragBar()
        {
            GUI.DrawTexture(DragRect, DragBackgroundTexture, ScaleMode.StretchToFill, true, 0);
            GUI.Box(DragRect, $"SAIN {AssemblyInfoClass.SAINVersion} GUI Editor | Preset: {SAINPlugin.LoadedPreset.Info.Name}", GetStyle(Style.dragBar));
            GUI.DragWindow(DragRect);
        }

        public static string ExceptionString = string.Empty;

        private static readonly GUIContent SaveContent = new("Save All Changes", $"Export All Changes to SAIN/Presets/{SAINPlugin.LoadedPreset.Info.Name}");

        private static void CreateTopBarOptions()
        {
            SaveContent.tooltip = ConfigEditingTracker.GetUnsavedValuesString();

            var style = GetStyle(Style.botTypeGrid);
            var oldAlignment = style.alignment;
            style.alignment = TextAnchor.MiddleCenter;

            bool advancedEnabled = PresetHandler.EditorDefaults.AdvancedBotConfigs;
            string status = advancedEnabled ? "ON" : "OFF";
            bool newValue = GUI.Toggle(AdvRect, advancedEnabled, $"Advanced Settings: [{status}]", GetStyle(Style.botTypeGrid));
            if (SAINLayout.RawMouseClickInside(AdvRect))
            {
                newValue = !advancedEnabled;
            }
            if (advancedEnabled != newValue)
            {
                PresetHandler.EditorDefaults.AdvancedBotConfigs = newValue;
                PresetHandler.ExportEditorDefaults();
                TryPlaySound(EUISoundType.MenuEscape);
            }

            if (GUI.Button(SaveAllRect, SaveContent, GetStyle(Style.botTypeGrid)) || SAINLayout.RawMouseClickInside(SaveAllRect))
            {
                SAINPresetClass.ExportAll(SAINPlugin.LoadedPreset);
                TryPlaySound(EUISoundType.InsuranceInsured);
            }

            if (GUI.Button(ExitRect, "X", GetStyle(Style.botTypeGrid)) || SAINLayout.RawMouseClickInside(ExitRect))
            {
                CloseGUI();
                TryPlaySound(EUISoundType.MenuEscape);
            }
            style.alignment = oldAlignment;
        }

        private static void TryPlaySound(EUISoundType sound)
        {
            try
            {
                PlaySound(sound);
            }
            catch (Exception ex)
            {
                SAINPlugin.WriteStartupLog($"PlaySound failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static void ConsumeWindowMouseEvent()
        {
            Event current = Event.current;
            if (current == null)
            {
                return;
            }

            if (current.rawType == EventType.MouseDown || current.rawType == EventType.MouseUp || current.rawType == EventType.MouseDrag)
            {
                current.Use();
            }
        }

        private static float _nextMouseTraceTime;

        private static void TraceMouseEvent(string source)
        {
            Event current = Event.current;
            if (current == null)
            {
                return;
            }

            bool interesting = current.type == EventType.MouseDown || current.type == EventType.MouseUp;
            if (!interesting && Time.realtimeSinceStartup < _nextMouseTraceTime)
            {
                return;
            }

            if (!interesting)
            {
                _nextMouseTraceTime = Time.realtimeSinceStartup + 2f;
            }

            SAINPlugin.WriteStartupLog(
                $"GUI {source}: type={current.type}, raw={current.rawType}, button={current.button}, mouse={current.mousePosition}, hot={GUIUtility.hotControl}, keyboard={GUIUtility.keyboardControl}");
        }

        private static void DrawTooltip()
        {
            if (string.IsNullOrEmpty(GUI.tooltip))
            {
                //var sb = new StringBuilder();
                //
                //sb.AppendLine(Event.current.rawType.ToString());
                //sb.AppendLine(Event.current.type.ToString());
                //sb.AppendLine(Event.current.control.ToString());
                //sb.AppendLine(Event.current.commandName.ToString());
                //GUI.tooltip = sb.ToString();
                return;
            }

            const int width = 250;
            var x = Event.current.mousePosition.x;
            var y = Event.current.mousePosition.y + 15;
            if (x > Screen.width / 3)
            {
                x -= width;
            }

            var ToolTipStyle = GetStyle(Style.tooltip);
            var height = ToolTipStyle.CalcHeight(new GUIContent(GUI.tooltip), width) + 10;
            GUI.Box(new Rect(x, y, width, height), GUI.tooltip, ToolTipStyle);
        }

        public static bool DisplayingWindow
        {
            get => CursorSettings.DisplayingWindow;
            set { CursorSettings.DisplayingWindow = value; }
        }

        public static Rect OpenTabRect = new(0, 0, MainWindow.width, 1000f);

        private static Texture2D DragBackgroundTexture => TexturesClass.GetTexture(EGraynessLevel.Mid);
    }
}
