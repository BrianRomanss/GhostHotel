using System;
using System.Collections.Generic;
using ChuchuGames.Core;

namespace GhostHotel.Model
{
    /// <summary>GDD §3 night loop.</summary>
    public enum NightPhase
    {
        Arrival,
        CheckIn,
        Midnight,
        Dawn,
        Day,
    }

    /// <summary>
    /// One night from arrival to the day after: owns the hotel, the undo stack and the phase
    /// machine. Screens listen to <see cref="PhaseChanged"/>; pure C#, so the flow is unit-tested.
    /// </summary>
    public sealed class NightSession
    {
        readonly StateMachine<NightPhase> _phase;
        readonly GameProgress _progress;
        readonly List<GuestState> _walkIns = new List<GuestState>();

        public int Number { get; }
        public string Title { get; }
        public HotelModel Hotel { get; }
        public CommandStack Commands { get; } = new CommandStack();
        public IReadOnlyList<MidnightEvent> Events { get; }
        public bool HasMidnight => Events.Count > 0;
        /// <summary>Moves allowed during Midnight (GDD §4.7 Swap Tokens).</summary>
        public int SwapTokens { get; }
        /// <summary>Tokens spent so far; undo refunds them.</summary>
        public int TokensUsed { get; private set; }
        public int TokensLeft => SwapTokens - TokensUsed;
        /// <summary>Guests who walked in at midnight (Unexpected Guest).</summary>
        public IReadOnlyList<GuestState> WalkIns => _walkIns;
        public DawnResult Dawn { get; private set; }
        public NightPhase Phase => _phase.Current;

        public event Action<NightPhase> PhaseChanged;

        public Perks Perks { get; }

        public NightSession(int number, string title, HotelModel hotel, GameProgress progress,
            IReadOnlyList<MidnightEvent> events = null, int swapTokens = 0, Perks perks = null)
        {
            Perks = perks ?? Perks.None;
            Number = number;
            Title = title;
            Hotel = hotel;
            Events = events ?? new MidnightEvent[0];
            SwapTokens = Math.Max(0, swapTokens) + (Events.Count > 0 ? Perks.ExtraSwaps : 0);
            _progress = progress;
            _phase = new StateMachine<NightPhase>(NightPhase.Arrival)
                .Allow(NightPhase.Arrival, NightPhase.CheckIn)
                .Allow(NightPhase.CheckIn, NightPhase.Midnight, NightPhase.Dawn)
                .Allow(NightPhase.Midnight, NightPhase.Dawn)
                .Allow(NightPhase.Dawn, NightPhase.Day);
            _phase.OnEnter(NightPhase.Midnight, FireEvents);
            _phase.OnEnter(NightPhase.Dawn, () => Dawn = Economy.ApplyDawn(Hotel, Number, _progress, Perks));
            _phase.Changed += (_, to) => PhaseChanged?.Invoke(to);
        }

        public void BeginCheckIn() => _phase.Go(NightPhase.CheckIn);

        /// <summary>"Open the doors" stays disabled until every guest is placed (GDD §5.1).</summary>
        public bool CanOpenDoors => Phase == NightPhase.CheckIn && Hotel.AllGuestsPlaced;

        public void OpenDoors()
        {
            if (!CanOpenDoors) throw new InvalidOperationException("Every guest must be placed first");
            Commands.Clear();
            _phase.Go(HasMidnight ? NightPhase.Midnight : NightPhase.Dawn);
        }

        void FireEvents()
        {
            foreach (var e in Events)
            {
                var walkIn = e.Fire(Hotel);
                if (walkIn != null) _walkIns.Add(walkIn);
            }
        }

        /// <summary>
        /// Whether a drag is allowed right now. During check-in: anything legal. During Midnight: placing
        /// a walk-in (or sending one away) is free; moving anyone else costs a Swap Token.
        /// </summary>
        public bool CanMove(GuestState guest, ChuchuGames.GridPuzzle.Cell? target)
        {
            if (!Hotel.CanPlace(guest, target)) return false;
            if (Phase == NightPhase.CheckIn) return true;
            if (Phase != NightPhase.Midnight) return false;
            return CostOf(guest, target) <= TokensLeft;
        }

        /// <summary>Moves a guest through the undo stack, charging Swap Tokens at Midnight.</summary>
        public void Move(GuestState guest, ChuchuGames.GridPuzzle.Cell? target)
        {
            if (!CanMove(guest, target)) throw new InvalidOperationException("That move isn't allowed now");
            ICommand cmd = new PlaceGuestCommand(Hotel, guest, target);
            int cost = Phase == NightPhase.Midnight ? CostOf(guest, target) : 0;
            if (cost > 0) cmd = new CostedCommand(cmd, cost, this);
            Commands.Execute(cmd);
        }

        int CostOf(GuestState guest, ChuchuGames.GridPuzzle.Cell? target)
        {
            bool walkInOnly = guest.Optional &&
                              (!target.HasValue || Hotel.GuestAt(target.Value) == null || Hotel.GuestAt(target.Value).Optional);
            return walkInOnly ? 0 : 1;
        }

        /// <summary>Ends the Midnight phase: "Let the night pass".</summary>
        public void EndMidnight()
        {
            Commands.Clear();
            _phase.Go(NightPhase.Dawn);
        }

        public void ContinueToDay() => _phase.Go(NightPhase.Day);

        sealed class CostedCommand : ICommand
        {
            readonly ICommand _inner;
            readonly int _cost;
            readonly NightSession _session;

            public CostedCommand(ICommand inner, int cost, NightSession session)
            {
                _inner = inner;
                _cost = cost;
                _session = session;
            }

            public string Name => _inner.Name;

            public void Execute()
            {
                _inner.Execute();
                _session.TokensUsed += _cost;
            }

            public void Undo()
            {
                _inner.Undo();
                _session.TokensUsed -= _cost;
            }
        }
    }
}
