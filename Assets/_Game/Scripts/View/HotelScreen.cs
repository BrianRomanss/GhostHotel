using System;
using System.Collections.Generic;
using ChuchuGames.GridPuzzle;
using ChuchuGames.UI;
using GhostHotel.Model;
using GhostHotel.Model.Rules;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GhostHotel.View
{
    /// <summary>
    /// Hotel View, the main gameplay screen (GDD §5.1): Guest Card left, hotel cutaway centre, Arrival
    /// Queue right, action bar bottom. Live previews, live stars and juice on every placement.
    /// </summary>
    public sealed class HotelScreen : MonoBehaviour
    {
        public const int HintsPerNight = 3;

        public sealed class Options
        {
            public int Ectoplasm;
            public int Calm;
            /// <summary>Stays completed per guest id, for the story candles.</summary>
            public Func<string, int> StaysDone = _ => 0;
            public Func<string, string> Era = _ => "";
            /// <summary>Returns the room number each guest has in a perfect solution (for hints), or null.</summary>
            public Func<Dictionary<string, int>> Solution;
            public Action OnOpenDoors;
            public Action OnPause;
            /// <summary>"Let the night pass" during Midnight.</summary>
            public Action OnEndMidnight;
            public int HintBonus;
        }

        sealed class RoomWidgets
        {
            public Image Frame;
            public RectTransform Slot, Icons;
            public Image[] Stars;
            public Text Mark;
            public Pulse Pulse;
        }

        sealed class GuestWidgets
        {
            public RectTransform Root;
            public Image Card, Ghost;
            public Text Name;
            public RectTransform Dots;
        }

        NightSession _session;
        HotelModel _hotel;
        Options _opt;
        float _guestSize;
        int _hintsLeft = HintsPerNight;

        readonly Dictionary<Cell, RoomWidgets> _rooms = new Dictionary<Cell, RoomWidgets>();
        readonly Dictionary<GuestState, GuestWidgets> _guests = new Dictionary<GuestState, GuestWidgets>();
        readonly Dictionary<GuestState, int> _lastStars = new Dictionary<GuestState, int>();

        RectTransform _queueContent, _cardRoot, _hotelArea;
        Text _queueTitle, _rating, _title;
        bool _midnight;
        Button _undo, _redo, _clear, _open, _hint;
        Pulse _openPulse;
        GuestState _focused;
        Feedback? _hoverFeedback;

        public static HotelScreen Create(RectTransform parent, NightSession session, Options options)
        {
            var rt = UiBuild.Rect("HotelScreen", parent).Fill();
            var screen = rt.gameObject.AddComponent<HotelScreen>();
            screen.Bind(session, options);
            return screen;
        }

        void Bind(NightSession session, Options options)
        {
            _session = session;
            _hotel = session.Hotel;
            _opt = options;
            _hintsLeft = HintsPerNight + options.HintBonus;
            Build();
            _hotel.Changed += Refresh;
            _session.Commands.Changed += RefreshButtons;
            foreach (var g in _hotel.Guests) _lastStars[g] = Scoring.ScoreGuest(g, _hotel);
            _focused = _hotel.Guests.Count > 0 ? _hotel.Guests[0] : null;
            Refresh();
        }

        void OnDestroy()
        {
            if (_hotel != null) _hotel.Changed -= Refresh;
            if (_session != null) _session.Commands.Changed -= RefreshButtons;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.escapeKey.wasPressedThisFrame) _opt.OnPause?.Invoke();
            if (!kb.ctrlKey.isPressed) return;
            if (kb.zKey.wasPressedThisFrame) _session.Commands.Undo();
            if (kb.yKey.wasPressedThisFrame) _session.Commands.Redo();
        }

        /// <summary>Spotlight targets for the tutorial: queue, card, open, undo, rating, room:NNN.</summary>
        public RectTransform TutorialTarget(string key)
        {
            switch (key)
            {
                case "queue": return (RectTransform)_queueContent.parent;
                case "card": return _cardRoot;
                case "open": return (RectTransform)_open.transform;
                case "undo": return (RectTransform)_undo.transform;
                case "rating": return _hotelArea;
            }
            if (key != null && key.StartsWith("room:") && int.TryParse(key.Substring(5), out var n))
            {
                var cell = new Cell(n / 100 - 1, n % 100 - 1);
                if (_rooms.TryGetValue(cell, out var w)) return w.Frame.rectTransform;
            }
            return _hotelArea;
        }

        /// <summary>
        /// Midnight "Fix it" mode (GDD §4.7): the top bar counts Swap Tokens, walk-ins join the queue,
        /// and the main button becomes "Let the night pass". Undo refunds tokens.
        /// </summary>
        public void EnterMidnight()
        {
            _midnight = true;
            _open.GetComponentInChildren<Text>().text = "Let the night pass";
            _hint.gameObject.SetActive(false);
            _clear.gameObject.SetActive(false);
            Theme.Music(null);
            Refresh();
        }

        /// <summary>Debug/screenshot helper: show the drag preview for a guest without dragging.</summary>
        public void PreviewFor(GuestState guest)
        {
            _focused = guest;
            RefreshCard();
            ShowPreview(guest);
        }

        // ---------------------------------------------------------------- build

        void Build()
        {
            var root = (RectTransform)transform;
            var dragDrop = gameObject.AddComponent<DragDropController>();
            dragDrop.DragStarted += OnDragStarted;
            dragDrop.HoverChanged += OnHover;
            dragDrop.SelectionChanged += d =>
            {
                if (d != null) { Focus(d); ShowPreview(GuestOf(d)); Tween.Punch(d.transform, 0.12f); }
                else ClearPreview();
            };
            dragDrop.Dropped += OnDropped;

            UiBuild.Panel("Background", root, Palette.NightNavy, rounded: false).rectTransform.Fill();
            var glow = UiBuild.Picture("Glow", root, Theme.Sprite("glow"));
            glow.color = new Color(0.75f, 0.85f, 0.95f, 0.08f); // cold moonlight, not lamplight
            glow.preserveAspect = false;
            glow.rectTransform.PlaceTopLeft(460, -300, 1000, 700);

            var top = UiBuild.Panel("TopBar", root, Palette.DeepPurple, rounded: false).rectTransform.Band(RectTransform.Edge.Top, 90);
            _title = Theme.Title("Title", top, $"Night {_session.Number} · {_session.Title}", 36, Palette.PaperCream, TextAnchor.MiddleLeft);
            _title.rectTransform.Fill(36);
            UiBuild.Label("Ectoplasm", top, $"Ectoplasm {_opt.Ectoplasm:N0}    Calm {_opt.Calm}", 26, Palette.Valid, TextAnchor.MiddleCenter)
                .rectTransform.Fill(36);
            _rating = UiBuild.Label("Rating", top, "", 28, Palette.WarmGlow, TextAnchor.MiddleRight);
            _rating.rectTransform.Fill(36);
            _rating.rectTransform.offsetMax = new Vector2(-130, -36);
            var pause = Theme.SecondaryButton("Pause", top, "II", () => _opt.OnPause?.Invoke(), 30);
            var prt = (RectTransform)pause.transform;
            prt.anchorMin = prt.anchorMax = new Vector2(1, 0.5f);
            prt.pivot = new Vector2(1, 0.5f);
            prt.anchoredPosition = new Vector2(-24, 0);
            prt.sizeDelta = new Vector2(80, 64);

            Theme.Atmosphere(root, 0.3f);
            _cardRoot = Theme.Paper("GuestCard", root).rectTransform.PlaceTopLeft(36, 120, 390, 790);

            _hotelArea = UiBuild.Rect("Hotel", root).PlaceTopLeft(456, 120, 1010, 790);
            BuildRooms(_hotelArea, 1010, 790);

            var queue = UiBuild.Panel("ArrivalQueue", root, Palette.DeepPurple).rectTransform.PlaceTopLeft(1496, 120, 390, 790);
            queue.gameObject.AddComponent<DropTarget>(); // drop here = back to the queue
            _queueTitle = UiBuild.Label("Title", queue, "", 24, Palette.LampAmber, TextAnchor.UpperLeft, FontStyle.Bold);
            _queueTitle.rectTransform.PlaceTopLeft(24, 18, 340, 36);
            _queueContent = UiBuild.Rect("Content", queue).PlaceTopLeft(18, 64, 354, 710);
            var grid = _queueContent.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(168, 168);
            grid.spacing = new Vector2(14, 14);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperCenter;

            foreach (var g in _hotel.Guests) _guests[g] = BuildGuest(g);

            var bar = UiBuild.Rect("ActionBar", root).PlaceTopLeft(456, 940, 1430, 100);
            var cmds = _session.Commands;
            _undo = Theme.SecondaryButton("Undo", bar, "Undo", () => cmds.Undo());
            ((RectTransform)_undo.transform).PlaceTopLeft(0, 10, 170, 76);
            _redo = Theme.SecondaryButton("Redo", bar, "Redo", () => cmds.Redo());
            ((RectTransform)_redo.transform).PlaceTopLeft(186, 10, 170, 76);
            _hint = Theme.SecondaryButton("Hint", bar, "", UseHint);
            ((RectTransform)_hint.transform).PlaceTopLeft(372, 10, 190, 76);
            _hint.gameObject.SetActive(GameSettings.ShowHints && _opt.Solution != null);
            _clear = Theme.SecondaryButton("ClearAll", bar, "Clear all", () => cmds.Execute(new ClearAllCommand(_hotel)));
            ((RectTransform)_clear.transform).PlaceTopLeft(578, 10, 190, 76);
            _open = Theme.PrimaryButton("OpenDoors", bar, "Open the doors", () =>
            {
                if (_midnight) _opt.OnEndMidnight?.Invoke();
                else _opt.OnOpenDoors?.Invoke();
            }, 32);
            ((RectTransform)_open.transform).PlaceTopLeft(1040, 4, 390, 88);
            _openPulse = _open.gameObject.AddComponent<Pulse>();

            Theme.Music("music_checkin");
        }

        void BuildRooms(RectTransform area, float areaW, float areaH)
        {
            const float gap = 12f, roof = 60f;
            int floors = _hotel.Floors, cols = _hotel.RoomsPerFloor;
            float usableH = areaH - roof - 20;
            float w = Mathf.Min(236f, (areaW - 40 - (cols - 1) * gap) / cols);
            float h = Mathf.Min(300f, (usableH - (floors - 1) * gap) / floors);
            float gridW = cols * w + (cols - 1) * gap, gridH = floors * h + (floors - 1) * gap;
            float x0 = (areaW - gridW) / 2f;
            float y0 = roof + (usableH - gridH) / 2f + 10;
            _guestSize = Mathf.Clamp(Mathf.Min(w - 30f, h - 70f), 60f, 170f);

            // Building shell: brass frame + violet body + roof band (the dollhouse cutaway).
            var shell = UiBuild.Panel("Shell", area, Palette.Brass);
            shell.rectTransform.PlaceTopLeft(x0 - 18, y0 - 18, gridW + 36, gridH + 36);
            UiBuild.Panel("Body", shell.transform, Palette.DuskViolet.WithAlpha(0.9f)).rectTransform.Fill(5);
            var roofBand = UiBuild.Panel("Roof", area, Palette.DeepPurple);
            roofBand.rectTransform.PlaceTopLeft(x0 - 40, y0 - 18 - roof, gridW + 80, roof);
            var lamp = UiBuild.Picture("Lamp", roofBand.transform, Theme.Sprite("glow"));
            lamp.color = Palette.WarmGlow.WithAlpha(0.8f);
            lamp.rectTransform.Centre(46, 46);
            lamp.gameObject.AddComponent<Flicker>();

            foreach (var cell in _hotel.Rooms.Cells())
            {
                var room = _hotel.Room(cell);
                int rowFromTop = floors - 1 - cell.Row; // floor 1 drawn at the bottom
                var frame = UiBuild.Panel($"Room{room.Number}", area, Palette.DeepPurple);
                frame.rectTransform.PlaceTopLeft(x0 + cell.Col * (w + gap), y0 + rowFromTop * (h + gap), w, h);
                frame.rectTransform.pivot = new Vector2(0.5f, 0.5f); // pulse scales from the centre
                frame.rectTransform.anchoredPosition += new Vector2(w / 2f, -h / 2f);
                frame.gameObject.AddComponent<DropTarget>().Payload = cell;
                var pulse = frame.gameObject.AddComponent<Pulse>();
                pulse.enabled = false;

                var stage = UiBuild.Picture("Stage", frame.transform, Theme.Sprite("room_stage"));
                stage.preserveAspect = false;
                stage.raycastTarget = true;
                stage.rectTransform.Fill(5);

                var plate = UiBuild.Panel("NumberPlate", stage.transform, Palette.NightNavy.WithAlpha(0.75f), raycastTarget: false);
                plate.rectTransform.PlaceTopLeft(6, 6, 58, 28);
                UiBuild.Label("Number", plate.transform, room.Number.ToString(), 18, Palette.PaperCream).rectTransform.Fill();

                var icons = UiBuild.Rect("Icons", stage.transform);
                icons.anchorMin = icons.anchorMax = icons.pivot = new Vector2(1, 1);
                icons.anchoredPosition = new Vector2(-4, -4);
                icons.sizeDelta = new Vector2(w - 70, 40);
                var hl = icons.gameObject.AddComponent<HorizontalLayoutGroup>();
                hl.childAlignment = TextAnchor.UpperRight;
                hl.spacing = 2;
                hl.childControlWidth = hl.childControlHeight = false;
                hl.childForceExpandWidth = hl.childForceExpandHeight = false;

                var slot = UiBuild.Rect("Slot", stage.transform);
                slot.anchorMin = slot.anchorMax = new Vector2(0.5f, 0f);
                slot.pivot = new Vector2(0.5f, 0f);
                slot.anchoredPosition = new Vector2(0, 12);
                slot.sizeDelta = new Vector2(_guestSize, _guestSize);

                var stars = new Image[Scoring.MaxStars];
                for (int i = 0; i < stars.Length; i++)
                {
                    var s = UiBuild.Picture($"Star{i}", stage.transform, Theme.Sprite("star"));
                    var srt = s.rectTransform;
                    srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(0.5f, 0f);
                    srt.sizeDelta = new Vector2(26, 26);
                    srt.anchoredPosition = new Vector2((i - 1) * 28f, 12 + _guestSize + 2);
                    stars[i] = s;
                }

                // Preview word on a dark plate inside the room's floor strip, so it never spills into the next row.
                var markPlate = UiBuild.Panel("MarkPlate", stage.transform, Palette.NightNavy.WithAlpha(0.7f), raycastTarget: false);
                var mp = markPlate.rectTransform;
                mp.anchorMin = mp.anchorMax = mp.pivot = new Vector2(0.5f, 0f);
                mp.anchoredPosition = new Vector2(0, 4);
                mp.sizeDelta = new Vector2(Mathf.Min(110, w - 20), 26);
                var mark = UiBuild.Label("Mark", markPlate.transform, "", 18, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                mark.rectTransform.Fill();
                markPlate.enabled = false;

                // Low mist pooling on each room's floor.
                var mist = UiBuild.Picture("Mist", stage.transform, Theme.Sprite("fog"));
                if (mist.sprite != null)
                {
                    mist.preserveAspect = false;
                    mist.color = new Color(1, 1, 1, 0.35f);
                    var mrt2 = mist.rectTransform;
                    mrt2.anchorMin = new Vector2(-0.2f, 0f);
                    mrt2.anchorMax = new Vector2(1.2f, 0.3f);
                    mrt2.offsetMin = mrt2.offsetMax = Vector2.zero;
                    mist.gameObject.AddComponent<Drift>().distance = 14f;
                    mist.transform.SetSiblingIndex(1); // behind the guest, above the room art
                }

                _rooms[cell] = new RoomWidgets { Frame = frame, Slot = slot, Icons = icons, Stars = stars, Mark = mark, Pulse = pulse };
            }
        }

        GuestWidgets BuildGuest(GuestState g)
        {
            var card = UiBuild.Panel($"Guest_{g.Def.Id}", _queueContent, Palette.PaperCream);
            var rt = card.rectTransform;
            if (!g.Locked) card.gameObject.AddComponent<Draggable>().Payload = g;
            else card.gameObject.AddComponent<GuestTapTarget>().Init(() => { _focused = g; RefreshCard(); });

            var ghost = UiBuild.Picture("Ghost", rt, Theme.Ghost(g.Def));
            ghost.rectTransform.anchorMin = new Vector2(0.1f, 0.22f);
            ghost.rectTransform.anchorMax = new Vector2(0.9f, 0.98f);
            ghost.rectTransform.offsetMin = ghost.rectTransform.offsetMax = Vector2.zero;
            ghost.gameObject.AddComponent<Bob>().amplitude = 3f;

            var label = g.Locked ? $"{g.Def.Name} (booked)" : g.Optional ? $"{g.Def.Name} (walk-in)" : g.Def.Name;
            var name = UiBuild.Label("Name", rt, label, 18, Palette.Ink, TextAnchor.LowerCenter, FontStyle.Bold);
            name.rectTransform.Fill(6);

            // Mini rule dots (GDD §5.1 queue cards): red needs, gold likes, grey dislikes.
            var dots = UiBuild.Rect("Dots", rt);
            dots.anchorMin = dots.anchorMax = dots.pivot = new Vector2(0, 1);
            dots.anchoredPosition = new Vector2(8, -8);
            dots.sizeDelta = new Vector2(120, 16);
            int x = 0;
            void Dot(Color c, int count)
            {
                for (int i = 0; i < count; i++, x += 18)
                {
                    var d = UiBuild.Picture("Dot", dots, Theme.Sprite("circle"));
                    d.color = c;
                    d.rectTransform.PlaceTopLeft(x, 0, 14, 14);
                }
            }
            if (g.IsRiddle) { Dot(Palette.Aura, 3); return new GuestWidgets { Root = rt, Card = card, Ghost = ghost, Name = name, Dots = dots }; }
            Dot(Palette.Broken, g.Def.Needs.Count);
            Dot(Palette.Partial, g.Def.Likes.Count);
            Dot(new Color(0.6f, 0.57f, 0.62f), g.Def.Dislikes.Count);

            return new GuestWidgets { Root = rt, Card = card, Ghost = ghost, Name = name, Dots = dots };
        }

        // ---------------------------------------------------------------- interaction

        static GuestState GuestOf(Draggable d) => d != null ? d.Payload as GuestState : null;

        void Focus(Draggable d)
        {
            var g = GuestOf(d);
            if (g == null) return;
            _focused = g;
            RefreshCard();
        }

        void OnDragStarted(Draggable d)
        {
            var g = GuestOf(d);
            Focus(d);
            ShowPreview(g);
            SetInRoomLook(_guests[g], true);
            Tween.Scale(d.transform, new Vector3(1.15f, 0.85f, 1), Vector3.one * 1.08f, 0.25f); // squash on pick-up
            Theme.Sfx("pickup", 0.6f, UnityEngine.Random.Range(0.95f, 1.08f));
            _hoverFeedback = null;
        }

        void OnHover(Draggable d, DropTarget target)
        {
            var g = GuestOf(d);
            if (g == null || target == null || !(target.Payload is Cell cell)) { _hoverFeedback = null; return; }
            var f = Scoring.Preview(g, cell, _hotel);
            if (f == Feedback.Red && _hoverFeedback != Feedback.Red) Theme.Sfx("creak", 0.5f);
            _hoverFeedback = f;
        }

        void OnDropped(Draggable d, DropTarget target)
        {
            ClearPreview();
            d.transform.localScale = Vector3.one;
            var guest = GuestOf(d);
            if (guest == null) return;

            Cell? destination = target != null && target.Payload is Cell c ? c : (Cell?)null; // the queue has no payload
            if (target == null || destination == guest.Room || !_session.CanMove(guest, destination))
            {
                if (_midnight && target != null && destination != guest.Room) Theme.Sfx("creak", 0.5f);
                Refresh(); // nothing changed: restore the look for wherever it came from
                return;
            }
            _session.Move(guest, destination);
            Tween.Scale(d.transform, new Vector3(0.85f, 1.2f, 1), Vector3.one, 0.35f); // stretch and settle
            if (destination.HasValue)
            {
                var f = Scoring.Evaluate(guest, _hotel).Feedback;
                Theme.Sfx(f == Feedback.Red ? "creak" : "drop", 0.7f, f == Feedback.Green ? 1f : 0.85f);
            }
            else Theme.Sfx("page", 0.5f);
        }

        void UseHint()
        {
            if (_hintsLeft <= 0) return;
            var solution = _opt.Solution?.Invoke();
            if (solution == null) return;
            foreach (var g in _hotel.Guests)
            {
                if (g.Locked || !solution.TryGetValue(g.Def.Id, out var number)) continue;
                var cell = new Cell(number / 100 - 1, number % 100 - 1);
                if (g.Room == cell) continue;
                if (!_session.CanMove(g, cell)) continue;
                _hintsLeft--;
                _session.Move(g, cell);
                _focused = g;
                Theme.Sfx("drop", 0.7f, 1.2f);
                if (_rooms.TryGetValue(cell, out var w)) Tween.Punch(w.Frame.transform, 0.08f);
                RefreshCard();
                RefreshButtons();
                return;
            }
        }

        void ShowPreview(GuestState guest)
        {
            if (guest == null) return;
            foreach (var kv in _rooms)
            {
                bool canMove = _session.CanMove(guest, kv.Key) || guest.Room == kv.Key;
                if (guest.IsRiddle && canMove)
                {
                    kv.Value.Frame.color = Palette.Aura;
                    kv.Value.Mark.text = "?";
                    kv.Value.Mark.color = Palette.Aura;
                    kv.Value.Mark.transform.parent.GetComponent<Image>().enabled = true;
                    kv.Value.Pulse.enabled = false;
                    continue;
                }
                var f = canMove ? Scoring.Preview(guest, kv.Key, _hotel) : Feedback.Red;
                kv.Value.Frame.color = Palette.For(f);
                kv.Value.Mark.text = !canMove ? (_midnight ? "no swaps" : "locked") : f == Feedback.Green ? "OK" : f == Feedback.Yellow ? "~ meh" : "X no";
                kv.Value.Mark.color = Palette.For(f);
                kv.Value.Mark.transform.parent.GetComponent<Image>().enabled = true;
                kv.Value.Pulse.enabled = f == Feedback.Green;
            }
        }

        void ClearPreview()
        {
            foreach (var w in _rooms.Values)
            {
                w.Frame.color = Palette.DeepPurple;
                w.Mark.text = "";
                w.Mark.transform.parent.GetComponent<Image>().enabled = false;
                w.Pulse.enabled = false;
            }
        }

        static void SetInRoomLook(GuestWidgets w, bool inRoom)
        {
            w.Card.color = inRoom ? new Color(0, 0, 0, 0) : Palette.PaperCream;
            w.Name.enabled = !inRoom;
            w.Dots.gameObject.SetActive(!inRoom);
            w.Ghost.rectTransform.anchorMin = inRoom ? Vector2.zero : new Vector2(0.1f, 0.22f);
            w.Ghost.rectTransform.anchorMax = inRoom ? Vector2.one : new Vector2(0.9f, 0.98f);
            w.Ghost.rectTransform.offsetMin = w.Ghost.rectTransform.offsetMax = Vector2.zero;
            var bob = w.Ghost.GetComponent<Bob>();
            if (bob != null) bob.Rebase();
        }

        // ---------------------------------------------------------------- refresh

        void Refresh()
        {
            foreach (var g in _hotel.Guests)
                if (!_guests.ContainsKey(g))
                {
                    _guests[g] = BuildGuest(g);
                    _lastStars[g] = 0;
                    Tween.Scale(_guests[g].Root, Vector3.zero, Vector3.one, 0.35f);
                }
            foreach (var kv in _guests)
            {
                var g = kv.Key;
                var w = kv.Value;
                if (g.Room.HasValue)
                {
                    w.Root.SetParent(_rooms[g.Room.Value].Slot, false);
                    w.Root.anchorMin = w.Root.anchorMax = w.Root.pivot = new Vector2(0.5f, 0.5f);
                    w.Root.anchoredPosition = Vector2.zero;
                    w.Root.sizeDelta = new Vector2(_guestSize, _guestSize);
                    SetInRoomLook(w, true);
                }
                else
                {
                    if (w.Root.parent != _queueContent) w.Root.SetParent(_queueContent, false);
                    SetInRoomLook(w, false);
                }
                int stars = Scoring.ScoreGuest(g, _hotel);
                w.Ghost.sprite = Theme.Ghost(g.Def, g.IsPlaced && !g.IsRiddle ? Theme.ExpressionFor(stars) : "neutral");
            }
            int i = 0;
            foreach (var g in _hotel.Guests)
                if (!g.IsPlaced) _guests[g].Root.SetSiblingIndex(i++); // arrival order

            foreach (var kv in _rooms)
            {
                var room = _hotel.Room(kv.Key);
                RefreshIcons(kv.Value.Icons, room);
                var occupant = room.Occupant;
                bool secret = occupant != null && occupant.IsRiddle;
                int stars = occupant != null ? Scoring.ScoreGuest(occupant, _hotel) : 0;
                for (int s = 0; s < kv.Value.Stars.Length; s++)
                {
                    kv.Value.Stars[s].enabled = occupant != null;
                    kv.Value.Stars[s].sprite = Theme.Sprite(secret ? "star_empty" : s < stars ? "star" : "star_empty");
                }
            }
            AnimateStarChanges();

            int waiting = 0;
            foreach (var g in _hotel.Guests) if (!g.IsPlaced) waiting++;
            _queueTitle.text = _midnight ? (waiting > 0 ? $"WALK-IN · place or leave ({waiting})" : "ARRIVAL QUEUE") : $"ARRIVAL QUEUE · {waiting}";
            if (_midnight) _title.text = $"Midnight · {_session.TokensLeft} swap{(_session.TokensLeft == 1 ? "" : "s")} left";
            _rating.text = $"Rating {Scoring.NightRating(_hotel):0.0} / 3";

            RefreshCard();
            RefreshButtons();
        }

        /// <summary>Stars pop in one at a time with rising pitch; guests whose stars changed bounce (GDD §5.1, §8).</summary>
        void AnimateStarChanges()
        {
            foreach (var g in _hotel.Guests)
            {
                int now = Scoring.ScoreGuest(g, _hotel);
                int before = _lastStars.TryGetValue(g, out var b) ? b : 0;
                _lastStars[g] = now;
                if (!g.IsPlaced || now == before) continue;
                var w = _rooms[g.Room.Value];
                Tween.Punch(_guests[g].Ghost.transform, 0.1f);
                for (int s = before; s < now; s++)
                {
                    int star = s;
                    float delay = 0.08f + 0.12f * (star - before);
                    var img = w.Stars[star];
                    img.transform.localScale = Vector3.zero;
                    Tween.Scale(img.transform, Vector3.zero, Vector3.one, 0.3f, Ease.OutBack, null, delay);
                    Tween.Delay(img, delay, () => Theme.Sfx($"star_{star + 1}", 0.45f), "chime");
                }
            }
        }

        void RefreshIcons(RectTransform holder, RoomState room)
        {
            foreach (Transform c in holder) Destroy(c.gameObject);
            foreach (var t in room.Tags.Effective())
            {
                var icon = UiBuild.Picture($"Tag_{t}", holder, Theme.Sprite($"tag_{t}"));
                icon.rectTransform.sizeDelta = new Vector2(34, 34);
                if (room.Tags.IsTemporary(t))
                {
                    // Temporary (aura/event) tags get a violet ring so they read differently from permanent ones.
                    var ring = UiBuild.Picture("Temp", icon.transform, Theme.Sprite("circle"));
                    ring.color = Palette.Aura.WithAlpha(0.45f);
                    ring.rectTransform.Fill(-3);
                    ring.transform.SetAsFirstSibling();
                }
            }
        }

        void RefreshButtons()
        {
            _undo.interactable = _session.Commands.CanUndo;
            _redo.interactable = _session.Commands.CanRedo;
            _open.interactable = _midnight || _session.CanOpenDoors;
            _openPulse.enabled = !_midnight && _session.CanOpenDoors;
            if (_midnight) _title.text = $"Midnight · {_session.TokensLeft} swap{(_session.TokensLeft == 1 ? "" : "s")} left";
            _hint.GetComponentInChildren<Text>().text = $"Hint ({_hintsLeft})";
            _hint.interactable = _hintsLeft > 0;
        }

        void RefreshCard()
        {
            var paper = (RectTransform)_cardRoot.GetChild(0);
            foreach (Transform c in paper) Destroy(c.gameObject);
            if (_focused == null)
            {
                UiBuild.Label("Empty", paper, "Tap or drag a guest.", 26, Palette.Ink).rectTransform.Fill(20);
                return;
            }

            var g = _focused;
            var eval = Scoring.Evaluate(g, _hotel);
            UiBuild.Picture("Portrait", paper, Theme.Portrait(g.Def, g.IsPlaced && !g.IsRiddle ? Theme.ExpressionFor(eval.Stars) : "neutral"))
                .rectTransform.PlaceTopLeft(115, 18, 152, 152);
            Theme.Title("Name", paper, g.Def.Name, 34, Palette.Ink, TextAnchor.UpperCenter).rectTransform.PlaceTopLeft(0, 176, 382, 44);
            var era = _opt.Era(g.Def.Id);
            UiBuild.Label("Type", paper, $"{g.Def.Type.Display()}{(string.IsNullOrEmpty(era) ? "" : " · " + era)}{(g.Locked ? " · pre-booked" : "")}",
                20, Palette.Ink.WithAlpha(0.7f), TextAnchor.UpperCenter).rectTransform.PlaceTopLeft(0, 220, 382, 28);
            UiBuild.Label("Where", paper, g.IsPlaced ? $"Room {_hotel.Room(g.Room.Value).Number} · {(g.IsRiddle ? "?" : eval.Stars.ToString())} / 3 stars" : "Waiting in the queue",
                20, Palette.Brass, TextAnchor.UpperCenter, FontStyle.Bold).rectTransform.PlaceTopLeft(0, 250, 382, 28);

            float y = 290;
            void Heading(string text, Color c)
            {
                UiBuild.Label(text, paper, text, 18, c, TextAnchor.UpperLeft, FontStyle.Bold).rectTransform.PlaceTopLeft(24, y, 340, 24);
                y += 26;
            }
            // Every icon also has a text label, and status uses words + colour (GDD §7.2: never colour alone).
            void Row(IRule r, Color dot, bool? ok)
            {
                Sprite icon = r is RequiresTagRule req ? Theme.Sprite($"tag_{req.Tag}") : r is ForbidsTagRule fb ? Theme.Sprite($"tag_{fb.Tag}") : null;
                var img = UiBuild.Picture("Icon", paper, icon ?? Theme.Sprite("circle"));
                if (icon == null) img.color = dot;
                img.rectTransform.PlaceTopLeft(24, y, 30, 30);
                if (r is ForbidsTagRule) UiBuild.Label("No", img.transform, "/", 34, Palette.Broken, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Fill(-4);
                string status = ok == null ? "" : ok.Value ? "  <color=#3E8E3E><b>ok</b></color>" : "  <color=#D9534F><b>no</b></color>";
                UiBuild.Label("Text", paper, r.Label + status, 22, Palette.Ink, TextAnchor.MiddleLeft).rectTransform.PlaceTopLeft(62, y - 2, 310, 34);
                y += 36;
            }

            if (g.IsRiddle)
            {
                // Riddle night (GDD §4.9): deduce this guest's rules from three clues.
                Heading("MYSTERY GUEST · CLUES", Palette.Aura);
                foreach (var clue in g.Def.RiddleClues)
                {
                    UiBuild.Label("Clue", paper, "\u2022 " + clue, 21, Palette.Ink, TextAnchor.UpperLeft).rectTransform.PlaceTopLeft(24, y, 340, 80);
                    y += 82;
                }
                UiBuild.Label("RiddleNote", paper, "Their stars stay secret until dawn.", 19, Palette.Ink.WithAlpha(0.6f), TextAnchor.UpperLeft)
                    .rectTransform.PlaceTopLeft(24, y, 340, 30);
                return;
            }

            if (g.Def.Needs.Count > 0)
            {
                Heading("NEEDS", Palette.Broken);
                foreach (var r in g.Def.Needs) Row(r, Palette.Broken, g.IsPlaced ? !eval.BrokenNeeds.Contains(r) : (bool?)null);
                y += 6;
            }
            if (g.Def.Likes.Count > 0)
            {
                Heading("LIKES", Palette.Brass);
                foreach (var r in g.Def.Likes) Row(r, Palette.Partial, g.IsPlaced && eval.MetLikes.Contains(r) ? true : (bool?)null);
                y += 6;
            }
            if (g.Def.Dislikes.Count > 0)
            {
                Heading("DISLIKES", new Color(0.42f, 0.4f, 0.47f));
                foreach (var r in g.Def.Dislikes) Row(r, new Color(0.6f, 0.57f, 0.62f), g.IsPlaced ? !eval.BrokenDislikes.Contains(r) : (bool?)null);
                y += 6;
            }
            if (g.Def.Hidden != null)
            {
                Heading("HIDDEN", Palette.Aura);
                if (g.HiddenRevealed) Row(g.Def.Hidden, Palette.Aura, g.IsPlaced ? g.Def.Hidden.Evaluate(g, _hotel) : (bool?)null);
                else
                {
                    UiBuild.Label("HiddenHint", paper, $"<b>?</b>  <i>\u201C{g.Def.HiddenHint}\u201D</i>", 21, Palette.Ink, TextAnchor.UpperLeft)
                        .rectTransform.PlaceTopLeft(24, y, 340, 56);
                    y += 58;
                }
                y += 6;
            }
            if (g.Def.Aura != null)
            {
                var a = g.Def.Aura;
                Heading("AURA", Palette.Aura);
                var text = !string.IsNullOrEmpty(a.AddTag) ? $"{a.Name}: adds {a.AddTag}" : $"{a.Name}: removes {a.RemoveTag}";
                UiBuild.Label("Aura", paper, text + (a.Directions == Direction.All ? " next door" : $" ({a.Directions})"), 22, Palette.Ink, TextAnchor.UpperLeft)
                    .rectTransform.PlaceTopLeft(24, y, 340, 30);
                y += 36;
            }
            foreach (var t in eval.UnpleasantTags)
            {
                UiBuild.Label("Unpleasant", paper, $"<color=#D9534F><b>no</b></color> {t} room: -1 star", 22, Palette.Ink, TextAnchor.UpperLeft)
                    .rectTransform.PlaceTopLeft(24, y, 340, 30);
                y += 32;
            }

            // Story candles (GDD §5.2): one per stay, lit when done.
            int done = _opt.StaysDone(g.Def.Id);
            if (g.Def.Stays < 10)
            {
                UiBuild.Label("Story", paper, $"STORY · stay {Mathf.Min(done + 1, g.Def.Stays)} / {g.Def.Stays}", 18, Palette.Brass, TextAnchor.UpperLeft, FontStyle.Bold)
                    .rectTransform.PlaceTopLeft(24, 718, 260, 24);
                for (int s = 0; s < g.Def.Stays; s++)
                {
                    var candle = UiBuild.Panel($"Candle{s}", paper, s < done ? Palette.LampAmber : Palette.DuskViolet.WithAlpha(0.3f), raycastTarget: false);
                    candle.rectTransform.PlaceTopLeft(24 + s * 30, 746, 18, 30);
                }
            }
        }
    }
}
