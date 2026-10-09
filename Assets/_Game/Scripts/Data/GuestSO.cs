using GhostHotel.Model.Content;
using UnityEngine;

namespace GhostHotel.Data
{
    /// <summary>One guest. Rules and story live in <see cref="data"/> (imported from guests.json); art is assigned here.</summary>
    [CreateAssetMenu(menuName = "Ghost Hotel/Guest", fileName = "Guest")]
    public sealed class GuestSO : ScriptableObject
    {
        public GuestData data = new GuestData();
        [Tooltip("Portraits by expression: 0 neutral, 1 happy, 2 sad (GDD §7.3).")]
        public Sprite[] portraits = new Sprite[0];
        public Sprite inRoomSprite;
        public KeepsakeSO keepsake;

        public string Id => data.id;
    }
}
