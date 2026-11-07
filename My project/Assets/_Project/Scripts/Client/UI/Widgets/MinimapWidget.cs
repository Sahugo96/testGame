using Game.Client.Localization;
using Game.Client.World;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Widgets
{
    /// <summary>
    /// Top-right minimap (02, 7.13): an orthographic top-down camera rendering into a texture. Placeholder until the
    /// minimap module (01 §13): it should only show what the client already knows via interest management.
    /// </summary>
    public sealed class MinimapWidget
    {
        private const int DepthBufferBits = 16;

        private readonly Camera _camera;
        private Transform _follow;

        public MinimapWidget(RectTransform zone, ClientContext ctx)
        {
            Text title = UiFactory.Label("ZoneName", zone, ctx.Font, HudLayout.FontNormal, TextAnchor.MiddleRight, HudPalette.Text);
            UiFactory.Place(title.rectTransform, Vector2.one, Vector2.one, Vector2.zero, new Vector2(HudLayout.MinimapSize, HudLayout.BarHeight));
            title.text = ctx.Text.Get(UiKeys.ZoneName);

            Image frame = UiFactory.Panel("MinimapFrame", zone, HudPalette.Minimap, false);
            UiFactory.Place(frame.rectTransform, Vector2.one, Vector2.one, new Vector2(0f, -HudLayout.BarHeight - HudLayout.Gap),
                new Vector2(HudLayout.MinimapSize, HudLayout.MinimapSize));

            var texture = new RenderTexture(HudLayout.MinimapTextureSize, HudLayout.MinimapTextureSize, DepthBufferBits);
            var go = new GameObject("MinimapCamera");
            _camera = go.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = HudLayout.MinimapWorldSize * 0.5f;
            _camera.cullingMask = RenderLayers.AllExceptPreview;
            _camera.targetTexture = texture;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = HudPalette.Minimap;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            RectTransform imageRect = UiFactory.Rect("MinimapImage", frame.transform);
            UiFactory.Stretch(imageRect, HudLayout.Gap);
            RawImage image = imageRect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
        }

        public void Follow(Transform target) => _follow = target;

        public void Tick()
        {
            if (_follow == null)
            {
                return;
            }
            Vector3 position = _follow.position;
            _camera.transform.position = new Vector3(position.x, position.y + HudLayout.MinimapCameraHeight, position.z);
        }

        public void Destroy()
        {
            if (_camera != null)
            {
                Object.Destroy(_camera.targetTexture);
                Object.Destroy(_camera.gameObject);
            }
        }
    }
}
