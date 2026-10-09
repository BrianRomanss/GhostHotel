using System.Collections.Generic;
using GhostHotel.Model;
using GhostHotel.Model.Content;
using UnityEngine;

namespace GhostHotel.Data
{
    /// <summary>The ordered list of story nights and every guest. The game's entry point to content.</summary>
    [CreateAssetMenu(menuName = "Ghost Hotel/Night Catalog", fileName = "NightCatalog")]
    public sealed class NightCatalogSO : ScriptableObject
    {
        public NightSO[] nights = new NightSO[0];
        public GuestSO[] guests = new GuestSO[0];

        public int Count => nights.Length;

        /// <summary>Night by 1-based number, or null.</summary>
        public NightSO Night(int number) => number >= 1 && number <= nights.Length ? nights[number - 1] : null;

        public ContentFactory CreateFactory()
        {
            var data = new List<GuestData>();
            foreach (var g in guests) if (g != null) data.Add(g.data);
            return new ContentFactory(data);
        }

        /// <summary>Builds a fresh, playable hotel for the night. Errors are logged.</summary>
        public HotelModel BuildNight(int number)
        {
            var night = Night(number);
            if (night == null) return null;
            var factory = CreateFactory();
            var hotel = factory.Night(night.data);
            foreach (var e in factory.Errors) Debug.LogError($"[Content] {e}", night);
            return factory.Errors.Count == 0 ? hotel : null;
        }

        public GuestSO Guest(string id)
        {
            foreach (var g in guests) if (g != null && g.Id == id) return g;
            return null;
        }
    }
}
