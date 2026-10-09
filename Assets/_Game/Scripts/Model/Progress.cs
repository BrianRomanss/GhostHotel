using System;
using System.Collections.Generic;

namespace GhostHotel.Model
{
    /// <summary>Story progress for one guest.</summary>
    [Serializable]
    public class GuestProgress
    {
        public string id;
        public int staysDone;
        public bool movedOn;
    }

    /// <summary>Best result for one night, for Night Select.</summary>
    [Serializable]
    public class NightRecord
    {
        public int number;
        public int bestMoons;
        public bool perfect;
    }

    [Serializable]
    public class PlacedRenovation
    {
        public string id;
        public string tag;
        /// <summary>Room number, e.g. "102".</summary>
        public string room;
    }

    /// <summary>Everything a save slot holds. Plain fields so JsonUtility can write it.</summary>
    [Serializable]
    public class GameProgress
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        /// <summary>Next story night to play (1-based). Past the last night = story finished.</summary>
        public int night = 1;
        public int ectoplasm;
        public int memories;
        /// <summary>0–100 hotel health (GDD §4.8).</summary>
        public int calm = Economy.StartingCalm;
        public List<GuestProgress> guests = new List<GuestProgress>();
        public List<NightRecord> nights = new List<NightRecord>();
        /// <summary>Renovations placed in the hotel, applied to every night that doesn't lock them.</summary>
        public List<PlacedRenovation> renovations = new List<PlacedRenovation>();
        /// <summary>Staff ever hired, and those working tonight (limited by Perks.StaffSlots).</summary>
        public List<string> hiredStaff = new List<string>();
        public List<string> activeStaff = new List<string>();
        /// <summary>Guest ids whose keepsakes are equipped (max 3).</summary>
        public List<string> equippedKeepsakes = new List<string>();
        /// <summary>How many of Mr. Vane's offers the player accepted (affects the ending).</summary>
        public int vaneDeals;
        /// <summary>Guests whose hidden rule the player has discovered (GDD §4.6).</summary>
        public List<string> revealedHidden = new List<string>();
        /// <summary>Night numbers whose ledger page has been found (GDD: Edith's Pages).</summary>
        public List<int> ledgerPages = new List<int>();
        public double playSeconds;
        public string savedAtUtc;

        public GuestProgress Guest(string id)
        {
            foreach (var g in guests) if (g.id == id) return g;
            var created = new GuestProgress { id = id };
            guests.Add(created);
            return created;
        }

        public NightRecord Record(int number)
        {
            foreach (var n in nights) if (n.number == number) return n;
            var created = new NightRecord { number = number };
            nights.Add(created);
            return created;
        }

        public int GuestsMovedOn
        {
            get
            {
                int n = 0;
                foreach (var g in guests) if (g.movedOn) n++;
                return n;
            }
        }
    }
}
