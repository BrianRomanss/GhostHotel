using System;

namespace GhostHotel.Model.Content
{
    // Authoring data: plain serializable fields so the same classes load from JSON (JsonUtility in
    // Unity, System.Text.Json in tests), live inside ScriptableObjects, and show in the Inspector.
    // Enums are strings on purpose: readable in JSON written by hand or by an AI, and validated by
    // ContentFactory with clear error messages.

    /// <summary>
    /// One rule. kind is one of: RequiresTag, ForbidsTag, Floor, AvoidNeighbour, WantNeighbour,
    /// Isolation, Company, Vertical, Edge, FloorCount.
    /// </summary>
    [Serializable]
    public class RuleData
    {
        public string kind;
        /// <summary>RequiresTag / ForbidsTag.</summary>
        public string tag;
        /// <summary>AvoidNeighbour / WantNeighbour / Vertical: match by ghost type…</summary>
        public string ghostType;
        /// <summary>…or WantNeighbour / Vertical: match one specific guest.</summary>
        public string guestId;
        /// <summary>Floor: "Top" or "Bottom".</summary>
        public string end;
        /// <summary>Floor: how many floors from that end. FloorCount: max ghosts on the floor.</summary>
        public int count;
        /// <summary>Vertical: "Below" (match directly above me) or "Above".</summary>
        public string relation;
    }

    [Serializable]
    public class AuraData
    {
        public string name;
        public string addTag;
        public string removeTag;
        /// <summary>Direction flags by name: All, Sides, Vertical, Left, Right, Up, Down (comma-separated).</summary>
        public string directions = "All";
        public int range = 1;

        public bool IsEmpty => string.IsNullOrEmpty(addTag) && string.IsNullOrEmpty(removeTag);
    }

    [Serializable]
    public class GuestData
    {
        public string id;
        public string name;
        /// <summary>GhostType name, e.g. "Drowned".</summary>
        public string type;
        public string era;
        public string bio;
        public int stays = 3;
        public RuleData[] needs = new RuleData[0];
        public RuleData[] likes = new RuleData[0];
        public RuleData[] dislikes = new RuleData[0];
        public AuraData aura;

        /// <summary>One unprinted rule (Act 2+, GDD §4.6), scored at dawn like a dislike.</summary>
        public RuleData hiddenRule;
        /// <summary>What the card shows instead of the hidden rule, e.g. "I can't bear to see my own face...".</summary>
        public string hiddenHint;
        /// <summary>Three clues shown instead of all rules when this guest is a Riddle night's mystery guest.</summary>
        public string[] riddleClues = new string[0];

        /// <summary>Art key when it differs from the ghost type, e.g. "Bartholomew" → ghost_Bartholomew_*.png.</summary>
        public string art;

        // Story (GDD §2 guest template). Dialogue script format: "Name[expression]: text", one line per row.
        /// <summary>First check-in: who they are plus a hint at their rules.</summary>
        public string arrival;
        /// <summary>One beat per stay, shown at the desk when they return (index = stays completed).</summary>
        public string[] story = new string[0];
        /// <summary>Said when they move on.</summary>
        public string checkout;
        /// <summary>Said when they come back after moving on (GDD Act 3: "guests you freed return to help").</summary>
        public string returnLine;
        public string keepsake;
        /// <summary>KeepsakePassive name, e.g. "ExtraSwap". Empty = memento only.</summary>
        public string keepsakePassive;
    }

    /// <summary>One tutorial spotlight (GDD §5.2: one thing per night, no text walls).</summary>
    [Serializable]
    public class TutorialStep
    {
        /// <summary>"queue", "card", "open", "undo", "rating" or "room:203".</summary>
        public string target;
        public string text;
    }

    /// <summary>Base tags for one room, by room number ("203" = floor 2, room 3).</summary>
    [Serializable]
    public class RoomData
    {
        public string room;
        public string[] tags = new string[0];
    }

    [Serializable]
    public class PlacementData
    {
        public string guest;
        public string room;
    }

    [Serializable]
    public class NightData
    {
        public int number;
        public string title;
        public int floors = 1;
        public int roomsPerFloor = 3;
        public RoomData[] rooms = new RoomData[0];
        /// <summary>Guest ids in arrival order.</summary>
        public string[] guests = new string[0];
        /// <summary>Guests already in place and locked (a difficulty lever, GDD §6).</summary>
        public PlacementData[] prePlaced = new PlacementData[0];
        public int swapTokens;
        /// <summary>Legacy event ids (unused; see <see cref="midnight"/>).</summary>
        public string[] events = new string[0];
        /// <summary>Midnight events that fire when the doors open (GDD §4.7). Empty = no Midnight phase.</summary>
        public MidnightEventData[] midnight = new MidnightEventData[0];
        /// <summary>Bartholomew's lines at the desk before guests arrive (dialogue script format).</summary>
        public string intro;
        /// <summary>Spotlight steps shown when check-in starts.</summary>
        public TutorialStep[] tutorial = new TutorialStep[0];
        /// <summary>Story nights may ignore the player's renovations (GDD §4.8).</summary>
        public bool lockRenovations;
        /// <summary>
        /// Mr. Vane's offer, shown at the desk before guests arrive (dialogue script whose last line has
        /// "> accept | refuse" replies). Accepting pays Ectoplasm but costs Calm and counts toward the ending.
        /// </summary>
        public string vaneOffer;
        public int vaneOfferEctoplasm = 150;
        /// <summary>Riddle night (GDD §4.9): this guest's rules are hidden behind their riddleClues.</summary>
        public string riddleGuest;
        /// <summary>One of Edith's torn ledger pages, found the first time this night is cleared (GDD §2).</summary>
        public string ledgerPage;
    }

    /// <summary>
    /// One midnight event. kind: RestlessNight, PowerFlicker (floor), BurstPipe (room),
    /// UnexpectedGuest (guestId), VanesVisit (room), SeanceDownstairs.
    /// </summary>
    [Serializable]
    public class MidnightEventData
    {
        public string kind;
        public string title;
        public string description;
        /// <summary>Room number for BurstPipe / VanesVisit.</summary>
        public string room;
        /// <summary>1-based floor for PowerFlicker.</summary>
        public int floor;
        /// <summary>Walk-in guest for UnexpectedGuest.</summary>
        public string guestId;
    }

    /// <summary>A renovation the player placed: adds one tag to one room (GDD §4.8).</summary>
    [Serializable]
    public class RenovationData
    {
        public string id;
        public string displayName;
        public string tag;
        public int cost;
    }

    /// <summary>Top-level shape of guests.json.</summary>
    [Serializable]
    public class GuestFile
    {
        public GuestData[] guests = new GuestData[0];
    }

    /// <summary>Top-level shape of nights.json.</summary>
    [Serializable]
    public class NightFile
    {
        public NightData[] nights = new NightData[0];
    }
}
