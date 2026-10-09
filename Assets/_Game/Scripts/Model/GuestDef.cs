using System.Collections.Generic;
using ChuchuGames.GridPuzzle;

namespace GhostHotel.Model
{
    /// <summary>
    /// One guest rule. Rules return true when the guest is happy with it, so needs, likes and
    /// dislikes all read the same way (GDD §9).
    /// </summary>
    public interface IRule
    {
        /// <summary>Card text, e.g. "Needs: Water".</summary>
        string Label { get; }
        bool Evaluate(GuestState guest, HotelModel hotel);
    }

    /// <summary>Immutable runtime definition of a guest, built from authoring data.</summary>
    public sealed class GuestDef
    {
        public string Id { get; }
        public string Name { get; }
        public GhostType Type { get; }
        public IReadOnlyList<IRule> Needs { get; }
        public IReadOnlyList<IRule> Likes { get; }
        public IReadOnlyList<IRule> Dislikes { get; }

        /// <summary>Optional effect on neighbouring rooms (GDD §4.3), or null.</summary>
        public AuraDef Aura { get; }

        /// <summary>Stays in this guest's story arc before they move on (GDD: 2–4).</summary>
        public int Stays { get; }

        /// <summary>Art file key: the ghost type unless the guest has their own art (e.g. "Bartholomew").</summary>
        public string ArtKey { get; set; }

        /// <summary>Unprinted rule (true = happy), or null. Scored at dawn as a dislike.</summary>
        public IRule Hidden { get; set; }
        public string HiddenHint { get; set; }
        public IReadOnlyList<string> RiddleClues { get; set; } = new string[0];

        public GuestDef(string id, string name, GhostType type,
            IReadOnlyList<IRule> needs = null, IReadOnlyList<IRule> likes = null, IReadOnlyList<IRule> dislikes = null,
            AuraDef aura = null, int stays = 3)
        {
            Id = id;
            Name = name;
            Type = type;
            Needs = needs ?? new IRule[0];
            Likes = likes ?? new IRule[0];
            Dislikes = dislikes ?? new IRule[0];
            Aura = aura;
            Stays = stays;
            ArtKey = type.ToString();
        }

        public override string ToString() => $"{Name} ({Type})";
    }

    /// <summary>
    /// Adds and/or removes a tag on neighbouring rooms, e.g. Poltergeist "Noisy" (adds Noisy to
    /// the sides and above/below), Banshee "Screech" (Noisy, vertical only), Wisp "Glow" (removes Dark).
    /// </summary>
    public sealed class AuraDef
    {
        public string Name { get; }
        public string AddTag { get; }
        public string RemoveTag { get; }
        public Direction Directions { get; }
        public int Range { get; }

        public AuraDef(string name, string addTag = null, string removeTag = null,
            Direction directions = Direction.All, int range = 1)
        {
            Name = name;
            AddTag = addTag;
            RemoveTag = removeTag;
            Directions = directions;
            Range = range < 1 ? 1 : range;
        }
    }

    /// <summary>A guest during one night: their definition plus where they are.</summary>
    public sealed class GuestState
    {
        public GuestDef Def { get; }

        /// <summary>The guest's room, or null while they wait in the Arrival Queue.</summary>
        public Cell? Room { get; internal set; }

        public bool IsPlaced => Room.HasValue;

        /// <summary>Pre-placed by the night: can't be moved, swapped or cleared.</summary>
        public bool Locked { get; internal set; }

        /// <summary>A midnight walk-in (Unexpected Guest): may be placed or turned away (left in the queue).</summary>
        public bool Optional { get; internal set; }

        /// <summary>The player knows this guest's hidden rule (revealed on an earlier stay, by staff or a keepsake).</summary>
        public bool HiddenRevealed { get; set; }

        /// <summary>Tonight's Riddle guest: rules are shown only as clues and live stars are hidden.</summary>
        public bool IsRiddle { get; set; }

        public GuestState(GuestDef def) => Def = def;

        public override string ToString() => Def.ToString();
    }

    public sealed class RoomState
    {
        public Cell Cell { get; }
        public TagLayers Tags { get; }
        public GuestState Occupant { get; internal set; }

        /// <summary>Display number: floor then room, e.g. 203. Floors start at 1.</summary>
        public int Number => (Cell.Row + 1) * 100 + Cell.Col + 1;

        public RoomState(Cell cell, IEnumerable<string> baseTags = null)
        {
            Cell = cell;
            Tags = baseTags == null ? new TagLayers() : new TagLayers(baseTags);
        }
    }
}
