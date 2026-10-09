using System;
using System.Collections.Generic;
using ChuchuGames.UI;
using GhostHotel.Model;
using UnityEngine;
using UnityEngine.UI;

namespace GhostHotel.View
{
    /// <summary>
    /// Dawn Results (GDD §5.2): stars fill guest by guest with rising chimes, then totals, moons and
    /// story beats. Perfect Nights get a wax-seal stamp (GDD §7.4).
    /// </summary>
    public static class DawnScreen
    {
        const float StarStep = 0.22f;

        public static GameObject Create(RectTransform parent, DawnResult r, GameProgress progress,
            Func<string, GuestDef> guest, Action onContinue)
        {
            var root = UiBuild.Rect("Dawn", parent).Fill();
            Theme.Background(root, "dawn", Palette.LampAmber);
            Theme.Sfx("dawn", 0.6f);
            Theme.Music(null);

            Theme.Title("Title", root, $"Dawn · Night {r.NightNumber}", 64, Palette.Ink, TextAnchor.UpperCenter).rectTransform.PlaceTopLeft(0, 50, 1920, 90);

            // Portrait row
            int n = r.Guests.Count;
            float cell = Mathf.Min(200f, 1700f / Mathf.Max(1, n));
            float x0 = (1920 - cell * n) / 2f;
            float t = 0.4f;
            for (int i = 0; i < n; i++)
            {
                var g = r.Guests[i];
                var def = guest(g.GuestId);
                var col = UiBuild.Rect($"Guest{i}", root).PlaceTopLeft(x0 + i * cell, 170, cell, 380);
                var portrait = UiBuild.Picture("Portrait", col, def != null ? Theme.Portrait(def, Theme.ExpressionFor(g.Stars)) : null);
                portrait.rectTransform.PlaceTopLeft(cell * 0.1f, 0, cell * 0.8f, cell * 0.8f);
                Tween.Scale(portrait.transform, Vector3.zero, Vector3.one, 0.35f, Ease.OutBack, null, t - 0.2f);
                UiBuild.Label("Name", col, g.Name, 22, Palette.Ink, TextAnchor.UpperCenter, FontStyle.Bold)
                    .rectTransform.PlaceTopLeft(0, cell * 0.8f + 6, cell, 30);

                var pill = UiBuild.Panel("StarPill", col, Palette.NightNavy.WithAlpha(0.6f), raycastTarget: false);
                pill.rectTransform.PlaceTopLeft(cell / 2f - 66, cell * 0.8f + 36, 132, 42);
                for (int s = 0; s < 3; s++)
                {
                    var star = UiBuild.Picture($"Star{s}", col, Theme.Sprite("star_empty"));
                    star.rectTransform.PlaceTopLeft(cell / 2f - 54 + s * 36, cell * 0.8f + 40, 34, 34);
                    if (s >= g.Stars) continue;
                    int index = s;
                    var filled = UiBuild.Picture("Fill", star.transform, Theme.Sprite("star"));
                    filled.rectTransform.Fill();
                    filled.transform.localScale = Vector3.zero;
                    Tween.Scale(filled.transform, Vector3.zero, Vector3.one, 0.3f, Ease.OutBack, null, t);
                    Tween.Delay(filled, t, () => Theme.Sfx($"star_{index + 1}", 0.4f), "chime");
                    t += StarStep;
                }
                t += 0.12f;

                string note = g.HiddenRevealed ? $"<color=#5B3FA0>secret: {g.HiddenLabel}</color>"
                    : g.MovedOn ? "<color=#5B3FA0><b>moved on!</b></color>"
                    : g.StoryAdvanced ? $"<color=#2F6E2F>story {g.StaysDone}/{g.Stays}</color>"
                    : r.FirstPlay && g.Stars < Economy.StoryStarsNeeded ? "<color=#7A3030>needs 2 stars</color>" : "";
                UiBuild.Label("Note", col, note, 20, Palette.Ink, TextAnchor.UpperCenter).rectTransform.PlaceTopLeft(0, cell * 0.8f + 82, cell, 30);
            }

            // Totals, after the stars have filled.
            var totals = Theme.Paper("Totals", root).rectTransform.PlaceTopLeft(560, 600, 800, 250);
            var paper = totals.GetChild(0);
            var group = totals.gameObject.AddComponent<CanvasGroup>();
            Tween.Fade(group, 0, 1, 0.3f, null, t);

            UiBuild.Label("EctoLabel", paper, "Ectoplasm", 24, Palette.Ink.WithAlpha(0.7f), TextAnchor.UpperLeft).rectTransform.PlaceTopLeft(40, 30, 220, 30);
            var ecto = UiBuild.Label("Ecto", paper, "+0", 52, Palette.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
            ecto.rectTransform.PlaceTopLeft(40, 60, 240, 70);
            Tween.Run(ecto, "count", 0.8f, Ease.OutQuad, p => { if (ecto) ecto.text = $"+{Mathf.RoundToInt(r.Ectoplasm * p)}"; }, null, false, t);

            if (r.FirstPlay)
            {
                UiBuild.Label("CalmLabel", paper, "Calm", 24, Palette.Ink.WithAlpha(0.7f), TextAnchor.UpperLeft).rectTransform.PlaceTopLeft(300, 30, 160, 30);
                UiBuild.Label("Calm", paper, $"{(r.CalmDelta >= 0 ? "+" : "")}{r.CalmDelta}", 52, r.CalmDelta >= 0 ? new Color(0.24f, 0.5f, 0.24f) : Palette.Broken,
                    TextAnchor.UpperLeft, FontStyle.Bold).rectTransform.PlaceTopLeft(300, 60, 160, 70);
            }
            UiBuild.Label("MoonLabel", paper, "Night rating", 24, Palette.Ink.WithAlpha(0.7f), TextAnchor.UpperLeft).rectTransform.PlaceTopLeft(500, 30, 260, 30);
            for (int m = 0; m < 3; m++)
            {
                var moon = UiBuild.Picture($"Moon{m}", paper, Theme.Sprite(m < r.Moons ? "moon" : "moon_empty"));
                moon.rectTransform.PlaceTopLeft(500 + m * 72, 66, 62, 62);
                Tween.Scale(moon.transform, Vector3.zero, Vector3.one, 0.3f, Ease.OutBack, null, t + 0.3f + m * 0.15f);
            }
            string footer = !r.FirstPlay ? "Replay: stories and Calm don't change." : $"Total Ectoplasm {progress.ectoplasm:N0} · Calm {progress.calm}/100";
            UiBuild.Label("Footer", paper, footer, 22, Palette.Ink.WithAlpha(0.7f), TextAnchor.UpperCenter).rectTransform.PlaceTopLeft(0, 160, 792, 30);

            if (r.Perfect)
            {
                var seal = UiBuild.Picture("Seal", root, Theme.Sprite("waxseal"));
                seal.rectTransform.PlaceTopLeft(1395, 590, 170, 170);
                seal.transform.localScale = Vector3.zero;
                var perfect = UiBuild.Label("Perfect", root, $"Perfect Night!  +{Economy.PerfectNightBonus}", 32, new Color(0.55f, 0.15f, 0.18f), TextAnchor.UpperCenter, FontStyle.Bold);
                perfect.rectTransform.PlaceTopLeft(1300, 770, 360, 46);
                perfect.enabled = false;
                // Stamp: wait for the totals, then slam down from large (scale is set only when the stamp starts).
                Tween.Delay(seal, t + 0.9f, () =>
                {
                    Tween.Scale(seal.transform, Vector3.one * 2.2f, Vector3.one, 0.3f, Ease.OutQuad, () => Theme.Sfx("drop", 0.8f, 0.7f));
                    if (perfect) perfect.enabled = true;
                }, "stamp");
            }

            if (r.Hauntquake)
            {
                var quake = UiBuild.Label("Quake", root, "<b>HAUNTQUAKE!</b> Calm ran out. The night shakes loose and must be replayed.",
                    34, Palette.Broken, TextAnchor.MiddleCenter);
                quake.rectTransform.PlaceTopLeft(160, 520, 1600, 60);
                Tween.Delay(root, t, () => Shake.Play(root, 22f, 0.7f), "quake");
            }

            var next = Theme.PrimaryButton("Continue", root, r.Hauntquake ? "Replay the night" : "Continue", onContinue, 36);
            ((RectTransform)next.transform).PlaceTopLeft(760, 900, 400, 96);
            var nextGroup = next.gameObject.AddComponent<CanvasGroup>();
            Tween.Fade(nextGroup, 0, 1, 0.3f, null, t + 0.6f);
            return root.gameObject;
        }
    }
}
