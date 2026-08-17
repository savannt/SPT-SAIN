using SAIN.Plugin;
using UnityEngine;

namespace SAIN.Editor
{
    public static class RectLayout
    {
        public static Vector2 ScaledPivot => GetScaling();

        private static float ReferenceResX => 1920 * PresetHandler.EditorDefaults.ConfigScaling;
        private static float ReferenceResY => 1080 * PresetHandler.EditorDefaults.ConfigScaling;

        private static Vector2 GetScaling()
        {
            float scaling = Mathf.Min(Screen.width / ReferenceResX, Screen.height / ReferenceResY);
            return new Vector2(scaling, scaling);
        }

        public static Rect MainWindow = new(0, 0, Screen.width, Screen.height);

        private const float RectHeight = 30f;
        private const float ExitWidth = 30f;
        private const float SaveAllWidth = 175f;
        private const float AdvWidth = 225f;

        public static Rect ExitRect;
        public static Rect DragRect;
        public static Rect SaveAllRect;
        public static Rect AdvRect;

        public static void UpdateForScreen()
        {
            MainWindow = new Rect(0, 0, Screen.width, Screen.height);

            float exitStartX = MainWindow.width - ExitWidth;
            float saveAllStartX = exitStartX - SaveAllWidth - 5;
            float advRectStartX = saveAllStartX - AdvWidth - 5;
            float dragWidth = advRectStartX - 5;

            ExitRect = new Rect(exitStartX, 0, ExitWidth, RectHeight);
            DragRect = new Rect(0, 0, dragWidth, RectHeight);
            SaveAllRect = new Rect(saveAllStartX, 0, SaveAllWidth, RectHeight);
            AdvRect = new Rect(advRectStartX, 0, AdvWidth, RectHeight);
        }
    }
}
