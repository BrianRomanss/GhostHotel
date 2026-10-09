using System;
using System.Collections.Generic;
using ChuchuGames.GridPuzzle;
using GhostHotel.Model.Content;

namespace GhostHotel.Model
{
    /// <summary>GDD §4.7 midnight events.</summary>
    public enum MidnightKind
    {
        /// <summary>Every Poltergeist's Noisy aura reaches one room further.</summary>
        RestlessNight,
        /// <summary>All Dark tags removed from one floor.</summary>
        PowerFlicker,
        /// <summary>One room gains Water; its neighbours gain Cold.</summary>
        BurstPipe,
        /// <summary>A new guest must be placed in an empty room or turned away.</summary>
        UnexpectedGuest,
        /// <summary>One room becomes Cursed: its guest loses a star (Act 2+).</summary>
        VanesVisit,
        /// <summary>Basement guests gain "wants company".</summary>
        SeanceDownstairs,
    }

    /// <summary>
    /// One midnight event for a night. Deterministic (room/floor/guest given in data), so the
    /// Night Validator can prove the night is still winnable after it fires.
    /// </summary>
    public sealed class MidnightEvent : ITagModifier
    {
        public MidnightKind Kind { get; }
        public string Title { get; }
        public string Description { get; }
        public Cell? Room { get; }
        /// <summary>0-based floor row for Power Flicker.</summary>
        public int FloorRow { get; }
        public GuestDef Guest { get; }

        public MidnightEvent(MidnightKind kind, string title, string description, Cell? room = null, int floorRow = 0, GuestDef guest = null)
        {
            Kind = kind;
            Title = title;
            Description = description;
            Room = room;
            FloorRow = floorRow;
            Guest = guest;
        }

        /// <summary>Applies the event to the hotel. Tag effects persist through every recompute until dawn.</summary>
        public GuestState Fire(HotelModel hotel)
        {
            GuestState walkIn = null;
            switch (Kind)
            {
                case MidnightKind.RestlessNight:
                    hotel.AuraRangeBonus[GhostType.Poltergeist] = (hotel.AuraRangeBonus.TryGetValue(GhostType.Poltergeist, out var b) ? b : 0) + 1;
                    break;
                case MidnightKind.SeanceDownstairs:
                    hotel.ExtraDislikes.Add(new BasementCompanyRule());
                    break;
                case MidnightKind.UnexpectedGuest:
                    walkIn = hotel.AddGuest(Guest);
                    walkIn.Optional = true;
                    break;
                default:
                    hotel.Modifiers.Add(this); // tag events
                    break;
            }
            hotel.RefreshModifiers();
            return walkIn;
        }

        IEnumerable<string> ITagModifier.AddedTags
        {
            get
            {
                switch (Kind)
                {
                    case MidnightKind.BurstPipe: return new[] { Tags.Water, Tags.Cold };
                    case MidnightKind.VanesVisit: return new[] { Tags.Cursed };
                    default: return new string[0];
                }
            }
        }

        IEnumerable<string> ITagModifier.RemovedTags =>
            Kind == MidnightKind.PowerFlicker ? new[] { Tags.Dark } : new string[0];

        void ITagModifier.Apply(HotelModel hotel)
        {
            switch (Kind)
            {
                case MidnightKind.PowerFlicker:
                    foreach (var c in hotel.Rooms.CellsInRow(FloorRow)) hotel.Room(c).Tags.RemoveModifier(Tags.Dark);
                    break;
                case MidnightKind.BurstPipe:
                    if (!Room.HasValue) return;
                    hotel.Room(Room.Value).Tags.AddModifier(Tags.Water);
                    foreach (var n in hotel.Rooms.Neighbours(Room.Value)) hotel.Room(n).Tags.AddModifier(Tags.Cold);
                    break;
                case MidnightKind.VanesVisit:
                    if (Room.HasValue) hotel.Room(Room.Value).Tags.AddModifier(Tags.Cursed);
                    break;
            }
        }
    }

    /// <summary>Séance Downstairs: anyone in the Basement now wants a neighbour (a dislike if alone).</summary>
    public sealed class BasementCompanyRule : IRule
    {
        public string Label => "Séance: Basement guests want company";

        public bool Evaluate(GuestState guest, HotelModel hotel)
        {
            if (!guest.Room.HasValue || !hotel.HasTag(guest.Room.Value, Tags.Basement)) return true;
            foreach (var _ in hotel.NeighbourGuests(guest.Room.Value)) return true;
            return false;
        }
    }
}
