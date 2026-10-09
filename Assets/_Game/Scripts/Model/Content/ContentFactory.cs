using System;
using System.Collections.Generic;
using ChuchuGames.GridPuzzle;
using GhostHotel.Model.Rules;

namespace GhostHotel.Model.Content
{
    /// <summary>
    /// Turns authoring data into runtime objects, collecting every problem it finds instead of
    /// stopping at the first, so an import or validation run reports everything at once.
    /// </summary>
    public sealed class ContentFactory
    {
        readonly Dictionary<string, GuestData> _guests = new Dictionary<string, GuestData>();
        readonly Dictionary<string, GuestDef> _built = new Dictionary<string, GuestDef>();

        public List<string> Errors { get; } = new List<string>();

        public ContentFactory(IEnumerable<GuestData> guests)
        {
            foreach (var g in guests)
            {
                if (g == null) continue;
                if (string.IsNullOrEmpty(g.id)) Errors.Add($"Guest '{g.name}' has no id");
                else if (_guests.ContainsKey(g.id)) Errors.Add($"Duplicate guest id '{g.id}'");
                else _guests[g.id] = g;
            }
        }

        public IEnumerable<string> GuestIds => _guests.Keys;

        public GuestDef Guest(string id)
        {
            if (id != null && _built.TryGetValue(id, out var def)) return def;
            if (id == null || !_guests.TryGetValue(id, out var data))
            {
                Errors.Add($"Unknown guest '{id}'");
                return null;
            }

            string where = $"Guest '{id}'";
            if (!TryEnum(data.type, out GhostType type, where + " type")) type = GhostType.Wisp;
            def = new GuestDef(data.id, string.IsNullOrEmpty(data.name) ? data.id : data.name, type,
                Rules(data.needs, where + " needs"), Rules(data.likes, where + " likes"), Rules(data.dislikes, where + " dislikes"),
                Aura(data.aura, where), data.stays <= 0 ? 3 : data.stays);
            if (!string.IsNullOrEmpty(data.art)) def.ArtKey = data.art;
            if (data.hiddenRule != null && !string.IsNullOrEmpty(data.hiddenRule.kind))
            {
                def.Hidden = Rule(data.hiddenRule, where + " hiddenRule");
                def.HiddenHint = string.IsNullOrEmpty(data.hiddenHint) ? "Something they haven't told you..." : data.hiddenHint;
            }
            def.RiddleClues = data.riddleClues ?? new string[0];
            _built[id] = def;
            return def;
        }

        /// <summary>Builds the night's hotel with its guests queued (and pre-placed ones locked in).</summary>
        /// <param name="renovations">Player renovations; ignored if the night locks them or the room doesn't exist yet.</param>
        public HotelModel Night(NightData night, IEnumerable<PlacedRenovation> renovations = null)
        {
            string where = $"Night {night.number}";
            if (night.floors < 1 || night.floors > 9 || night.roomsPerFloor < 1 || night.roomsPerFloor > 9)
            {
                Errors.Add($"{where}: hotel must be 1–9 floors × 1–9 rooms (got {night.floors}×{night.roomsPerFloor})");
                return null;
            }

            var hotel = new HotelModel(night.floors, night.roomsPerFloor);
            foreach (var room in night.rooms ?? new RoomData[0])
            {
                if (!TryRoom(hotel, room.room, out var cell, where)) continue;
                foreach (var tag in room.tags ?? new string[0])
                    if (CheckTag(tag, $"{where} room {room.room}")) hotel.Room(cell).Tags.AddBase(tag);
            }

            if (renovations != null && !night.lockRenovations)
                foreach (var r in renovations)
                    if (int.TryParse(r.room, out var n) && hotel.Rooms.InBounds(new Cell(n / 100 - 1, n % 100 - 1)) && Array.IndexOf(Tags.All, r.tag) >= 0)
                        hotel.Room(new Cell(n / 100 - 1, n % 100 - 1)).Tags.AddBase(r.tag);

            var byId = new Dictionary<string, GuestState>();
            foreach (var id in night.guests ?? new string[0])
            {
                if (byId.ContainsKey(id)) { Errors.Add($"{where}: guest '{id}' listed twice"); continue; }
                var def = Guest(id);
                if (def != null) byId[id] = hotel.AddGuest(def);
            }

            if (!string.IsNullOrEmpty(night.riddleGuest))
            {
                if (!byId.TryGetValue(night.riddleGuest, out var riddle)) Errors.Add($"{where}: riddle guest '{night.riddleGuest}' is not booked");
                else if (riddle.Def.RiddleClues.Count < 3) Errors.Add($"{where}: riddle guest '{night.riddleGuest}' needs 3 riddleClues");
                else riddle.IsRiddle = true;
            }

            if (byId.Count > hotel.Rooms.Count)
                Errors.Add($"{where}: {byId.Count} guests but only {hotel.Rooms.Count} rooms");

            foreach (var p in night.prePlaced ?? new PlacementData[0])
            {
                if (!byId.TryGetValue(p.guest ?? "", out var g)) { Errors.Add($"{where}: pre-placed guest '{p.guest}' is not in the guest list"); continue; }
                if (!TryRoom(hotel, p.room, out var cell, where)) continue;
                if (hotel.GuestAt(cell) != null) { Errors.Add($"{where}: room {p.room} pre-placed twice"); continue; }
                hotel.Place(g, cell);
                g.Locked = true;
            }

            return hotel;
        }

