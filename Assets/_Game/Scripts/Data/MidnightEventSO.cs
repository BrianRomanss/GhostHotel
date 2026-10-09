using UnityEngine;

namespace GhostHotel.Data
{
    /// <summary>Midnight event card (GDD §4.7). Effects are implemented in M5.</summary>
    [CreateAssetMenu(menuName = "Ghost Hotel/MidnightEvent", fileName = "MidnightEvent")]
    public sealed class MidnightEventSO : ScriptableObject
    {
        public string id;
        public string title;
        [TextArea] public string description;
        public Sprite art;
    }
}
