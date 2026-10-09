using GhostHotel.Model;
using UnityEngine;

namespace GhostHotel.View
{
    /// <summary>Master palette from GDD §7.2–7.3.</summary>
    public static class Palette
    {
        public static readonly Color NightNavy = Hex(0x1B1B3A);
        public static readonly Color DeepPurple = Hex(0x2E2654);
        public static readonly Color DuskViolet = Hex(0x4B3F7A);
        public static readonly Color LampAmber = Hex(0xF2A541);
        public static readonly Color WarmGlow = Hex(0xFFD58A);
        // Aged from the GDD's #F6EEDC / #B08A3E so panels read as old ledger paper and tarnished fittings.
        public static readonly Color PaperCream = Hex(0xEAE0C8);
        public static readonly Color Brass = Hex(0x9A7B3E);
        public static readonly Color Ink = Hex(0x2A2333);

        // Rule feedback: always paired with a shape/word for colour-blind players.
        public static readonly Color Valid = Hex(0x5CB85C);
        public static readonly Color Partial = Hex(0xE8B730);
        public static readonly Color Broken = Hex(0xD9534F);
        public static readonly Color Aura = Hex(0x9370DB);

        public static Color WithAlpha(this Color c, float a) => new Color(c.r, c.g, c.b, a);

        public static Color For(Feedback f) => f == Feedback.Green ? Valid : f == Feedback.Yellow ? Partial : Broken;

        public static Color Tint(GhostType type)
        {
            switch (type)
            {
                case GhostType.Weeper: return Hex(0xA9CFF2);
                case GhostType.Poltergeist: return Hex(0x9FE3B0);
                case GhostType.Victorian: return Hex(0xC9B6F2);
                case GhostType.Drowned: return Hex(0x7FD6CC);
                case GhostType.ChildGhost: return Hex(0xFFE29A);
                case GhostType.Banshee: return Hex(0xF2B8C6);
                case GhostType.Wisp: return Hex(0xFFF3B0);
                case GhostType.HeadlessKnight: return Hex(0xB9C2CF);
                default: return Color.white;
            }
        }

        static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }
}
