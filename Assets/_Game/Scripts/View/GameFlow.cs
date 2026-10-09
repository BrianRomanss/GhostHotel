using System;
using System.Collections.Generic;
using System.IO;
using ChuchuGames.Core;
using ChuchuGames.Dialogue;
using ChuchuGames.Dialogue.UI;
using ChuchuGames.GridPuzzle;
using ChuchuGames.UI;
using GhostHotel.Data;
using GhostHotel.Model;
using GhostHotel.Model.Content;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace GhostHotel.View
{
    /// <summary>
    /// Top-level flow (GDD §5 screen map): Main Menu → Save Slots → Lobby ⇄ Night Select / Ledger /
    /// Renovation → Arrival Desk → Hotel View → (Midnight) → Dawn → Lobby. Autosaves at Dawn and in
    /// the Lobby (GDD §9). Debug flags for automated runs:
    /// --gh-screen menu|lobby|desk|hotel|dawn, --gh-night N, --gh-solve, --gh-demo, --gh-open,
    /// --gh-tutorial, --gh-save-dir PATH.
    /// </summary>
    public sealed class GameFlow : MonoBehaviour
    {
        const string LastSlotKey = "gh.lastSlot";

        [SerializeField] NightCatalogSO catalog;
        [SerializeField] ArtLibrarySO art;

        SaveSlots<GameProgress> _saves;
        int _slot;
        GameProgress _progress;
        RectTransform _root;
        GameObject _screen;
        NightSession _session;
        HotelScreen _hotelScreen;
        ContentFactory _factory;
        float _sessionStart;

        public NightCatalogSO Catalog { get => catalog; set => catalog = value; }
        public ArtLibrarySO Art { get => art; set => art = value; }

        void Start()
        {
            Application.targetFrameRate = 60;
            EnsureEventSystem();
            GameSettings.Apply();
            Theme.Init(art);
            var canvas = UiBuild.Canvas("GameCanvas", new Vector2(1920, 1080));
            canvas.transform.SetParent(transform, false);
            _root = (RectTransform)canvas.transform;

            if (catalog == null || catalog.Count == 0)
            {
                UiBuild.Label("Error", _root, "No NightCatalog assigned. Run Tools/Ghost Hotel/Import Content, then Build Scenes.",
                    36, Palette.Broken).rectTransform.Fill(80);
                return;
            }
            _factory = catalog.CreateFactory();

            var saveDir = LaunchScreenshot.Arg("--gh-save-dir") ?? Path.Combine(Application.persistentDataPath, "saves");
            _saves = new SaveSlots<GameProgress>(saveDir, p => JsonUtility.ToJson(p, true), JsonUtility.FromJson<GameProgress>);

            switch (LaunchScreenshot.Arg("--gh-screen"))
            {
                case "lobby":
                    UseSlot(1, fresh: false);
                    ShowLobby();
                    OpenDebugPanel(LaunchScreenshot.Arg("--gh-panel"));
                    return;
                case "desk":
                case "hotel":
                case "dawn":
                    UseSlot(1, fresh: false);
                    StartNight(int.TryParse(LaunchScreenshot.Arg("--gh-night"), out var n) ? n : _progress.night);
                    return;
            }
            ShowMainMenu();
        }

        // ---------------------------------------------------------------- menu & slots

        void ShowMainMenu()
        {
            Theme.Music("music_lobby");
            int last = PlayerPrefs.GetInt(LastSlotKey, 0);
            var lastData = last >= 1 && last <= _saves.SlotCount ? _saves.Load(last) : null;
            string label = lastData == null ? "Continue" : lastData.night > catalog.Count ? "Continue · story complete" : $"Continue · Night {lastData.night}";
            Show(MainMenuScreen.Create(_root, lastData != null, label,
                () => { UseSlot(last, fresh: false); ShowLobby(); },
                () => SaveSlotsPanel.Create(_root, _saves, catalog.Count, slot => { UseSlot(slot, fresh: false); ShowLobby(); }),
                () => Overlays.Settings(_root),
                Application.Quit));
        }

        void UseSlot(int slot, bool fresh)
        {
            _slot = Mathf.Clamp(slot, 1, _saves.SlotCount);
            _progress = fresh ? null : _saves.Load(_slot);
            _progress = _progress ?? new GameProgress();
            _sessionStart = Time.realtimeSinceStartup;
            PlayerPrefs.SetInt(LastSlotKey, _slot);
            PlayerPrefs.Save();
        }

        // ---------------------------------------------------------------- lobby

        void ShowLobby()
        {
            Save();
            Theme.Music("music_lobby");
            Show(LobbyScreen.Create(_root, catalog, _progress, new LobbyScreen.Actions
            {
                StartNight = StartNight,
                Settings = () => Overlays.Settings(_root),
                MainMenu = () => { Save(); ShowMainMenu(); },
                Talk = script => Talk(_root, script, null),
                ReadPage = page => Talk(_root, EdithScript(page), null),
                Changed = () => { Save(); ShowLobby(); },
            }));
        }

        // ---------------------------------------------------------------- night

        void StartNight(int number)
        {
            var hotel = BuildHotel(number);
            if (hotel == null) return; // content errors are logged
            var night = catalog.Night(number);
            bool firstPlay = number >= _progress.night;
            var factory = catalog.CreateFactory();
            var events = factory.Midnight(night.data);
            foreach (var e in factory.Errors) Debug.LogError($"[Content] {e}", night);
            _session = new NightSession(number, night.data.title, hotel, _progress, events, night.data.swapTokens, CurrentPerks());
            _session.PhaseChanged += OnPhase;

            var screenArg = LaunchScreenshot.Arg("--gh-screen");
            if (screenArg == "hotel" || screenArg == "dawn") { BeginCheckIn(firstPlay); return; }

            var arrivals = new List<ArrivalDeskScreen.Arrival>();
            foreach (var g in hotel.Guests)
            {
                var data = catalog.Guest(g.Def.Id)?.data;
                var gp = _progress.Guest(g.Def.Id);
                arrivals.Add(new ArrivalDeskScreen.Arrival
                {
                    Def = g.Def,
                    Line = firstPlay ? StoryText.DeskLine(data, gp) : "", // includes "back to help" lines
                });
            }
            var intro = firstPlay ? night.data.intro : null;
            Show(ArrivalDeskScreen.Create(_root, number, night.data.title, intro, arrivals, Talk, () => BeginCheckIn(firstPlay)).gameObject);
            if (firstPlay && !string.IsNullOrEmpty(night.data.vaneOffer)) OfferFromVane(night.data);
        }

        HotelModel BuildHotel(int number)
        {
            var night = catalog.Night(number);
            if (night == null) return null;
            var f = catalog.CreateFactory();
            var hotel = f.Night(night.data, _progress.renovations);
            foreach (var e in f.Errors) Debug.LogError($"[Content] {e}", night);
            if (f.Errors.Count > 0) return null;
            CurrentPerks().ApplyTo(hotel, _progress);
            return hotel;
        }

        void BeginCheckIn(bool firstPlay)
        {
            _session.BeginCheckIn();
            var number = _session.Number;
            var screen = HotelScreen.Create(_root, _session, new HotelScreen.Options
            {
                Ectoplasm = _progress.ectoplasm,
                Calm = _progress.calm,
                StaysDone = id => _progress.Guest(id).staysDone,
                Era = id => catalog.Guest(id)?.data.era ?? "",
                Solution = () => NightSolver.Analyse(BuildHotel(number)).ExamplePerfect,
                OnOpenDoors = () => { if (_session.CanOpenDoors) _session.OpenDoors(); },
                OnEndMidnight = () => _session.EndMidnight(),
                HintBonus = _session.Perks.ExtraHints,
                OnPause = ShowPause,
            });
            _hotelScreen = screen;
            Show(screen.gameObject);

            var steps = catalog.Night(number).data.tutorial;
            if ((firstPlay || LaunchScreenshot.HasFlag("--gh-tutorial")) && steps != null && steps.Length > 0)
                Tween.Delay(screen, 0.35f, () => RunTutorial(screen, steps, 0), "tutorial");
            ApplyDebugFlags(screen);
        }

        void RunTutorial(HotelScreen screen, TutorialStep[] steps, int i)
        {
            if (screen == null || i >= steps.Length) return;
            Spotlight.Show(_root, screen.TutorialTarget(steps[i].target), steps[i].text, () => RunTutorial(screen, steps, i + 1),
                Theme.Sprite("hand"), Palette.LampAmber, Palette.PaperCream, Palette.Ink);
        }

        void ShowPause()
        {
            if (_root.Find("Modal") != null) return;
            Overlays.Pause(_root, () => StartNight(_session.Number), ShowLobby);
        }

        void OnPhase(NightPhase phase)
        {
            switch (phase)
            {
                case NightPhase.Midnight:
                    MidnightOverlay.Show(_root, _session.Events, _session.SwapTokens,
                        () => { if (_hotelScreen != null) _hotelScreen.EnterMidnight(); },
                        () => _session.EndMidnight());
                    break;
                case NightPhase.Dawn:
                    var dawn = _session.Dawn;
                    var page = catalog.Night(dawn.NightNumber).data.ledgerPage;
                    bool newPage = dawn.FirstPlay && !dawn.Hauntquake && !string.IsNullOrEmpty(page)
                                   && !_progress.ledgerPages.Contains(dawn.NightNumber);
                    if (newPage) _progress.ledgerPages.Add(dawn.NightNumber);
                    Save();
                    Show(DawnScreen.Create(_root, dawn, _progress, id => _factory.Guest(id), () =>
                    {
                        if (dawn.Hauntquake) StartNight(dawn.NightNumber);
                        else PlayCheckouts(dawn, 0, () =>
                        {
                            if (!newPage) { _session.ContinueToDay(); return; }
                            Theme.Sfx("page", 0.8f);
                            Talk(_root, "Bartholomew[sad]: A torn page just fluttered out of the ledger. That's Edith's handwriting.\n" + EdithScript(page),
                                () => _session.ContinueToDay());
                        });
                    }));
                    break;
                case NightPhase.Day:
                    ShowLobby();
                    break;
            }
        }

        /// <summary>Guests who moved on say goodbye and leave a keepsake (GDD §2 template).</summary>
        void PlayCheckouts(DawnResult dawn, int index, Action done)
        {
            while (index < dawn.Guests.Count && !dawn.Guests[index].MovedOn) index++;
            if (index >= dawn.Guests.Count) { done(); return; }
            var g = dawn.Guests[index];
            var data = catalog.Guest(g.GuestId)?.data;
            Theme.Sfx("move_on", 0.7f);
            var script = (data?.checkout ?? $"{g.Name}[happy]: Thank you, keeper.") +
                         (string.IsNullOrEmpty(data?.keepsake) ? "" : $"\nKeepsake: {data.keepsake} was added to the cabinet.");
            Talk(_root, script, () => PlayCheckouts(dawn, index + 1, done));
        }

        /// <summary>Turns a ledger page into dialogue lines spoken by Edith.</summary>
        public static string EdithScript(string page)
        {
            var lines = page.Replace("\r", "").Split('\n');
            for (int i = 0; i < lines.Length; i++) lines[i] = "Edith: " + lines[i];
            return string.Join("\n", lines);
        }

        /// <summary>Mr. Vane's offer (GDD §2): the first reply accepts. Money now, Calm lost, and he gets closer to the deed.</summary>
        void OfferFromVane(NightData night)
        {
            var lines = DialogueScript.Parse(night.vaneOffer, "Mr. Vane");
            if (lines.Count == 0) return;
            DialogueView.Play(_root, lines, DialogueStyleFor(), runner =>
            {
                if (runner.Choices.Count == 0 || runner.Choices[runner.Choices.Count - 1] != 0) return;
                _progress.vaneDeals++;
                _progress.ectoplasm += night.vaneOfferEctoplasm;
                _progress.calm = Mathf.Max(5, _progress.calm - 10);
                Theme.Sfx("creak", 0.8f, 0.7f);
                Save();
            });
        }

        Perks CurrentPerks() => Perks.Compute(_progress, id => catalog.Guest(id)?.data);

        // ---------------------------------------------------------------- dialogue

        GameObject Talk(RectTransform parent, string script, Action done)
        {
            var lines = DialogueScript.Parse(script, "Bartholomew");
            if (lines.Count == 0) { done?.Invoke(); return null; }
            var view = DialogueView.Play(parent, lines, DialogueStyleFor(), _ => done?.Invoke());
            return view.gameObject;
        }

        DialogueStyle DialogueStyleFor() => new DialogueStyle
        {
            Box = Palette.PaperCream,
            Ink = Palette.Ink,
            NamePlate = Palette.Brass,
            NameFont = Theme.Serif,
            Dim = new Color(0, 0, 0, 0.35f),
            CharsPerSecond = Tween.ReduceMotion ? 400f : 48f,
            Portrait = (speaker, expr) => SpeakerDef(speaker) is GuestDef d ? Theme.Portrait(d, expr)
                : Theme.CastArtKey(speaker) is string key ? Theme.Portrait(key, expr) : null,
            Voice = speaker => Theme.Art != null
                ? Theme.Art.Clip($"blip_{SpeakerDef(speaker)?.ArtKey ?? Theme.CastArtKey(speaker) ?? "Bartholomew"}") : null,
        };

        GuestDef SpeakerDef(string speaker)
        {
            if (string.IsNullOrEmpty(speaker)) return null;
            foreach (var g in catalog.guests)
                if (g != null && (g.data.name == speaker || g.Id == speaker)) return _factory.Guest(g.Id);
            return null;
        }

        // ---------------------------------------------------------------- plumbing

        void Show(GameObject screen)
        {
            if (_screen != null) Destroy(_screen);
            foreach (Transform child in _root)
                if (child.name == "Modal" || child.name == "Dialogue" || child.name == "Spotlight" || child.name == "Midnight") Destroy(child.gameObject);
            _screen = screen;
            if (_screen != null) _screen.transform.SetAsFirstSibling();
        }

        void Save()
        {
            if (_saves == null || _progress == null || _slot == 0) return;
            var now = Time.realtimeSinceStartup;
            _progress.playSeconds += now - _sessionStart;
            _sessionStart = now;
            _progress.savedAtUtc = DateTime.UtcNow.ToString("o");
            try
            {
                _saves.Save(_slot, _progress);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameFlow] Save failed: {e.Message}");
            }
        }

        void OnApplicationQuit() => Save();

        /// <summary>Opens an overlay for screenshot checks: slots, settings, pause, ledger, nights, reno, talk.</summary>
        void OpenDebugPanel(string panel)
        {
            switch (panel)
            {
                case "slots": SaveSlotsPanel.Create(_root, _saves, catalog.Count, _ => { }); break;
                case "settings": Overlays.Settings(_root); break;
                case "pause": Overlays.Pause(_root, () => { }, () => { }); break;
                case "ledger": LobbyScreen.LedgerPanel(_root, catalog, _progress, page => Talk(_root, EdithScript(page), null)); break;
                case "nights": LobbyScreen.NightSelectPanel(_root, catalog, _progress, _ => { }); break;
                case "reno": LobbyScreen.RenovationPanel(_root, catalog, _progress, () => { }); break;
                case "staff": LobbyScreen.StaffPanel(_root, catalog, _progress, () => { }); break;
                case "talk": Talk(_root, catalog.Guest("morrow")?.data.arrival + "\n> Welcome aboard | Not tonight", null); break;
            }
        }

        /// <summary>Automation hooks for screenshots and smoke tests.</summary>
        void ApplyDebugFlags(HotelScreen screen)
        {
            var hotel = _session.Hotel;
            bool demo = LaunchScreenshot.HasFlag("--gh-demo");
            if (LaunchScreenshot.HasFlag("--gh-solve") || demo)
            {
                var report = NightSolver.Analyse(BuildHotel(_session.Number));
                if (report.ExamplePerfect == null) return;
                GuestState held = null;
                foreach (var g in hotel.Guests)
                {
                    if (g.Locked || !report.ExamplePerfect.TryGetValue(g.Def.Id, out var number)) continue;
                    if (demo && held == null) { held = g; continue; } // leave one in the queue, previewed
                    _session.Move(g, new Cell(number / 100 - 1, number % 100 - 1));
                }
                if (held != null) screen.PreviewFor(held);
            }
            if ((LaunchScreenshot.HasFlag("--gh-open") || LaunchScreenshot.Arg("--gh-screen") == "dawn") && _session.CanOpenDoors)
                _session.OpenDoors();
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
}
