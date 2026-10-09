using UnityEngine;

namespace GhostHotel.Data
{
    /// <summary>Room tag (GDD §4.2). Id matches GhostHotel.Model.Tags.</summary>
    [CreateAssetMenu(menuName = "Ghost Hotel/Tag", fileName = "Tag")]
    public sealed class TagSO : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;
        [Tooltip("Shown only from auras/events, never as a base room tag.")] public bool isVirtual;
    }
}
