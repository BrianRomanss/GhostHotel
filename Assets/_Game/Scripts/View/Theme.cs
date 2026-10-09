using ChuchuGames.UI;
using GhostHotel.Data;
using GhostHotel.Model;
using UnityEngine;
using UnityEngine.UI;

namespace GhostHotel.View
{
    /// <summary>
    /// Look and sound of the code-built UI (GDD §7–8): Lora for titles, Inter for UI, ledger-paper
    /// panels, brass buttons, and named art/audio from the ArtLibrary.
    /// </summary>
    public static class Theme
    {
        public static ArtLibrarySO Art { get; private set; }

        public static void Init(ArtLibrarySO art)
        {
            Art = art;
            if (art == null) return;
            if (art.sans != null) UiBuild.DefaultFont = art.sans;
            UiBuild.PanelSprite = art.Sprite("panel_9s");
            UiBuild.ButtonSprite = art.Sprite("button_9s");
            UiBuild.ClickSound = art.Clip("click");
        }

        public static Font Serif => Art != null && Art.serif != null ? Art.serif : UiBuild.DefaultFont;

        /// <summary>Bookish serif heading (titles and names).</summary>
        public static Text Title(string name, Transform parent, string text, int size, Color color,
            TextAnchor anchor = TextAnchor.UpperLeft)
        {
            var t = UiBuild.Label(name, parent, text, size, color, anchor, FontStyle.Bold);
            t.font = Serif;
            return t;
        }

        /// <summary>Full-screen background that keeps its aspect (crops instead of stretching).</summary>
        public static Image Background(RectTransform parent, string sprite, Color? fallback = null)
        {
            var holder = UiBuild.Panel("BgHolder", parent, fallback ?? Palette.NightNavy, raycastTarget: true, rounded: false);
            holder.rectTransform.Fill();
            var s = Sprite(sprite);
            if (s == null) return holder;
            var img = UiBuild.Picture("Background", holder.transform, s);
            img.preserveAspect = false;
            var fitter = img.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = s.rect.width / s.rect.height;
            Atmosphere(holder.rectTransform);
            return img;
        }

        /// <summary>Drifting floor fog plus a vignette over a full-screen area (haunted, never bright).</summary>
        public static void Atmosphere(RectTransform area, float fogAlpha = 0.38f)
        {
            var fog = UiBuild.Picture("Fog", area, Sprite("fog"));
            if (fog.sprite != null)
            {
                fog.preserveAspect = false;
                var rt = fog.rectTransform;
                rt.anchorMin = new Vector2(-0.1f, 0f);
                rt.anchorMax = new Vector2(1.1f, 0.32f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                fog.color = new Color(1, 1, 1, fogAlpha);
                var d = fog.gameObject.AddComponent<ChuchuGames.UI.Drift>();
                d.distance = 80f;
                d.period = 30f;
            }
            var vignette = UiBuild.Picture("Vignette", area, Sprite("vignette"));
            if (vignette.sprite != null)
            {
                vignette.preserveAspect = false;
                vignette.rectTransform.Fill();
            }
        }

        public static Sprite Sprite(string name) => Art != null ? Art.Sprite(name) : null;

        public static Sprite Ghost(GuestDef def, string expression = "neutral") => Sprite($"ghost_{def.ArtKey}_{expression}");
        public static Sprite Portrait(GuestDef def, string expression = "neutral") => Sprite($"portrait_{def.ArtKey}_{expression}");
        public static Sprite Portrait(string artKey, string expression) => Sprite($"portrait_{artKey}_{expression}");

        /// <summary>Art keys for story characters who never check in.</summary>
        public static string CastArtKey(string speaker)
        {
            switch (speaker)
            {
                case "Mr. Vane":
                case "Vane": return "Vane";
                case "Edith": return "Edith";
                default: return null;
            }
        }

        /// <summary>Expression that matches how happy a guest is right now.</summary>
        public static string ExpressionFor(int stars) => stars >= 3 ? "happy" : stars <= 1 ? "sad" : "neutral";

        public static void Sfx(string name, float volume = 1f, float pitch = 1f)
        {
            if (Art != null) AudioPlayer.Play(Art.Clip(name), volume, pitch);
        }

        public static void Music(string name) => AudioPlayer.PlayMusic(Art != null && name != null ? Art.Clip(name) : null);

        /// <summary>Cream "ledger paper" panel with a brass edge.</summary>
        public static Image Paper(string name, Transform parent)
        {
            var edge = UiBuild.Panel(name, parent, Palette.Brass);
            var inner = UiBuild.Panel("Paper", edge.transform, Palette.PaperCream);
            inner.rectTransform.Fill(4);
            return edge;
        }

        public static Button PrimaryButton(string name, Transform parent, string label, System.Action onClick, int size = 32) =>
            UiBuild.Button(name, parent, label, Palette.LampAmber, Palette.Ink, onClick, size);

        /// <summary>Cream button for dark backgrounds (action bar, lobby).</summary>
        public static Button SecondaryButton(string name, Transform parent, string label, System.Action onClick, int size = 28) =>
            UiBuild.Button(name, parent, label, Palette.PaperCream, Palette.Ink, onClick, size);

        /// <summary>Button for cream paper panels: warm tan with a brass edge so it never vanishes into the paper.</summary>
        public static Button PaperButton(string name, Transform parent, string label, System.Action onClick, int size = 28, Color? fill = null)
        {
            var b = UiBuild.Button(name, parent, label, Palette.Brass, Palette.Ink, onClick, size);
            var inner = UiBuild.Panel("Fill", b.transform, fill ?? PaperButtonFill, raycastTarget: false);
            inner.rectTransform.Fill(3);
            inner.transform.SetSiblingIndex(0);
            return b;
        }

        public static readonly Color PaperButtonFill = new Color(0.93f, 0.85f, 0.68f);
    }
}
