using GhostHotel.Model.Content;
using UnityEngine;

namespace GhostHotel.Data
{
    /// <summary>One night (level). Layout and guest ids live in <see cref="data"/>; <see cref="guests"/> are resolved references.</summary>
    [CreateAssetMenu(menuName = "Ghost Hotel/Night", fileName = "Night")]
    public sealed class NightSO : ScriptableObject
    {
        public NightData data = new NightData();
        public GuestSO[] guests = new GuestSO[0];
        public MidnightEventSO[] events = new MidnightEventSO[0];
    }
}
