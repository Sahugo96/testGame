using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Client.UI
{
    /// <summary>
    /// Drag and drop of a UI element (bag stacks): Began/Ended on the dragged element, Dropped on the element it was
    /// released over. The owner keeps what is being dragged; this component only reports the events.
    /// </summary>
    public sealed class DragTrigger : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public Action Began;
        public Action Ended;
        public Action Dropped;

        public void OnBeginDrag(PointerEventData eventData) => Began?.Invoke();

        /// <summary>Needed so uGUI starts a drag on this element; the ghost follows the pointer through the tooltip.</summary>
        public void OnDrag(PointerEventData eventData)
        {
        }

        public void OnEndDrag(PointerEventData eventData) => Ended?.Invoke();

        public void OnDrop(PointerEventData eventData) => Dropped?.Invoke();
    }
}
