using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Client.UI
{
    /// <summary>Reports pointer enter/exit and clicks of a UI element (used for tooltips and slot clicks).</summary>
    public sealed class HoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Action Entered;
        public Action Exited;
        public Action<PointerEventData.InputButton> Clicked;

        public void OnPointerEnter(PointerEventData eventData) => Entered?.Invoke();

        public void OnPointerExit(PointerEventData eventData) => Exited?.Invoke();

        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke(eventData.button);
    }
}