        /// <summary>Builds the night's midnight events, validating rooms, floors and guests against its hotel size.</summary>
        public List<MidnightEvent> Midnight(NightData night)
        {
            var list = new List<MidnightEvent>();
            var probe = new HotelModel(Math.Max(1, night.floors), Math.Max(1, night.roomsPerFloor));
            for (int i = 0; i < (night.midnight?.Length ?? 0); i++)
            {
                var e = night.midnight[i];
                string where = $"Night {night.number} midnight[{i}]";
                if (e == null || !TryEnum(e.kind, out MidnightKind kind, where + " kind")) continue;
                string title = string.IsNullOrEmpty(e.title) ? DefaultTitle(kind) : e.title;
                Cell? room = null;
                GuestDef guest = null;
                int floorRow = 0;
                switch (kind)
                {
                    case MidnightKind.BurstPipe:
                    case MidnightKind.VanesVisit:
                        if (!TryRoom(probe, e.room, out var cell, where)) continue;
                        room = cell;
                        break;
                    case MidnightKind.PowerFlicker:
                        if (e.floor < 1 || e.floor > night.floors) { Errors.Add($"{where}: floor {e.floor} doesn't exist"); continue; }
                        floorRow = e.floor - 1;
                        break;
                    case MidnightKind.UnexpectedGuest:
                        if (Array.IndexOf(night.guests ?? new string[0], e.guestId) >= 0) { Errors.Add($"{where}: walk-in '{e.guestId}' is already booked tonight"); continue; }
                        guest = Guest(e.guestId);
                        if (guest == null) continue;
                        break;
                }
                list.Add(new MidnightEvent(kind, title, string.IsNullOrEmpty(e.description) ? DefaultDescription(kind) : e.description, room, floorRow, guest));
            }
            return list;
        }

        static string DefaultTitle(MidnightKind k)
        {
            switch (k)
            {
                case MidnightKind.RestlessNight: return "Restless Night";
                case MidnightKind.PowerFlicker: return "Power Flicker";
                case MidnightKind.BurstPipe: return "Burst Pipe";
                case MidnightKind.UnexpectedGuest: return "Unexpected Guest";
                case MidnightKind.VanesVisit: return "Mr. Vane's Visit";
                default: return "Séance Downstairs";
            }
        }

        static string DefaultDescription(MidnightKind k)
        {
            switch (k)
            {
                case MidnightKind.RestlessNight: return "Poltergeists' noise now reaches 2 rooms in each direction.";
                case MidnightKind.PowerFlicker: return "The lights die on one floor. Its rooms are no longer Dark.";
                case MidnightKind.BurstPipe: return "A pipe bursts: one room floods with Water and its neighbours turn Cold.";
                case MidnightKind.UnexpectedGuest: return "Someone is knocking. Find them an empty room, or turn them away.";
                case MidnightKind.VanesVisit: return "Mr. Vane left something behind. One room is Cursed.";
                default: return "Candles in the cellar. Basement guests now want company.";
            }
        }

