using Game.Shared.Core;
using UnityEngine;

namespace Game.Client.World
{
    /// <summary>Visual of one server entity. Interpolates between snapshots (simulation and presentation are separate, principle 9).</summary>
    public sealed class EntityView : MonoBehaviour
    {
        private const float DeadTiltDegrees = 90f;

        private Vector3 _from;
        private Vector3 _to;
        private Quaternion _fromRotation;
        private Quaternion _toRotation;
        private float _t = 1f;
        private float _interval;
        private Renderer _renderer;
        private Color _baseColor;
        private Color _deadColor;
        private bool _isDead;
        private float _heightOffset;

        public EntityId Id { get; private set; }
        public Collider Collider { get; private set; }

        public void Init(EntityId id, float interval, Color color, Color deadColor, float heightOffset)
        {
            _heightOffset = heightOffset;
            Id = id;
            _interval = interval;
            _renderer = GetComponent<Renderer>();
            Collider = GetComponent<Collider>();
            _baseColor = color;
            _deadColor = deadColor;
            _renderer.material.color = color;
        }

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
            _toRotation = Quaternion.Euler(isDead ? DeadTiltDegrees : 0f, yaw, 0f);
            _t = 0f;
            if (isDead != _isDead)
            {
                _isDead = isDead;
                _renderer.material.color = isDead ? _deadColor : _baseColor;
            }
        }

        private void Update()
        {
            if (_t >= 1f)
            {
                return;
            }
            _t = _interval > 0f ? Mathf.Min(1f, _t + Time.deltaTime / _interval) : 1f;
            transform.SetPositionAndRotation(Vector3.Lerp(_from, _to, _t), Quaternion.Slerp(_fromRotation, _toRotation, _t));
        }

        private Vector3 ToWorld(Vec3 position) => new Vector3(position.X, position.Y + _heightOffset, position.Z);
    }
}
