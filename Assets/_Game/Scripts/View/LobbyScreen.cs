using System;
using System.Collections.Generic;
using ChuchuGames.GridPuzzle;
using ChuchuGames.UI;
using GhostHotel.Data;
using GhostHotel.Model;
using GhostHotel.Model.Content;
using UnityEngine;
using UnityEngine.UI;

namespace GhostHotel.View
{
    /// <summary>
    /// Lobby, the day hub (GDD §5.2): point-and-click hotspots on the lobby art. Bell = start night,
    /// Ledger = guests met, Calendar = Night Select, Bartholomew = a hint. Renovation from here too.
    /// </summary>
    public static class LobbyScreen
    {
        public sealed class Actions
        {
            public Action<int> StartNight;
            public Action Settings;
            public Action MainMenu;
            public Action<string> Talk;   // dialogue script for Bartholomew
            public Action Changed;        // progress changed (renovation bought): save + redraw
            public Action<string> ReadPage; // one of Edith's ledger pages
        }

        // Hotspots in lobby.png pixels (must match GhostHotelArt.Lobby()).
        static readonly Rect LedgerSpot = new Rect(160, 230, 260, 340);
        static readonly Rect CalendarSpot = new Rect(1500, 230, 260, 300);
        static readonly Rect BellSpot = new Rect(870, 500, 180, 110);
        static readonly Rect BartSpot = new Rect(1130, 330, 230, 230);

        public static GameObject Create(RectTransform parent, NightCatalogSO catalog, GameProgress p, Actions a)
        {
            var root = UiBuild.Rect("Lobby", parent).Fill();
            var bg = Theme.Background(root, "lobby", Palette.DeepPurple);
            var art = bg.rectTransform; // hotspots ride on the background so they stay aligned at any aspect ratio
            bool finished = p.night > catalog.Count;

            Hotspot(art, "Ledger", LedgerSpot, "Ledger", () => LedgerPanel(root, catalog, p, a.ReadPage));
            Hotspot(art, "Calendar", CalendarSpot, "Calendar · Night Select", () => NightSelectPanel(root, catalog, p, a.StartNight));
            UiBuild.Label("CalendarNight", art, finished ? "ACT 1\nDONE" : $"NIGHT\n<size=96>{p.night}</size>", 34, Palette.Broken, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.AnchorTo(new Rect(1500, 300, 260, 230));
            var bell = Hotspot(art, "Bell", BellSpot, finished ? "Act 2 is coming soon" : $"Bell · Start Night {p.night}", () =>
            {
                if (finished) return;
                Theme.Sfx("drop", 0.8f, 1.25f);
                a.StartNight(p.night);
            });
            if (!finished) bell.gameObject.AddComponent<Pulse>().amount = 0.04f;

            var bart = UiBuild.Picture("Bartholomew", art, Theme.Sprite("ghost_Bartholomew_happy"), raycastTarget: true);
            bart.rectTransform.AnchorTo(BartSpot);
            bart.gameObject.AddComponent<Bob>();
            bart.gameObject.AddComponent<Button>().onClick.AddListener(() => a.Talk(BartholomewHint(catalog, p)));
            Plate(art, "Bartholomew", new Rect(BartSpot.x, BartSpot.yMax - 10, BartSpot.width, 44));

            // Top bar
            var top = UiBuild.Panel("TopBar", root, Palette.NightNavy.WithAlpha(0.8f), rounded: false).rectTransform.Band(RectTransform.Edge.Top, 80);
            UiBuild.Label("Stats", top,
                $"Ectoplasm <b>{p.ectoplasm:N0}</b>    ·    Memories <b>{p.memories}</b>    ·    Calm <b>{p.calm}</b>/100    ·    Guests moved on <b>{p.GuestsMovedOn}</b>",
                28, Palette.WarmGlow, TextAnchor.MiddleLeft).rectTransform.Fill(36);

            var bar = UiBuild.Rect("Buttons", root);
            bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0, 0);
            bar.anchoredPosition = new Vector2(36, 30);
            bar.sizeDelta = new Vector2(1100, 80);
            var reno = Theme.PrimaryButton("Renovate", bar, "Renovate", () => RenovationPanel(root, catalog, p, a.Changed), 28);
            ((RectTransform)reno.transform).PlaceTopLeft(0, 0, 240, 80);
            var staff = Theme.PrimaryButton("Staff", bar, "Staff & Keepsakes", () => StaffPanel(root, catalog, p, a.Changed), 26);
            ((RectTransform)staff.transform).PlaceTopLeft(260, 0, 300, 80);
            var settings = Theme.SecondaryButton("Settings", bar, "Settings", a.Settings, 26);
            ((RectTransform)settings.transform).PlaceTopLeft(580, 0, 200, 80);
            var menu = Theme.SecondaryButton("Menu", bar, "Main Menu", a.MainMenu, 26);
            ((RectTransform)menu.transform).PlaceTopLeft(800, 0, 220, 80);

            var group = root.gameObject.AddComponent<CanvasGroup>();
            Tween.Fade(group, 0, 1, 0.35f);
            return root.gameObject;
        }

