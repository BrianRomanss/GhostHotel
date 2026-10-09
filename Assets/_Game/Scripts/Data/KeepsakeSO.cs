using UnityEngine;

namespace GhostHotel.Data
{
    /// <summary>Keepsake left by a guest who moves on (GDD §4.8). Passives are implemented in M5.</summary>
    [CreateAssetMenu(menuName = "Ghost Hotel/Keepsake", fileName = "Keepsake")]
    public sealed class KeepsakeSO : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string farewellLine;
        [TextArea] public string passive;
        public Sprite icon;
    }
}
