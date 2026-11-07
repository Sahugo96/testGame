using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Client.UI
{
    /// <summary>Drags a window by its header (02, 7.13: windows can be moved). Clicking also brings the window to front.</summary>
    public sealed class WindowDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
    {
        private RectTransform _window;
        private Canvas _canvas;

        public Action Focused;

        public void Init(RectTransform window, Canvas canvas)
        {
            _window = window;
            _canvas = canvas;
        }

        public void OnPointerDown(PointerEventData eventData) => Focused?.Invoke();

        public void OnBeginDrag(PointerEventData eventData) => Focused?.Invoke();

        public void OnDrag(PointerEventData eventData)
        {
            float scale = _canvas != null ? _canvas.scaleFactor : 1f;
            _window.anchoredPosition += eventData.delta / scale;
        }
    }
}
