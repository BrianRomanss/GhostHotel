using ChuchuGames.GridPuzzle;

namespace GhostHotel.Model.Rules
{
    /// <summary>Matches a guest by ghost type or by a specific guest id ("Next to Lady Ashworth").</summary>
    public sealed class GuestMatcher
    {
        public GhostType? Type { get; }
        public string GuestId { get; }
        public string Display { get; }

        GuestMatcher(GhostType? type, string guestId, string display)
        {
            Type = type;
            GuestId = guestId;
            Display = display;
        }

        public static GuestMatcher OfType(GhostType type) => new GuestMatcher(type, null, type.Display());
        public static GuestMatcher Guest(string id, string name) => new GuestMatcher(null, id, name);

        public bool Matches(GuestState g) => GuestId != null ? g.Def.Id == GuestId : g.Def.Type == Type;
    }

    public enum FloorEnd
    {
        Top,
        Bottom,
    }

    /// <summary>"Top two floors only" / "Ground floor only" — floor index within N of an end.</summary>
    public sealed class FloorRule : IRule
    {
        public FloorEnd End { get; }
        public int Count { get; }

        public FloorRule(FloorEnd end, int count)
        {
            End = end;
            Count = count < 1 ? 1 : count;
        }

        public string Label =>
            End == FloorEnd.Top
                ? (Count == 1 ? "Top floor only" : $"Top {Count} floors only")
                : (Count == 1 ? "Ground floor only" : $"Bottom {Count} floors only");

        public bool Evaluate(GuestState guest, HotelModel hotel)
        {
            if (!guest.Room.HasValue) return false;
            int row = guest.Room.Value.Row;
            return End == FloorEnd.Top ? row >= hotel.Floors - Count : row < Count;
        }
    }

    /// <summary>"Next to Lady Ashworth" — at least one matching orthogonal neighbour.</summary>
    public sealed class NeighbourWantRule : IRule
    {
        public GuestMatcher Match { get; }
        public NeighbourWantRule(GuestMatcher match) => Match = match;

        public string Label => $"Next to {Match.Display}";

        public bool Evaluate(GuestState guest, HotelModel hotel)
        {
            if (!guest.Room.HasValue) return false;
            foreach (var n in hotel.NeighbourGuests(guest.Room.Value))
                if (Match.Matches(n)) return true;
            return false;
        }
    }

    /// <summary>"No neighbours" — every orthogonal neighbour room is empty.</summary>
    public sealed class IsolationRule : IRule
    {
        public string Label => "No neighbours";

        public bool Evaluate(GuestState guest, HotelModel hotel)
        {
            if (!guest.Room.HasValue) return false;
            foreach (var _ in hotel.NeighbourGuests(guest.Room.Value)) return false;
            return true;
        }
    }

    /// <summary>"Wants a neighbour" — at least one orthogonal neighbour.</summary>
    public sealed class CompanyRule : IRule
    {
        public string Label => "Wants a neighbour";

        public bool Evaluate(GuestState guest, HotelModel hotel)
        {
            if (!guest.Room.HasValue) return false;
            foreach (var _ in hotel.NeighbourGuests(guest.Room.Value)) return true;
            return false;
        }
    }

    public enum VerticalRelation
    {
        /// <summary>The guest is directly below the match (match is in the room above).</summary>
        Below,
        /// <summary>The guest is directly above the match.</summary>
        Above,
    }

    /// <summary>"Below a Victorian" — that guest/type in the room directly above (or below).</summary>
    public sealed class VerticalRule : IRule
    {
        public VerticalRelation Relation { get; }
        public GuestMatcher Match { get; }

        public VerticalRule(VerticalRelation relation, GuestMatcher match)
        {
            Relation = relation;
            Match = match;
        }

        public string Label => $"{Relation} {Article(Match)}";

        public bool Evaluate(GuestState guest, HotelModel hotel)
        {
            if (!guest.Room.HasValue) return false;
            var dir = Relation == VerticalRelation.Below ? Direction.Up : Direction.Down;
            foreach (var n in hotel.NeighbourGuests(guest.Room.Value, dir))
                if (Match.Matches(n)) return true;
            return false;
        }

        static string Article(GuestMatcher m) => m.GuestId != null ? m.Display : $"a {m.Display}";
    }

    /// <summary>"Corner room" — first or last column of the floor.</summary>
    public sealed class EdgeRule : IRule
    {
        public string Label => "Corner room";

        public bool Evaluate(GuestState guest, HotelModel hotel) =>
            guest.Room.HasValue && hotel.Rooms.IsEdgeColumn(guest.Room.Value);
    }

    /// <summary>"Max 2 ghosts on my floor" — occupied rooms on the guest's floor, including theirs, ≤ N.</summary>
    public sealed class FloorCountRule : IRule
    {
        public int Max { get; }
        public FloorCountRule(int max) => Max = max;

        public string Label => $"Max {Max} ghosts on my floor";

        public bool Evaluate(GuestState guest, HotelModel hotel)
        {
            if (!guest.Room.HasValue) return false;
            int count = 0;
            foreach (var c in hotel.Rooms.CellsInRow(guest.Room.Value.Row))
                if (hotel.GuestAt(c) != null) count++;
            return count <= Max;
        }
    }
}