        /// <summary>Anchors a rect to a region given in 1920×1080 background pixels (y down).</summary>
        static RectTransform AnchorTo(this RectTransform rt, Rect px)
        {
            rt.anchorMin = new Vector2(px.xMin / 1920f, 1f - px.yMax / 1080f);
            rt.anchorMax = new Vector2(px.xMax / 1920f, 1f - px.yMin / 1080f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        static Button Hotspot(RectTransform art, string name, Rect px, string label, Action onClick)
        {
            var hit = UiBuild.Panel(name, art, new Color(1, 1, 1, 0.001f), raycastTarget: true, rounded: false);
            hit.rectTransform.AnchorTo(px);
            var b = hit.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.highlightedColor = new Color(1, 0.95f, 0.8f, 0.18f);
            colors.pressedColor = new Color(1, 0.95f, 0.8f, 0.3f);
            b.colors = colors;
            b.onClick.AddListener(() => onClick());
            Plate(art, label, new Rect(px.x - 40, px.yMax + 8, px.width + 80, 44));
            return b;
        }

        static void Plate(RectTransform art, string label, Rect px)
        {
            var plate = UiBuild.Panel("Plate", art, Palette.NightNavy.WithAlpha(0.85f), raycastTarget: false);
            plate.rectTransform.AnchorTo(px);
            UiBuild.Label("Text", plate.transform, label, 22, Palette.PaperCream).rectTransform.Fill(4);
        }

        static string BartholomewHint(NightCatalogSO catalog, GameProgress p)
        {
            if (p.night > catalog.Count) return "Bartholomew[happy]: Every room booked, every guest content. Edith would be proud. I'm proud!";
            if (p.night == 1) return "Bartholomew[happy]: Ring the bell on the desk when you're ready. I'll show you the ropes.";
            if (p.ectoplasm >= Renovations.All[0].cost && p.renovations.Count == 0)
                return "Bartholomew: You've saved up some Ectoplasm. Blackout curtains make any room Dark. Gloria would approve.";
            if (p.calm < 40) return "Bartholomew[sad]: The halls feel restless. A good night's work will calm them.";
            return "Bartholomew: Stuck on a night? Read every guest card before you place anyone. Needs first, then likes.";
        }

        // ------------------------------------------------------------------ panels

        public static void NightSelectPanel(RectTransform parent, NightCatalogSO catalog, GameProgress p, Action<int> start)
        {
            var (root, panel) = Overlays.Modal(parent, 1600, 820, "Night Select");
            var grid = UiBuild.Rect("Grid", panel).PlaceTopLeft(50, 110, 1500, 560);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(280, 250);
            layout.spacing = new Vector2(25, 25);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 5;

            for (int n = 1; n <= catalog.Count; n++)
            {
                int number = n;
                bool reached = n <= p.night;
                var rec = p.nights.Find(r => r.number == n);
                var cell = UiBuild.Panel($"Night{n}", grid, reached ? Palette.WarmGlow.WithAlpha(0.4f) : Palette.DuskViolet.WithAlpha(0.25f));
                UiBuild.Label("N", cell.transform, $"<b>Night {n}</b>\n<size=22>{(reached ? catalog.Night(n).data.title : "Fogged")}</size>", 30, Palette.Ink, TextAnchor.UpperCenter)
                    .rectTransform.PlaceTopLeft(10, 16, 260, 100);
                for (int m = 0; m < 3; m++)
                {
                    var moon = UiBuild.Picture($"Moon{m}", cell.transform, Theme.Sprite(rec != null && m < rec.bestMoons ? "moon" : "moon_empty"));
                    moon.rectTransform.PlaceTopLeft(65 + m * 52, 120, 46, 46);
                }
                if (rec != null && rec.perfect)
                    UiBuild.Picture("Seal", cell.transform, Theme.Sprite("waxseal")).rectTransform.PlaceTopLeft(200, 8, 64, 64);
                var b = cell.gameObject.AddComponent<Button>();
                b.interactable = reached;
                b.onClick.AddListener(() =>
                {
                    UnityEngine.Object.Destroy(root);
                    start(number);
                });
                UiBuild.Label("Hint", cell.transform, reached ? (n < p.night ? "Replay" : "Play") : "", 22, Palette.Brass, TextAnchor.LowerCenter)
                    .rectTransform.Fill(12);
            }
            var close = Theme.PaperButton("Close", panel, "Back", () => UnityEngine.Object.Destroy(root));
            ((RectTransform)close.transform).PlaceTopLeft(660, 720, 280, 70);
        }

        public static void LedgerPanel(RectTransform parent, NightCatalogSO catalog, GameProgress p, Action<string> readPage = null)
        {
            Theme.Sfx("page");
            var (root, panel) = Overlays.Modal(parent, 1500, 860, "Guest Ledger");
            var factory = catalog.CreateFactory();
            int i = 0, unmet = 0;
            foreach (var gso in catalog.guests)
            {
                var gp = p.guests.Find(g => g.id == gso.Id);
                bool met = gp != null && (gp.staysDone > 0 || gp.movedOn);
                if (!met) { unmet++; continue; }
                if (i >= 14) continue; // 7 rows × 2 columns
                var def = factory.Guest(gso.Id);
                var row = UiBuild.Rect($"Guest{i}", panel).PlaceTopLeft(50 + (i % 2) * 710, 100 + (i / 2) * 80, 690, 76);
                UiBuild.Picture("Portrait", row, Theme.Portrait(def, gp.movedOn ? "happy" : "neutral")).rectTransform.PlaceTopLeft(0, 0, 72, 72);
                var status = gp.movedOn ? $"<color=#7E5FD0>Moved on</color> · keepsake: {gso.data.keepsake}"
                    : def.Stays >= 10 ? "Hotel staff" : $"Stay {gp.staysDone} / {def.Stays}";
                UiBuild.Label("Text", row, $"<b>{def.Name}</b>  <size=18>{def.Type.Display()} · {gso.data.era}</size>\n<size=21>{status}</size>",
                    25, Palette.Ink, TextAnchor.MiddleLeft).rectTransform.PlaceTopLeft(86, 0, 600, 72);
                i++;
            }
            if (i == 0) UiBuild.Label("Empty", panel, "No guests yet. Ring the bell!", 32, Palette.Ink).rectTransform.PlaceTopLeft(0, 300, 1500, 60);
            UiBuild.Label("Unmet", panel, $"{unmet} guests not yet met.", 22, Palette.Ink.WithAlpha(0.6f), TextAnchor.MiddleLeft)
                .rectTransform.PlaceTopLeft(50, 800, 400, 30);

            // Edith's Pages (GDD §5.2 Guest Ledger tab): torn pages found on Grand Nights.
            Theme.Title("PagesTitle", panel, "Edith's Pages", 30, Palette.Ink).rectTransform.PlaceTopLeft(50, 680, 400, 40);
            int found = 0;
            foreach (var night in p.ledgerPages)
            {
                var data = catalog.Night(night)?.data;
                if (data == null || string.IsNullOrEmpty(data.ledgerPage)) continue;
                var pageText = data.ledgerPage;
                var b = Theme.PaperButton($"Page{night}", panel, $"Page from Night {night}", () => readPage?.Invoke(pageText), 22);
                ((RectTransform)b.transform).PlaceTopLeft(50 + found * 300, 726, 280, 56);
                found++;
            }
            if (found == 0)
                UiBuild.Label("NoPages", panel, "None found yet. Edith hid them well.", 22, Palette.Ink.WithAlpha(0.6f), TextAnchor.MiddleLeft)
                    .rectTransform.PlaceTopLeft(50, 726, 600, 56);
            var close = Theme.PaperButton("Close", panel, "Close", () => UnityEngine.Object.Destroy(root));
            ((RectTransform)close.transform).PlaceTopLeft(1220, 30, 230, 64);
        }

        /// <summary>Staff & Keepsakes (GDD §5 screen 16): hire staff into slots, equip up to 3 keepsakes.</summary>
        public static void StaffPanel(RectTransform parent, NightCatalogSO catalog, GameProgress p, Action changed)
        {
            var (root, panel) = Overlays.Modal(parent, 1600, 880, "Staff & Keepsakes");
            void Redraw()
            {
                UnityEngine.Object.Destroy(root);
                changed?.Invoke();
                StaffPanel(parent, catalog, p, changed);
            }

            int slots = Perks.StaffSlots(p.night);
            Theme.Title("StaffTitle", panel, $"Staff  ·  {Mathf.Min(p.activeStaff.Count, slots)} / {slots} on duty", 30, Palette.Ink)
                .rectTransform.PlaceTopLeft(50, 100, 800, 40);
            if (slots == 0)
                UiBuild.Label("NoSlots", panel, "Staff can be hired from Act 2.", 22, Palette.Ink.WithAlpha(0.6f), TextAnchor.UpperLeft)
                    .rectTransform.PlaceTopLeft(50, 140, 700, 30);
            for (int i = 0; i < Perks.Staff.Length; i++)
            {
                var s = Perks.Staff[i];
                bool hired = p.hiredStaff.Contains(s.Id), onDuty = p.activeStaff.Contains(s.Id);
                var card = UiBuild.Panel($"Staff{i}", panel, onDuty ? Palette.WarmGlow.WithAlpha(0.45f) : Theme.PaperButtonFill.WithAlpha(0.5f));
                card.rectTransform.PlaceTopLeft(50 + i * 500, 180, 480, 230);
                UiBuild.Label("Name", card.transform, $"<b>{s.Name}</b>", 28, Palette.Ink, TextAnchor.UpperLeft).rectTransform.PlaceTopLeft(20, 14, 440, 36);
                UiBuild.Label("Desc", card.transform, s.Description, 20, Palette.Ink, TextAnchor.UpperLeft).rectTransform.PlaceTopLeft(20, 56, 440, 90);
                string label = !hired ? $"Hire · {s.Cost} Ectoplasm" : onDuty ? "On duty · send home" : "Put on duty";
                var b = Theme.PaperButton("Action", card.transform, label, () =>
                {
                    if (!hired)
                    {
                        if (slots == 0 || p.ectoplasm < s.Cost) { Theme.Sfx("creak"); return; }
                        p.ectoplasm -= s.Cost;
                        p.hiredStaff.Add(s.Id);
                        if (p.activeStaff.Count < slots) p.activeStaff.Add(s.Id);
                    }
                    else if (onDuty) p.activeStaff.Remove(s.Id);
                    else
                    {
                        if (p.activeStaff.Count >= slots) { Theme.Sfx("creak"); return; }
                        p.activeStaff.Add(s.Id);
                    }
                    Theme.Sfx("drop", 0.7f);
                    Redraw();
                }, 22);
                ((RectTransform)b.transform).PlaceTopLeft(20, 160, 440, 56);
            }

            Theme.Title("CabinetTitle", panel, $"Keepsake cabinet  ·  {p.equippedKeepsakes.Count} / {Perks.MaxKeepsakes} equipped", 30, Palette.Ink)
                .rectTransform.PlaceTopLeft(50, 440, 900, 40);
            int n = 0;
            foreach (var gso in catalog.guests)
            {
                var gp = p.guests.Find(x => x.id == gso.Id);
                if (gp == null || !gp.movedOn || string.IsNullOrEmpty(gso.data.keepsake)) continue;
                if (n >= 12) break;
                var passive = Perks.Passive(gso.data);
                bool equipped = p.equippedKeepsakes.Contains(gso.Id);
                var id = gso.Id;
                var b = Theme.PaperButton($"Keepsake{n}", panel,
                    $"<b>{gso.data.keepsake}</b>\n<size=17>{Perks.Describe(passive)}</size>" + (equipped ? "\n<size=17><color=#7A4B10>equipped</color></size>" : ""),
                    () =>
                    {
                        if (equipped) p.equippedKeepsakes.Remove(id);
                        else if (p.equippedKeepsakes.Count < Perks.MaxKeepsakes) p.equippedKeepsakes.Add(id);
                        else { Theme.Sfx("creak"); return; }
                        Theme.Sfx("page", 0.6f);
                        Redraw();
                    }, 20, equipped ? Palette.LampAmber : (Color?)null);
                ((RectTransform)b.transform).PlaceTopLeft(50 + (n % 4) * 380, 490 + (n / 4) * 100, 360, 90);
                n++;
            }
            if (n == 0)
                UiBuild.Label("Empty", panel, "Guests leave a keepsake when they move on. The cabinet is empty for now.", 22,
                    Palette.Ink.WithAlpha(0.6f), TextAnchor.UpperLeft).rectTransform.PlaceTopLeft(50, 490, 1000, 30);
            var close = Theme.PrimaryButton("Close", panel, "Done", () => UnityEngine.Object.Destroy(root));
            ((RectTransform)close.transform).PlaceTopLeft(650, 790, 300, 70);
        }

        public static void RenovationPanel(RectTransform parent, NightCatalogSO catalog, GameProgress p, Action changed)
        {
            var reno = Renovations.All[0];
            var next = catalog.Night(Mathf.Clamp(p.night, 1, catalog.Count)).data;
            var (root, panel) = Overlays.Modal(parent, 1300, 840, "Renovation");
            UiBuild.Picture("Icon", panel, Theme.Sprite($"tag_{reno.tag}")).rectTransform.PlaceTopLeft(60, 110, 90, 90);
            UiBuild.Label("Info", panel,
                $"<b>{reno.displayName}</b>: adds <b>{reno.tag}</b> to a room. {reno.cost} Ectoplasm.\n<size=24>You have {p.ectoplasm:N0}. Removing refunds it in full. Rooms shown for Night {next.number}'s hotel.</size>",
                30, Palette.Ink, TextAnchor.MiddleLeft).rectTransform.PlaceTopLeft(170, 100, 1070, 110);

            // Room picker laid out like the hotel: top floor first.
            var hotel = catalog.CreateFactory().Night(next, p.renovations);
            float cw = Mathf.Min(220, 1100f / next.roomsPerFloor), ch = Mathf.Min(150, 420f / next.floors);
            for (int f = next.floors - 1; f >= 0; f--)
            for (int c = 0; c < next.roomsPerFloor; c++)
            {
                var cell = new Cell(f, c);
                var room = hotel.Room(cell);
                string number = room.Number.ToString();
                var placed = p.renovations.Find(r => r.room == number && r.id == reno.id);
                var label = $"<b>{number}</b>\n<size=20>{string.Join(", ", room.Tags.Effective())}</size>" + (placed != null ? "\n<size=20>curtains · tap to remove</size>" : "");
                var b = Theme.PaperButton($"Room{number}", panel, label, () =>
                {
                    bool ok = placed != null ? Renovations.Remove(p, placed) : Renovations.Place(p, reno, number);
                    if (!ok) { Theme.Sfx("creak"); return; }
                    Theme.Sfx(placed != null ? "page" : "drop");
                    UnityEngine.Object.Destroy(root);
                    changed?.Invoke();
                    RenovationPanel(parent, catalog, p, changed);
                }, 24, placed != null ? Palette.LampAmber : (Color?)null);
                ((RectTransform)b.transform).PlaceTopLeft(100 + c * (cw + 12), 240 + (next.floors - 1 - f) * (ch + 12), cw, ch);
            }
            var close = Theme.PrimaryButton("Close", panel, "Done", () => UnityEngine.Object.Destroy(root));
            ((RectTransform)close.transform).PlaceTopLeft(500, 740, 300, 76);
        }
    }
}
