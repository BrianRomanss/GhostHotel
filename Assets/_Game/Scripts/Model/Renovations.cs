using GhostHotel.Model.Content;

namespace GhostHotel.Model
{
    /// <summary>Renovation catalogue (GDD §4.8: each adds one tag). The vertical slice ships one.</summary>
    public static class Renovations
    {
        public static readonly RenovationData[] All =
        {
            new RenovationData { id = "blackout_curtains", displayName = "Blackout Curtains", tag = Tags.Dark, cost = 60 },
        };

        public static RenovationData Find(string id)
        {
            foreach (var r in All) if (r.id == id) return r;
            return null;
        }

        /// <summary>Buys and places a renovation. False if unaffordable or the room already has that tag from a renovation.</summary>
        public static bool Place(GameProgress p, RenovationData r, string room)
        {
            if (p.ectoplasm < r.cost) return false;
            foreach (var placed in p.renovations)
                if (placed.room == room && placed.tag == r.tag) return false;
            p.ectoplasm -= r.cost;
            p.renovations.Add(new PlacedRenovation { id = r.id, tag = r.tag, room = room });
            return true;
        }

        /// <summary>Removes a renovation and refunds it in full (GDD: "free undo until you leave").</summary>
        public static bool Remove(GameProgress p, PlacedRenovation placed)
        {
            if (!p.renovations.Remove(placed)) return false;
            var r = Find(placed.id);
            if (r != null) p.ectoplasm += r.cost;
            return true;
        }
    }
}
