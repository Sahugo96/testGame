using UnityEngine;

namespace Game.Client.CameraRig
{
    /// <summary>
    /// Simple third-person orbit camera: right mouse button rotates, wheel zooms. Temporary until Cinemachine 3.x
    /// is set up in module 2 (01 §3).
    /// </summary>
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        private const float MinDistance = 4f;
        private const float MaxDistance = 30f;
        private const float StartDistance = 14f;
        private const float MinPitch = 10f;
        private const float MaxPitch = 80f;
        private const float StartPitch = 45f;
        private const float LookSensitivity = 0.2f;
        private const float ZoomStep = 1.5f;
        private const float FocusHeight = 1.2f;
        private const float FollowSharpness = 12f;

        private Transform _target;
        private float _yaw;
        private float _pitch = StartPitch;
        private float _distance = StartDistance;
        private float _sensitivity = 1f;
        private bool _invertY;
        private float _zoomSpeed = 1f;
        private Vector3 _focus;

        /// <summary>Horizontal camera angle, used to turn WASD into a world direction.</summary>
        public float Yaw => _yaw;

        public void SetTarget(Transform target)
        {
            _target = target;
            if (target != null)
            {
                _focus = target.position;
            }
        }

        /// <summary>Account settings (module 17): sensitivity and zoom are multipliers of the built-in steps.</summary>
        public void Configure(float sensitivity, bool invertY, float zoomSpeed)
        {
            _sensitivity = sensitivity;
            _invertY = invertY;
            _zoomSpeed = zoomSpeed;
        }

        public void Rotate(Vector2 delta)
        {
            float speed = LookSensitivity * _sensitivity;
            _yaw += delta.x * speed;
            _pitch = Mathf.Clamp(_pitch - (_invertY ? -delta.y : delta.y) * speed, MinPitch, MaxPitch);
        }

        /// <summary>Zooms one step per wheel notch; only the sign is used because scroll scale differs between platforms.</summary>
        public void ZoomBy(float scroll)
        {
            if (Mathf.Approximately(scroll, 0f))
            {
                return;
            }
            _distance = Mathf.Clamp(_distance - Mathf.Sign(scroll) * ZoomStep * _zoomSpeed, MinDistance, MaxDistance);
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }
            _focus = Vector3.Lerp(_focus, _target.position, 1f - Mathf.Exp(-FollowSharpness * Time.deltaTime));
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 pivot = _focus + Vector3.up * FocusHeight;
            transform.position = pivot - rotation * Vector3.forward * _distance;
            transform.rotation = rotation;
        }
    }
}
