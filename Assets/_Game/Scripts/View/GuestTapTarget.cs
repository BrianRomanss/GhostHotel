using System;
using UnityEngine;

namespace GhostHotel.View
{
    /// <summary>Click handler for non-draggable guests (pre-booked) so their card can still be read.</summary>
    public sealed class GuestTapTarget : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
    {
        Action _onTap;
        public void Init(Action onTap) => _onTap = onTap;
        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData) => _onTap?.Invoke();
    }
}
