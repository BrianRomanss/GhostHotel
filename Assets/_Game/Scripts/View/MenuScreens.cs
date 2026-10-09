using System;
using ChuchuGames.Core;
using ChuchuGames.UI;
using GhostHotel.Model;
using UnityEngine;
using UnityEngine.UI;

namespace GhostHotel.View
{
    /// <summary>Main Menu (GDD §5.2): hotel exterior at night; Continue is the largest button.</summary>
    public static class MainMenuScreen
    {
        public static GameObject Create(RectTransform parent, bool canContinue, string continueLabel,
            Action onContinue, Action onNewGame, Action onSettings, Action onQuit)
        {
            var root = UiBuild.Rect("MainMenu", parent).Fill();
            Theme.Background(root, "exterior");

            var title = Theme.Title("Title", root, "Ghost Hotel", 132, Palette.PaperCream);
            title.rectTransform.PlaceTopLeft(110, 90, 1000, 170);
            UiBuild.Label("Tagline", root, "A cozy-spooky hotel for the dead", 36, Palette.WarmGlow, TextAnchor.UpperLeft, FontStyle.Italic)
                .rectTransform.PlaceTopLeft(118, 250, 900, 50);

            float y = 360;
            Button Add(string name, string label, Action a, bool primary, float h = 84, float w = 460)
            {
                var b = primary ? Theme.PrimaryButton(name, root, label, a, 36) : Theme.SecondaryButton(name, root, label, a);
                ((RectTransform)b.transform).PlaceTopLeft(110, y, w, h);
                y += h + 20;
                return b;
            }

            if (canContinue) Add("Continue", continueLabel, onContinue, true, 110, 560);
            Add("NewGame", "New Game", onNewGame, !canContinue, canContinue ? 84 : 110, canContinue ? 460 : 560);
            var endless = Add("Endless", "Endless · unlocks in Act 3", null, false);
            endless.interactable = false;
            Add("Settings", "Settings", onSettings, false);
            if (!Application.isMobilePlatform) Add("Quit", "Quit", onQuit, false);

            UiBuild.Label("Version", root, $"Vertical slice · v{Application.version}", 20, Palette.PaperCream.WithAlpha(0.6f), TextAnchor.LowerRight)
                .rectTransform.Fill(24);

            var group = root.gameObject.AddComponent<CanvasGroup>();
            Tween.Fade(group, 0, 1, 0.6f);
            Tween.Move(title.rectTransform, new Vector2(110, -60), new Vector2(110, -90), 0.8f);
            return root.gameObject;
        }
    }

    /// <summary>Save Slots (GDD §5.2): three cards with night, act, play time and guests freed.</summary>
    public static class SaveSlotsPanel
    {
        public static GameObject Create(RectTransform parent, SaveSlots<GameProgress> saves, int nightCount, Action<int> onPlay)
        {
            var (root, panel) = Overlays.Modal(parent, 1400, 760, "Choose a save slot");
            for (int slot = 1; slot <= saves.SlotCount; slot++)
            {
                int s = slot;
                var data = saves.Load(slot);
                var card = UiBuild.Panel($"Slot{slot}", panel, data != null ? Palette.WarmGlow.WithAlpha(0.35f) : Palette.DuskViolet.WithAlpha(0.15f));
                card.rectTransform.PlaceTopLeft(50 + (slot - 1) * 440, 120, 420, 520);
                Theme.Title("Name", card.transform, $"Slot {slot}", 40, Palette.Ink, TextAnchor.UpperCenter).rectTransform.PlaceTopLeft(0, 24, 420, 56);

                string body;
                if (data == null) body = "Empty\n\nA new keeper,\na new beginning.";
                else
                {
                    var t = TimeSpan.FromSeconds(data.playSeconds);
                    var when = DateTime.TryParse(data.savedAtUtc, out var dt) ? dt.ToLocalTime().ToString("d MMM, HH:mm") : "";
                    string night = data.night > nightCount ? "Story complete" : $"Night {data.night}";
                    body = $"{night} · Act 1\n\nGuests moved on: {data.GuestsMovedOn}\nEctoplasm: {data.ectoplasm:N0}\nPlay time: {(int)t.TotalHours}h {t.Minutes:00}m\n\n<size=22>{when}</size>";
                }
                UiBuild.Label("Body", card.transform, body, 28, Palette.Ink, TextAnchor.UpperCenter).rectTransform.PlaceTopLeft(20, 100, 380, 300);

                var play = Theme.PrimaryButton("Play", card.transform, data == null ? "Start here" : "Play", () =>
                {
                    UnityEngine.Object.Destroy(root);
                    onPlay(s);
                });
                ((RectTransform)play.transform).PlaceTopLeft(40, 400, 340, 80);

                if (data != null)
                {
                    var del = Theme.PaperButton("Delete", card.transform, "<color=#A8323A>Erase</color>", null, 20);
                    ((RectTransform)del.transform).PlaceTopLeft(300, 24, 100, 44);
                    del.onClick.AddListener(() => Overlays.Confirm(parent, $"Erase slot {s}? This can't be undone.", "Erase", () =>
                    {
                        saves.Delete(s);
                        UnityEngine.Object.Destroy(root);
                        Create(parent, saves, nightCount, onPlay);
                    }));
                }
            }
            var close = Theme.PaperButton("Close", panel, "Back", () => UnityEngine.Object.Destroy(root));
            ((RectTransform)close.transform).PlaceTopLeft(560, 666, 280, 70);
            return root;
        }
    }
}
