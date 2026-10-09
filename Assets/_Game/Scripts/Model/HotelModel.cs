using System;
using System.Collections.Generic;
using ChuchuGames.GridPuzzle;

namespace GhostHotel.Model
{
    /// <summary>
    /// The hotel for one night: a grid of rooms and the guests in or waiting for them.
    /// Pure C#, so the view, the undo stack and the solver all share it.
    /// </summary>
    public sealed class HotelModel
    {
        readonly List<GuestState> _guests = new List<GuestState>();

        public Grid<RoomState> Rooms { get; }
        public IReadOnlyList<GuestState> Guests => _guests;
        public int Floors => Rooms.Rows;
        public int RoomsPerFloor => Rooms.Cols;

        /// <summary>Raised after any placement change, once tags have been recomputed.</summary>
        public event Action Changed;

        /// <param name="autoFloorTags">
        /// Adds Attic to the top floor and Basement to the bottom floor (GDD §4.2). Only applied
        /// when there are at least two floors, so a one-floor hotel isn't both at once.
        /// </param>
        public HotelModel(int floors, int roomsPerFloor, bool autoFloorTags = true)
        {
            Rooms = new Grid<RoomState>(floors, roomsPerFloor, c => new RoomState(c));
            if (autoFloorTags && floors >= 2)
            {
                foreach (var c in Rooms.CellsInRow(floors - 1)) Rooms[c].Tags.AddBase(Tags.Attic);
                foreach (var c in Rooms.CellsInRow(0)) Rooms[c].Tags.AddBase(Tags.Basement);
            }
        }

        public RoomState Room(Cell cell) => Rooms[cell];
        public GuestState GuestAt(Cell cell) => Rooms[cell].Occupant;
        public bool HasTag(Cell cell, string tag) => Rooms[cell].Tags.Has(tag);

        public GuestState AddGuest(GuestDef def)
        {
            var g = new GuestState(def);
            _guests.Add(g);
            return g;
        }

        public IEnumerable<GuestState> NeighbourGuests(Cell cell, Direction directions = Direction.All, int range = 1)
        {
            foreach (var n in Rooms.Neighbours(cell, directions, range))
            {
                var occupant = Rooms[n].Occupant;
                if (occupant != null) yield return occupant;
            }
        }

        /// <summary>True when every guest who must stay has a room (optional walk-ins may be turned away).</summary>
        public bool AllGuestsPlaced
        {
            get
            {
                foreach (var g in _guests)
                    if (!g.IsPlaced && !g.Optional) return false;
                return true;
            }
        }

        /// <summary>Guests who count tonight: everyone except optional walk-ins left in the queue (turned away).</summary>
        public IEnumerable<GuestState> CountedGuests
        {
            get
            {
                foreach (var g in _guests)
                    if (g.IsPlaced || !g.Optional) yield return g;
            }
        }

        /// <summary>False when the guest or the target room's occupant is locked in place.</summary>
        public bool CanPlace(GuestState guest, Cell? target)
        {
            if (guest.Locked) return false;
            if (target.HasValue && Rooms[target.Value].Occupant is GuestState other && other != guest && other.Locked) return false;
            return true;
        }

        /// <summary>
        /// Moves a guest to a room, or back to the queue when <paramref name="target"/> is null.
        /// If the room is occupied, the occupant swaps into the guest's old room (or the queue).
        /// </summary>
        public void Place(GuestState guest, Cell? target)
        {
            PlaceSilently(guest, target);
            Refresh();
        }

        /// <summary>Puts every given guest back at the given position in one change.</summary>
        public void Restore(IReadOnlyDictionary<GuestState, Cell?> positions)
        {
            foreach (var g in positions.Keys) SetRoom(g, null);
            foreach (var kv in positions)
                if (kv.Value.HasValue) SetRoom(kv.Key, kv.Value);
            Refresh();
        }

        public Dictionary<GuestState, Cell?> SnapshotPositions()
        {
            var d = new Dictionary<GuestState, Cell?>();
            foreach (var g in _guests) d[g] = g.Room;
            return d;
        }