        IReadOnlyList<IRule> Rules(RuleData[] data, string where)
        {
            var list = new List<IRule>();
            if (data == null) return list;
            for (int i = 0; i < data.Length; i++)
            {
                var r = Rule(data[i], $"{where}[{i}]");
                if (r != null) list.Add(r);
            }
            return list;
        }

        public IRule Rule(RuleData d, string where)
        {
            if (d == null) { Errors.Add($"{where}: missing rule"); return null; }
            switch (d.kind)
            {
                case "RequiresTag": return CheckTag(d.tag, where) ? new RequiresTagRule(d.tag) : null;
                case "ForbidsTag": return CheckTag(d.tag, where) ? new ForbidsTagRule(d.tag) : null;
                case "Floor":
                    if (!TryEnum(d.end, out FloorEnd end, where + " end")) return null;
                    if (d.count < 1) { Errors.Add($"{where}: Floor rule needs count ≥ 1"); return null; }
                    return new FloorRule(end, d.count);
                case "AvoidNeighbour":
                    return TryEnum(d.ghostType, out GhostType avoid, where + " ghostType") ? new AvoidNeighbourTypeRule(avoid) : null;
                case "WantNeighbour":
                    return Matcher(d, where) is GuestMatcher want ? new NeighbourWantRule(want) : null;
                case "Isolation": return new IsolationRule();
                case "Company": return new CompanyRule();
                case "Vertical":
                    if (!TryEnum(d.relation, out VerticalRelation rel, where + " relation")) return null;
                    return Matcher(d, where) is GuestMatcher m ? new VerticalRule(rel, m) : null;
                case "Edge": return new EdgeRule();
                case "FloorCount":
                    if (d.count < 1) { Errors.Add($"{where}: FloorCount rule needs count ≥ 1"); return null; }
                    return new FloorCountRule(d.count);
                default:
                    Errors.Add($"{where}: unknown rule kind '{d.kind}'");
                    return null;
            }
        }

        GuestMatcher Matcher(RuleData d, string where)
        {
            if (!string.IsNullOrEmpty(d.guestId))
            {
                if (!_guests.TryGetValue(d.guestId, out var target)) { Errors.Add($"{where}: unknown guestId '{d.guestId}'"); return null; }
                return GuestMatcher.Guest(d.guestId, string.IsNullOrEmpty(target.name) ? target.id : target.name);
            }
            return TryEnum(d.ghostType, out GhostType t, where + " ghostType") ? GuestMatcher.OfType(t) : null;
        }

        AuraDef Aura(AuraData a, string where)
        {
            if (a == null || a.IsEmpty) return null;
            if (!string.IsNullOrEmpty(a.addTag) && !CheckTag(a.addTag, where + " aura")) return null;
            if (!string.IsNullOrEmpty(a.removeTag) && !CheckTag(a.removeTag, where + " aura")) return null;
            if (!TryEnum(string.IsNullOrEmpty(a.directions) ? "All" : a.directions, out Direction dirs, where + " aura directions")) return null;
            return new AuraDef(a.name, a.addTag, a.removeTag, dirs, a.range);
        }

        bool CheckTag(string tag, string where)
        {
            if (Array.IndexOf(Tags.All, tag) >= 0) return true;
            Errors.Add($"{where}: unknown tag '{tag}' (known: {string.Join(", ", Tags.All)})");
            return false;
        }

        bool TryEnum<T>(string value, out T result, string where) where T : struct
        {
            if (!string.IsNullOrEmpty(value) && Enum.TryParse(value, ignoreCase: false, out result)) return true;
            Errors.Add($"{where}: '{value}' is not one of {string.Join(", ", Enum.GetNames(typeof(T)))}");
            result = default;
            return false;
        }

        /// <summary>Parses a room number like "203" (floor 2, room 3) into a cell.</summary>
        public bool TryRoom(HotelModel hotel, string number, out Cell cell, string where)
        {
            cell = default;
            if (int.TryParse(number, out var n) && n >= 101)
            {
                var c = new Cell(n / 100 - 1, n % 100 - 1);
                if (hotel.Rooms.InBounds(c)) { cell = c; return true; }
            }
            Errors.Add($"{where}: room '{number}' doesn't exist in a {hotel.Floors}×{hotel.RoomsPerFloor} hotel");
            return false;
        }
    }
}
