using System;
using System.Collections.Generic;
using ChuchuGames.Dialogue;
using ChuchuGames.UI;
using GhostHotel.Model;
using UnityEngine;
using UnityEngine.UI;

namespace GhostHotel.View
{
    /// <summary>
    /// Arrival Desk (GDD §5.2): guests float across the counter one by one and say their line,
    /// then wait in the queue. "Begin check-in" opens the Hotel View.
    /// </summary>
    public sealed class ArrivalDeskScreen : MonoBehaviour
    {
        public sealed class Arrival
        {
            public GuestDef Def;
            /// <summary>Dialogue script for this stay (may be empty).</summary>
            public string Line;
            public string StayLabel;
        }

        RectTransform _root, _queue;
        List<Arrival> _arrivals;
        string _intro;
        Func<RectTransform, string, Action, GameObject> _talk;
        Action _onBegin;
        Button _begin;
        int _next;
        bool _skipping;
        GameObject _activeDialogue;
        RectTransform _activeGhost;

        /// <param name="talk">Shows a dialogue script over the given parent, then calls back. Returns the dialogue object.</param>
        public static ArrivalDeskScreen Create(RectTransform parent, int number, string title, string intro, List<Arrival> arrivals,
            Func<RectTransform, string, Action, GameObject> talk, Action onBegin)
        {
            var root = UiBuild.Rect("ArrivalDesk", parent).Fill();
            var s = root.gameObject.AddComponent<ArrivalDeskScreen>();
            s._root = root;
            s._arrivals = arrivals;
            s._intro = intro;
            s._talk = talk;
            s._onBegin = onBegin;
            s.Build(number, title);
            return s;
        }

        void Build(int number, string title)
        {
            Theme.Background(_root, "desk", Palette.DuskViolet);
            var top = UiBuild.Panel("TopBar", _root, Palette.NightNavy.WithAlpha(0.8f), rounded: false).rectTransform.Band(RectTransform.Edge.Top, 90);
            Theme.Title("Title", top, $"Night {number} · {title}", 40, Palette.PaperCream, TextAnchor.MiddleLeft).rectTransform.Fill(36);

            var queuePanel = UiBuild.Panel("Queue", _root, Palette.NightNavy.WithAlpha(0.75f));
            queuePanel.rectTransform.PlaceTopLeft(1560, 120, 330, 500);
            UiBuild.Label("QueueTitle", queuePanel.transform, "ARRIVAL QUEUE", 24, Palette.LampAmber, TextAnchor.UpperCenter, FontStyle.Bold)
                .rectTransform.PlaceTopLeft(0, 16, 330, 34);
            _queue = UiBuild.Rect("Cards", queuePanel.transform).PlaceTopLeft(15, 60, 300, 430);
            var grid = _queue.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(92, 92);
            grid.spacing = new Vector2 (12, 12);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;

            _begin = Theme.PrimaryButton("Begin", _root, "Begin check-in", () => _onBegin(), 36);
            ((RectTransform)_begin.transform).PlaceTopLeft(1440, 960, 450, 96);
            _begin.gameObject.SetActive(false);

            var skip = Theme.SecondaryButton("Skip", _root, "Skip", SkipAll, 24);
            ((RectTransform)skip.transform).PlaceTopLeft(1700, 18, 180, 56);

            if (!string.IsNullOrEmpty(_intro)) _activeDialogue = _talk(_root, _intro, NextGuest);
            else NextGuest();
        }

        void NextGuest()
        {
            _activeDialogue = null;
            if (_next >= _arrivals.Count)
            {
                Finish();
                return;
            }
            var a = _arrivals[_next++];
            var ghost = UiBuild.Picture($"Ghost_{a.Def.Id}", _root, Theme.Ghost(a.Def, "happy"));
            _activeGhost = ghost.rectTransform;
            _activeGhost.anchorMin = _activeGhost.anchorMax = _activeGhost.pivot = new Vector2(0.5f, 0.5f);
            _activeGhost.sizeDelta = new Vector2(300, 300);
            Theme.Sfx("pickup", 0.5f, 0.8f);

            if (_skipping)
            {
                SendToQueue(a, ghost.gameObject);
                NextGuest();
                return;
            }

            Tween.Move(_activeGhost, new Vector2(1200, 60), new Vector2(0, 60), 0.9f, Ease.OutQuad, () =>
            {
                if (_skipping) return;
                ghost.gameObject.AddComponent<Bob>();
                if (string.IsNullOrEmpty(a.Line)) Tween.Delay(ghost, 0.5f, () => Leave(a, ghost.gameObject));
                else _activeDialogue = _talk(_root, a.Line, () => Leave(a, ghost.gameObject));
            });
        }

        void Leave(ArrivalDeskScreen.Arrival a, GameObject ghost)
        {
            if (ghost == null) return;
            var bob = ghost.GetComponent<Bob>();
            if (bob) Destroy(bob);
            var rt = (RectTransform)ghost.transform;
            Tween.Move(rt, rt.anchoredPosition, new Vector2(700, 200), 0.45f, Ease.InOutQuad, () =>
            {
                SendToQueue(a, ghost);
                NextGuest();
            });
            Tween.Scale(rt, Vector3.one, Vector3.one * 0.3f, 0.45f, Ease.InOutQuad);
        }

        void SendToQueue(Arrival a, GameObject ghost)
        {
            if (ghost != null) Destroy(ghost);
            var card = UiBuild.Picture($"Card_{a.Def.Id}", _queue, Theme.Portrait(a.Def, "neutral"));
            Tween.Scale(card.transform, Vector3.zero, Vector3.one, 0.3f);
            Theme.Sfx("drop", 0.4f, 1.4f);
        }

        void Finish()
        {
            _activeGhost = null;
            _begin.gameObject.SetActive(true);
            _begin.gameObject.AddComponent<Pulse>();
            Tween.Scale(_begin.transform, Vector3.one * 0.6f, Vector3.one, 0.35f);
        }

        void SkipAll()
        {
            if (_skipping) return;
            _skipping = true;
            if (_activeDialogue != null) Destroy(_activeDialogue);
            if (_activeGhost != null)
            {
                Tween.Stop(_activeGhost, "move");
                Destroy(_activeGhost.gameObject);
                // The guest that was mid-arrival still needs a queue card.
                SendToQueue(_arrivals[_next - 1], null);
            }
            NextGuest();
        }
    }
}
