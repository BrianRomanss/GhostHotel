using ChuchuGames.UI;
using UnityEngine;

namespace GhostHotel.View
{
    /// <summary>Player settings (GDD §5.2 Settings), stored per device in PlayerPrefs and applied immediately.</summary>
    public static class GameSettings
    {
        const string Prefix = "gh.settings.";

        public static float Master { get => Get("master", 1f); set => Set("master", value); }
        public static float SfxVolume { get => Get("sfx", 1f); set => Set("sfx", value); }
        public static float MusicVolume { get => Get("music", 0.6f); set => Set("music", value); }
        public static bool ReduceMotion { get => Get("reduceMotion", 0f) > 0.5f; set => Set("reduceMotion", value ? 1f : 0f); }
        public static bool ShowHints { get => Get("hints", 1f) > 0.5f; set => Set("hints", value ? 1f : 0f); }
        /// <summary>Text size multiplier, 0.9–1.3 (GDD §5.2 Accessibility: text size).</summary>
        public static float TextScale { get => Mathf.Clamp(Get("textScale", 1f), 0.9f, 1.3f); set => Set("textScale", Mathf.Clamp(value, 0.9f, 1.3f)); }

        public static void Apply()
        {
            AudioPlayer.Master = Master;
            AudioPlayer.Sfx = SfxVolume;
            AudioPlayer.Music = MusicVolume;
            AudioPlayer.ApplyMusicVolume();
            Tween.ReduceMotion = ReduceMotion;
            UiBuild.TextScale = TextScale;
        }

        static float Get(string key, float fallback)
        {
            try { return PlayerPrefs.GetFloat(Prefix + key, fallback); }
            catch { return fallback; }
        }

        static void Set(string key, float value)
        {
            PlayerPrefs.SetFloat(Prefix + key, value);
            PlayerPrefs.Save();
            Apply();
        }
    }
}
