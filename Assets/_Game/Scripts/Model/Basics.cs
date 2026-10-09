namespace GhostHotel.Model
{
    public enum GhostType
    {
        Poltergeist,
        Weeper,
        Victorian,
        Drowned,
        ChildGhost,
        Banshee,
        Wisp,
        HeadlessKnight,
    }

    public static class GhostTypes
    {
        /// <summary>Player-facing name: "HeadlessKnight" → "Headless Knight".</summary>
        public static string Display(this GhostType type)
        {
            switch (type)
            {
                case GhostType.ChildGhost: return "Child Ghost";
                case GhostType.HeadlessKnight: return "Headless Knight";
                default: return type.ToString();
            }
        }
    }

    /// <summary>Room tag ids (GDD §4.2) plus virtual tags that only auras and events add.</summary>
    public static class Tags
    {
        public const string Dark = "Dark";
        public const string Water = "Water";
        public const string Warm = "Warm";
        public const string Attic = "Attic";
        public const string Basement = "Basement";
        public const string Mirror = "Mirror";
        public const string Music = "Music";
        public const string Garden = "Garden";
        public const string Cursed = "Cursed";

        // Virtual tags (from auras / events).
        public const string Noisy = "Noisy";
        public const string Cold = "Cold";

        /// <summary>Tags that cost a star unless the guest needs or likes them.</summary>
        public static readonly string[] Unpleasant = { Noisy, Cold, Cursed };

        public static readonly string[] All = { Dark, Water, Warm, Attic, Basement, Mirror, Music, Garden, Cursed, Noisy, Cold };
    }

    /// <summary>Live drag-preview colour (GDD §3): green = all fine, yellow = a dislike broken, red = a need broken.</summary>
    public enum Feedback
    {
        Green,
        Yellow,
        Red,
    }
}
