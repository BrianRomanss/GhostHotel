using System;
using ChuchuGames.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GhostHotel.View
{
    /// <summary>Modal overlays (GDD §5: Settings and Pause open from anywhere; destructive actions confirm).</summary>
    public static class Overlays
    {
        /// <summary>Dimmed full-screen blocker with a centred paper panel. Returns (root, panel).</summary>
        public static (GameObject root, RectTransform panel) Modal(RectTransform parent, float width, float height, string title)
        {
            var root = UiBuild.Panel("Modal", parent, new Color(0, 0, 0, 0.6f), raycastTarget: true, rounded: false).rectTransform.Fill();
            root.SetAsLastSibling();
            var panel = Theme.Paper("Panel", root).rectTransform.Centre(width, height);
            Theme.Title("Title", panel, title, 48, Palette.Ink, TextAnchor.UpperCenter).rectTransform.PlaceTopLeft(0, 28, width, 64);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            Tween.Fade(group, 0, 1, 0.15f);
            Tween.Scale(panel, Vector3.one * 0.92f, Vector3.one, 0.25f);
            return (root.gameObject, panel);
        }

        public static void Confirm(RectTransform parent, string question, string yes, Action onYes)
        {
            var (root, panel) = Modal(parent, 760, 360, "Are you sure?");
            UiBuild.Label("Q", panel, question, 30, Palette.Ink).rectTransform.PlaceTopLeft(40, 110, 680, 100);
            var no = Theme.PaperButton("No", panel, "Cancel", () => UnityEngine.Object.Destroy(root));
            ((RectTransform)no.transform).PlaceTopLeft(60, 240, 300, 80);
            var ok = Theme.PrimaryButton("Yes", panel, yes, () =>
            {
                UnityEngine.Object.Destroy(root);
                onYes();
            });
            ((RectTransform)ok.transform).PlaceTopLeft(400, 240, 300, 80);
        }

        public static GameObject Settings(RectTransform parent, Action onClose = null)
        {
            var (root, panel) = Modal(parent, 900, 820, "Settings");
            float y = 120;
            void Volume(string label, float value, Action<float> set)
            {
                UiBuild.Label(label, panel, label, 30, Palette.Ink, TextAnchor.MiddleLeft).rectTransform.PlaceTopLeft(60, y, 260, 60);
                var s = UiBuild.Slider(label + "Slider", panel, value, Palette.DuskViolet.WithAlpha(0.35f), Palette.LampAmber, Palette.Brass, set);
                ((RectTransform)s.transform).PlaceTopLeft(330, y, 500, 60);
                y += 90;
            }
            void Toggle(string label, Func<bool> get, Action<bool> set)
            {
                UiBuild.Label(label, panel, label, 30, Palette.Ink, TextAnchor.MiddleLeft).rectTransform.PlaceTopLeft(60, y, 420, 60);
                Button b = null;
                b = Theme.PaperButton(label + "Toggle", panel, get() ? "On" : "Off", () =>
                {
                    set(!get());
                    b.GetComponentInChildren<Text>().text = get() ? "On" : "Off";
                });
                ((RectTransform)b.transform).PlaceTopLeft(630, y, 200, 60);
                y += 90;
            }

            Volume("Master", GameSettings.Master, v => GameSettings.Master = v);
            Volume("Effects", GameSettings.SfxVolume, v => GameSettings.SfxVolume = v);
            Volume("Music", GameSettings.MusicVolume, v => GameSettings.MusicVolume = v);
            UiBuild.Label("TextSize", panel, "Text size", 30, Palette.Ink, TextAnchor.MiddleLeft).rectTransform.PlaceTopLeft(60, y, 260, 60);
            var textSlider = UiBuild.Slider("TextSizeSlider", panel, (GameSettings.TextScale - 0.9f) / 0.4f,
                Palette.DuskViolet.WithAlpha(0.35f), Palette.LampAmber, Palette.Brass, v => GameSettings.TextScale = 0.9f + v * 0.4f);
            ((RectTransform)textSlider.transform).PlaceTopLeft(330, y, 500, 60);
            y += 90;
            Toggle("Reduce motion", () => GameSettings.ReduceMotion, v => GameSettings.ReduceMotion = v);
            Toggle("Hint button", () => GameSettings.ShowHints, v => GameSettings.ShowHints = v);

            var close = Theme.PrimaryButton("Close", panel, "Done", () =>
            {
                UnityEngine.Object.Destroy(root);
                onClose?.Invoke();
            });
            ((RectTransform)close.transform).PlaceTopLeft(300, 716, 300, 80);
            UiBuild.Label("Note", panel, "Text size applies to the next screen you open.", 20, Palette.Ink.WithAlpha(0.6f))
                .rectTransform.PlaceTopLeft(0, 670, 900, 40);
            return root;
        }

        public static GameObject Pause(RectTransform parent, Action onRestart, Action onQuitToLobby)
        {
            var (root, panel) = Modal(parent, 640, 640, "Paused");
            float y = 120;
            void Add(string label, Action a, bool primary = false)
            {
                var b = primary ? Theme.PrimaryButton(label, panel, label, a) : Theme.PaperButton(label, panel, label, a);
                ((RectTransform)b.transform).PlaceTopLeft(120, y, 400, 84);
                y += 104;
            }
            Add("Resume", () => UnityEngine.Object.Destroy(root), primary: true);
            Add("Restart night", () => Confirm(parent, "Start this night over? Your placements will be cleared.", "Restart", () =>
            {
                UnityEngine.Object.Destroy(root);
                onRestart();
            }));
            Add("Settings", () => Settings(parent));
            Add("Quit to Lobby", () => Confirm(parent, "Leave this night? You can play it again any time.", "Leave", () =>
            {
                UnityEngine.Object.Destroy(root);
                onQuitToLobby();
            }));
            return root;
        }
    }
}
