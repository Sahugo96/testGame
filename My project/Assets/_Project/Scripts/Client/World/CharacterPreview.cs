using System.Collections.Generic;
using UnityEngine;

namespace Game.Client.World
{
    /// <summary>
    /// Character portrait for the character window: a small stage far below the world on its own layer, filmed by a
    /// camera into a texture. The camera renders only while the window is open.
    /// </summary>
    public sealed class CharacterPreview : MonoBehaviour
    {
        private static readonly Vector3 StagePosition = new Vector3(0f, -500f, 0f);
        private static readonly Vector3 CameraOffset = new Vector3(0f, 1.1f, 3.6f);
        private static readonly Color Background = new Color(0.12f, 0.11f, 0.1f, 1f);
        private const float ModelCenterHeight = 1f;
        private const float CameraFieldOfView = 40f;
        private const float RotationDegreesPerSecond = 25f;
        private const int TextureSize = 512;
        private const int DepthBits = 16;

        private readonly List<GameObject> _attachments = new List<GameObject>();
        private Camera _camera;
        private Transform _model;
        private Renderer _body;

        public RenderTexture Texture { get; private set; }

        public static CharacterPreview Create()
        {
            var go = new GameObject("CharacterPreview");
            CharacterPreview preview = go.AddComponent<CharacterPreview>();
            preview.Build();
            return preview;
        }

        public void SetVisible(bool visible) => _camera.enabled = visible;

        public void SetBodyColor(Color color) => _body.material.color = color;

        /// <summary>Rebuilds placeholder parts: one entry per equipped item (slot type and color).</summary>
        public void ShowEquipment(IEnumerable<KeyValuePair<string, Color>> equipped)
        {
            foreach (GameObject attachment in _attachments)
            {
                Destroy(attachment);
            }
            _attachments.Clear();
            var ringsShown = 0;
            foreach (KeyValuePair<string, Color> item in equipped)
            {
                if (!PreviewAttachments.BySlotType.TryGetValue(item.Key, out PreviewAttachment part))
                {
                    continue;
                }
                Vector3 position = part.Position;
                if (item.Key == "Ring" && ringsShown++ % 2 == 1)
                {
                    position.x = -position.x;
                }
                Spawn(part.Shape, position, part.Scale, item.Value);
                if (part.Mirrored)
                {
                    Spawn(part.Shape, new Vector3(-position.x, position.y, position.z), part.Scale, item.Value);
                }
            }
        }

        private void Build()
        {
            transform.position = StagePosition;
            GameObject model = new GameObject("Model");
            _model = model.transform;
            _model.SetParent(transform, false);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Prepare(body, _model);
            body.transform.localPosition = new Vector3(0f, ModelCenterHeight, 0f);
            _body = body.GetComponent<Renderer>();

            Texture = new RenderTexture(TextureSize, TextureSize, DepthBits);
            var cameraObject = new GameObject("PreviewCamera");
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.localPosition = CameraOffset;
            cameraObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.cullingMask = RenderLayers.PreviewMask;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Background;
            _camera.fieldOfView = CameraFieldOfView;
            _camera.targetTexture = Texture;
            _camera.enabled = false;
        }

        private void Spawn(PrimitiveType shape, Vector3 position, Vector3 scale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(shape);
            Prepare(part, _model);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().material.color = color;
            _attachments.Add(part);
        }

        private static void Prepare(GameObject go, Transform parent)
        {
            go.layer = RenderLayers.Preview;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
        }

        private void Update()
        {
            if (_camera.enabled)
            {
                _model.Rotate(0f, RotationDegreesPerSecond * Time.deltaTime, 0f);
            }
        }

        private void OnDestroy()
        {
            if (Texture != null)
            {
                Texture.Release();
                Destroy(Texture);
            }
        }
    }
}
