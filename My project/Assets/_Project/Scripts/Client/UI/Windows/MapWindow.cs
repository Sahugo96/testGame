using System;
using Game.Client.Localization;
using Game.Client.World;
using Game.Shared.Content.Dto;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Windows
{
    /// <summary>
    /// Zone map: a top-down camera over the whole zone, rendering only while the window is open. Placeholder for the
    /// minimap/map module (01 §13: should use only data the client already knows).
    /// </summary>
    public sealed class MapWindow : HudWindow
    {
        private static readonly Vector2 Size = new Vector2(600f, 660f);
        private const float CameraHeight = 100f;
        private const int TextureSize = 512;
        private const int DepthBits = 16;
        private const float HintHeight = 24f;

        private readonly Camera _camera;
        private readonly RenderTexture _texture;

        public MapWindow(HudWindowDto definition, RectTransform parent, Canvas canvas, ClientContext ctx, Tooltip tooltip,
            Action<HudWindow> focus, Action<HudWindow> close) : base(definition, parent, canvas, ctx, tooltip, focus, close, Size)
        {
            _texture = new RenderTexture(TextureSize, TextureSize, DepthBits);
            var go = new GameObject("MapCamera");
            go.transform.position = new Vector3(0f, CameraHeight, 0f);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _camera = go.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = ctx.Content.Config.WorldHalfSize;
            _camera.cullingMask = RenderLayers.AllExceptPreview;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = HudPalette.Minimap;
            _camera.targetTexture = _texture;
            _camera.enabled = false;

            RectTransform imageRect = UiFactory.Rect("MapImage", Content);
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = new Vector2(0f, HintHeight + HudLayout.Gap);
            imageRect.offsetMax = Vector2.zero;
            RawImage image = imageRect.gameObject.AddComponent<RawImage>();
            image.texture = _texture;
            image.raycastTarget = false;

            Text hint = UiFactory.Label("Hint", Content, ctx.Font, HudLayout.FontSmall, TextAnchor.MiddleLeft, HudPalette.TextDim);
            UiFactory.Place(hint.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(Size.x, HintHeight));
            hint.text = ctx.Text.Get(UiKeys.MapHint);
        }

        public override void Refresh(PlayerStateSnapshot state)
        {
        }

        protected override void OnVisibilityChanged(bool open) => _camera.enabled = open;
    }
}
