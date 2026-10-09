namespace GhostHotel.Model.Rules
{
    /// <summary>"Needs: Water" — the guest's room has the tag.</summary>
    public sealed class RequiresTagRule : IRule
    {
        public string Tag { get; }
        public RequiresTagRule(string tag) => Tag = tag;

        public string Label => $"{Tag} room";

        public bool Evaluate(GuestState guest, HotelModel hotel) =>
            guest.Room.HasValue && hotel.HasTag(guest.Room.Value, Tag);
    }

    /// <summary>"Hates: Attic" — the guest's room lacks the tag.</summary>
    public sealed class ForbidsTagRule : IRule
    {
        public string Tag { get; }
        public ForbidsTagRule(string tag) => Tag = tag;

        public string Label => $"No {Tag}";

        public bool Evaluate(GuestState guest, HotelModel hotel) =>
            guest.Room.HasValue && !hotel.HasTag(guest.Room.Value, Tag);
    }

    /// <summary>"No Poltergeists next door" — no orthogonal neighbour of that type.</summary>
    public sealed class AvoidNeighbourTypeRule : IRule
    {
        public GhostType Type { get; }
        public AvoidNeighbourTypeRule(GhostType type) => Type = type;

        public string Label => $"No {Type.Display()} next door";

        public bool Evaluate(GuestState guest, HotelModel hotel)
        {
            if (!guest.Room.HasValue) return false;
            foreach (var n in hotel.NeighbourGuests(guest.Room.Value))
                if (n.Def.Type == Type) return false;
            return true;
        }
    }
}
