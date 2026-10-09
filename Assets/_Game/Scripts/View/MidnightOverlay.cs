using System;
using System.Collections.Generic;
using ChuchuGames.UI;
using GhostHotel.Model;
using UnityEngine;
using UnityEngine.UI;

namespace GhostHotel.View
{
    /// <summary>
    /// Midnight Event (GDD §5.2): the clock strikes, each event card flips over, then the player
    /// chooses "Fix it (N swaps)" or "Let the night pass".
    /// </summary>
    public static class MidnightOverlay
    {
        public static GameObject Show(RectTransform parent, IReadOnlyList<MidnightEvent> events, int swapTokens,
            Action onFix, Action onPass)
        {
            Theme.Sfx("midnight", 0.9f);
            Shake.Play(parent, 16f, 0.6f);

            var root = UiBuild.Panel("Midnight", parent, new Color(0.02f, 0.01f, 0.05f, 0.82f), raycastTarget: true, rounded: false).rectTransform.Fill();
            root.SetAsLastSibling();
            var group = root.gameObject.AddComponent<CanvasGroup>();
            Tween.Fade(group, 0, 1, 0.3f);

            // Clock face.
            var clock = UiBuild.Picture("Clock", root, Theme.Sprite("circle"));
            clock.color = Palette.PaperCream;
            clock.rectTransform.anchorMin = clock.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            clock.rectTransform.pivot = new Vector2(0.5f, 1f);
            clock.rectTransform.anchoredPosition = new Vector2(0, -60);
            clock.rectTransform.sizeDelta = new Vector2(120, 120);
            UiBuild.Label("XII", clock.transform, "XII", 30, Palette.Ink, TextAnchor.UpperCenter, FontStyle.Bold).rectTransform.Fill(8);
            var hand = UiBuild.Panel("Hand", clock.transform, Palette.Ink, raycastTarget: false, rounded: false);
            hand.rectTransform.anchorMin = hand.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            hand.rectTransform.pivot = new Vector2(0.5f, 0f);
            hand.rectTransform.sizeDelta = new Vector2(6, 46);
            Tween.Punch(clock.transform, 0.12f, 0.5f);
            Theme.Title("Title", root, "Midnight", 64, Palette.PaperCream, TextAnchor.UpperCenter).rectTransform.PlaceTopLeft(0, 200, 1920, 80);

            // Event cards, flipping one by one.
            float w = 420, gap = 40, total = events.Count * w + (events.Count - 1) * gap;
            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                var card = Theme.Paper($"Card{i}", root).rectTransform;
                card.PlaceTopLeft((1920 - total) / 2f + i * (w + gap), 320, w, 470);
                var paper = card.GetChild(0);
                UiBuild.Picture("Art", paper, Art(e)).rectTransform.PlaceTopLeft(w / 2f - 80, 30, 160, 160);
                Theme.Title("Name", paper, e.Title, 36, Palette.Ink, TextAnchor.UpperCenter).rectTransform.PlaceTopLeft(10, 210, w - 20, 50);
                UiBuild.Label("Text", paper, e.Description + Where(e), 26, Palette.Ink, TextAnchor.UpperCenter).rectTransform.PlaceTopLeft(30, 270, w - 60, 180);
                card.localScale = new Vector3(0, 1, 1);
                float delay = 0.5f + i * 0.45f;
                Tween.Delay(card, delay, () =>
                {
                    Theme.Sfx("page", 0.7f, 0.8f);
                    Tween.Run(card, "flip", 0.35f, Ease.OutBack, p => { if (card) card.localScale = new Vector3(p, 1, 1); });
                }, "flipdelay");
            }

            var buttons = UiBuild.Rect("Buttons", root).PlaceTopLeft(560, 860, 800, 100);
            var fix = Theme.PrimaryButton("Fix", buttons, swapTokens > 0 ? $"Fix it ({swapTokens} swap{(swapTokens == 1 ? "" : "s")})" : "Look around", () =>
            {
                UnityEngine.Object.Destroy(root.gameObject);
                onFix();
            }, 32);
            ((RectTransform)fix.transform).PlaceTopLeft(0, 0, 380, 90);
            var pass = Theme.SecondaryButton("Pass", buttons, "Let the night pass", () =>
            {
                UnityEngine.Object.Destroy(root.gameObject);
                onPass();
            }, 30);
            ((RectTransform)pass.transform).PlaceTopLeft(420, 0, 380, 90);
            return root.gameObject;
        }

        static Sprite Art(MidnightEvent e)
        {
            switch (e.Kind)
            {
                case MidnightKind.RestlessNight: return Theme.Sprite("tag_Noisy");
                case MidnightKind.PowerFlicker: return Theme.Sprite("tag_Dark");
                case MidnightKind.BurstPipe: return Theme.Sprite("tag_Water");
                case MidnightKind.VanesVisit: return Theme.Sprite("tag_Cursed");
                case MidnightKind.SeanceDownstairs: return Theme.Sprite("tag_Basement");
                case MidnightKind.UnexpectedGuest: return e.Guest != null ? Theme.Portrait(e.Guest, "sad") : null;
                default: return null;
            }
        }

        static string Where(MidnightEvent e)
        {
            if (e.Room.HasValue) return $"\n<b>Room {(e.Room.Value.Row + 1) * 100 + e.Room.Value.Col + 1}</b>";
            if (e.Kind == MidnightKind.PowerFlicker) return $"\n<b>Floor {e.FloorRow + 1}</b>";
            if (e.Kind == MidnightKind.UnexpectedGuest && e.Guest != null) return $"\n<b>{e.Guest.Name}</b>";
            return "";
        }
    }
}