        /// <summary>
        /// Evaluates a placement without keeping it, for the drag preview. Neighbours' views
        /// are not notified.
        /// </summary>
        public T WhatIf<T>(GuestState guest, Cell? target, Func<T> evaluate)
        {
            var before = new Dictionary<GuestState, Cell?> { [guest] = guest.Room };
            if (target.HasValue && Rooms[target.Value].Occupant is GuestState other && other != guest)
                before[other] = other.Room;

            PlaceSilently(guest, target);
            RecomputeModifiers();
            try
            {
                return evaluate();
            }
            finally
            {
                foreach (var g in before.Keys) SetRoom(g, null);
                foreach (var kv in before)
                    if (kv.Value.HasValue) SetRoom(kv.Key, kv.Value);
                RecomputeModifiers();
            }
        }

        void PlaceSilently(GuestState guest, Cell? target)
        {
            if (!_guests.Contains(guest)) throw new ArgumentException("Guest is not in this hotel", nameof(guest));
            if (!CanPlace(guest, target)) throw new InvalidOperationException($"{guest} can't move there: a locked guest is involved");
            var from = guest.Room;
            if (from == target) return;

            GuestState displaced = target.HasValue ? Rooms[target.Value].Occupant : null;
            SetRoom(guest, null);
            if (displaced != null)
            {
                SetRoom(displaced, null);
                if (from.HasValue) SetRoom(displaced, from);
            }
            if (target.HasValue) SetRoom(guest, target);
        }

        void SetRoom(GuestState guest, Cell? cell)
        {
            if (guest.Room.HasValue) Rooms[guest.Room.Value].Occupant = null;
            guest.Room = cell;
            if (cell.HasValue)
            {
                var room = Rooms[cell.Value];
                if (room.Occupant != null) throw new InvalidOperationException($"Room {room.Number} is already occupied");
                room.Occupant = guest;
            }
        }

        void Refresh()
        {
            RecomputeModifiers();
            Changed?.Invoke();
        }

        /// <summary>Night-wide tag changes (midnight events, curses), applied after auras.</summary>
        public List<ITagModifier> Modifiers { get; } = new List<ITagModifier>();

        /// <summary>Extra aura reach per ghost type (Restless Night: a Poltergeist's noise reaches 2 rooms).</summary>
        public Dictionary<GhostType, int> AuraRangeBonus { get; } = new Dictionary<GhostType, int>();

        /// <summary>Night-wide rules every guest is scored against as dislikes (Séance Downstairs).</summary>
        public List<IRule> ExtraDislikes { get; } = new List<IRule>();

        /// <summary>Re-applies auras and modifiers; call after changing <see cref="Modifiers"/>.</summary>
        public void RefreshModifiers() => Refresh();

        /// <summary>
        /// Fast path for the solver: puts guest i in rooms[i] (null = queue), recomputes tags once
        /// and raises no events.
        /// </summary>
        public void ApplyArrangement(IReadOnlyList<GuestState> guests, IReadOnlyList<Cell?> rooms)
        {
            foreach (var g in guests) SetRoom(g, null);
            for (int i = 0; i < guests.Count; i++)
                if (rooms[i].HasValue) SetRoom(guests[i], rooms[i]);
            RecomputeModifiers();
        }

        /// <summary>
        /// Rebuilds temporary tags: auras first (GDD §4.4: "auras are applied before rules are
        /// checked"), then night modifiers. Removals always win over additions (see TagLayers).
        /// Brute force is fine on a 5x5 grid (GDD §9).
        /// </summary>
        void RecomputeModifiers()
        {
            foreach (var c in Rooms.Cells()) Rooms[c].Tags.ClearModifiers();

            foreach (var g in _guests)
            {
                var aura = g.Def.Aura;
                if (aura == null || !g.Room.HasValue) continue;
                int range = aura.Range + (AuraRangeBonus.TryGetValue(g.Def.Type, out var bonus) ? bonus : 0);
                foreach (var n in Rooms.Neighbours(g.Room.Value, aura.Directions, range))
                {
                    var tags = Rooms[n].Tags;
                    if (!string.IsNullOrEmpty(aura.AddTag)) tags.AddModifier(aura.AddTag);
                    if (!string.IsNullOrEmpty(aura.RemoveTag)) tags.RemoveModifier(aura.RemoveTag);
                }
            }

            foreach (var m in Modifiers) m.Apply(this);
        }
    }

    /// <summary>
    /// A night-wide change to room tags, re-applied on every recompute. It declares which tags it can add
    /// or remove so the solver can keep pruning on every other tag.
    /// </summary>
    public interface ITagModifier
    {
        void Apply(HotelModel hotel);
        IEnumerable<string> AddedTags { get; }
        IEnumerable<string> RemovedTags { get; }
    }
}
