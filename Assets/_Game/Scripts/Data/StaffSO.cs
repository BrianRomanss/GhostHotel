using UnityEngine;

namespace GhostHotel.Data
{
    /// <summary>Hireable staff with a nightly ability (GDD §4.8). Abilities are implemented in M5.</summary>
    [CreateAssetMenu(menuName = "Ghost Hotel/Staff", fileName = "Staff")]
    public sealed class StaffSO : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string ability;
        public int cost = 100;
        public Sprite portrait;
    }
}
