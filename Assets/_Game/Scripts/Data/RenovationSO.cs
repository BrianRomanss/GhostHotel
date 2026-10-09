using UnityEngine;

namespace GhostHotel.Data
{
    /// <summary>Renovation that adds one tag to a room slot (GDD §4.8).</summary>
    [CreateAssetMenu(menuName = "Ghost Hotel/Renovation", fileName = "Renovation")]
    public sealed class RenovationSO : ScriptableObject
    {
        public string id;
        public string displayName;
        public string addsTag;
        public int cost = 50;
        public Sprite icon;
    }
}
