using Game.Shared.Core;
using UnityEngine;
using EntityId = Game.Shared.Core.EntityId;

namespace Game.Client.World
{
    /// <summary>
    /// Visual of one server entity. Interpolates between snapshots (simulation and presentation are separate, principle 9).
    /// The object itself sits at the middle of the figure and only turns around the vertical axis; the figure hangs
    /// under it, walks (limbs swing with the distance covered) and falls on its back when the entity dies.
    /// </summary>
    public sealed class EntityView : MonoBehaviour
    {
        private const float FallDegrees = -90f;
        private const float FallSeconds = 0.35f;

        private Vector3 _from;
        private Vector3 _to;
        private Quaternion _fromRotation;
        private Quaternion _toRotation;
        private float _t = 1f;
        private float _interval;
        private Figure _figure;
        private FigureMotion _motion;
        private Renderer[] _renderers;
        private Material[] _aliveMaterials;
        private Material _deadMaterial;
        private bool _isDead;
        private float _fall;
        private float _heightOffset;
        private Quaternion _modelFacing = Quaternion.identity;

        public EntityId Id { get; private set; }
        public Collider Collider { get; private set; }

        /// <summary>Width of the figure on the ground (for the target marker).</summary>
        public float Footprint => Mathf.Max(_figure.Size.x, _figure.Size.z);

        /// <summary>Height of the ground under the figure, relative to this object.</summary>
        public float GroundOffset => -_heightOffset;

        /// <summary>`modelYaw` turns the figure itself (degrees): for things whose direction the server does not send.</summary>
        public void Init(EntityId id, float interval, Figure figure, Color deadColor, float modelYaw)
        {
            _modelFacing = Quaternion.Euler(0f, modelYaw, 0f);
            Id = id;
            _interval = interval;
            _figure = figure;
            _heightOffset = figure.Size.y * 0.5f;
            figure.Model.SetParent(transform, false);
            figure.Model.localPosition = new Vector3(0f, -_heightOffset, 0f);
            figure.Model.localRotation = _modelFacing;
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.size = figure.Size;
            Collider = box;
            _renderers = figure.Model.GetComponentsInChildren<Renderer>();
            _aliveMaterials = new Material[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _aliveMaterials[i] = _renderers[i].sharedMaterial;
            }
            _deadMaterial = WorldPrimitives.Colored(deadColor);
            _motion = new FigureMotion(figure);
        }

        public void SetInterval(float interval) => _interval = interval;

        public void Snap(Vec3 position, float yaw)
        {
            _to = ToWorld(position);
            _from = _to;
            _toRotation = Quaternion.Euler(0f, yaw, 0f);
            _fromRotation = _toRotation;
            _t = 1f;
            transform.SetPositionAndRotation(_to, _toRotation);
        }

        public void SetTarget(Vec3 position, float yaw, bool isDead)
        {
            _from = transform.position;
            _fromRotation = transform.rotation;
            _to = ToWorld(position);
            _toRotation = Quaternion.Euler(0f, yaw, 0f);
            _t = 0f;
            if (isDead != _isDead)
            {
                _isDead = isDead;
                for (int i = 0; i < _renderers.Length; i++)
                {
                    _renderers[i].sharedMaterial = isDead ? _deadMaterial : _aliveMaterials[i];
                }
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            float moved = 0f;
            if (_t < 1f)
            {
                Vector3 before = transform.position;
                _t = _interval > 0f ? Mathf.Min(1f, _t + deltaTime / _interval) : 1f;
                Vector3 now = Vector3.Lerp(_from, _to, _t);
                transform.SetPositionAndRotation(now, Quaternion.Slerp(_fromRotation, _toRotation, _t));
                float dx = now.x - before.x;
                float dz = now.z - before.z;
                moved = Mathf.Sqrt(dx * dx + dz * dz);
            }
            float fallTarget = _isDead && _figure.FallsWhenDead ? 1f : 0f;
            if (_fall != fallTarget)
            {
                _fall = Mathf.MoveTowards(_fall, fallTarget, deltaTime / FallSeconds);
                _figure.Model.localRotation = _modelFacing * Quaternion.Euler(FallDegrees * _fall, 0f, 0f);
                _figure.Model.localPosition = new Vector3(0f, -_heightOffset + _figure.LieHeight * _fall, 0f);
            }
            if (!_isDead)
            {
                _motion.Tick(moved, deltaTime);
            }
        }

        private Vector3 ToWorld(Vec3 position) => new Vector3(position.X, position.Y + _heightOffset, position.Z);
    }
}
