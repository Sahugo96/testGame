using UnityEngine;

namespace Game.Client.World
{
    /// <summary>
    /// Moves the parts of a <see cref="Figure"/>: limbs swing with the distance walked and return to rest when the
    /// figure stops, spinners turn, bobbers float. Purely cosmetic: driven by the entity view, not by the server.
    /// </summary>
    public sealed class FigureMotion
    {
        private const float SwingDegrees = 30f;
        /// <summary>Step cycle: radians of the swing per metre walked (a full cycle in about 1.6 m).</summary>
        private const float RadiansPerMetre = 4f;
        private const float BlendPerSecond = 8f;
        private const float MovingMetresPerSecond = 0.2f;
        private const float FullTurn = Mathf.PI * 2f;

        private readonly Figure _figure;
        private float _phase;
        private float _blend;
        private float _time;

        public FigureMotion(Figure figure)
        {
            _figure = figure;
        }

        /// <summary>`movedMetres` — how far the figure moved over the ground this frame.</summary>
        public void Tick(float movedMetres, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }
            if (_figure.Limbs.Count > 0)
            {
                bool moving = movedMetres / deltaTime > MovingMetresPerSecond;
                if (moving || _blend > 0f)
                {
                    _phase = Mathf.Repeat(_phase + movedMetres * RadiansPerMetre, FullTurn);
                    _blend = Mathf.MoveTowards(_blend, moving ? 1f : 0f, BlendPerSecond * deltaTime);
                    float angle = Mathf.Sin(_phase) * SwingDegrees * _blend;
                    for (int i = 0; i < _figure.Limbs.Count; i++)
                    {
                        Figure.Limb limb = _figure.Limbs[i];
                        limb.Joint.localRotation = Quaternion.Euler(angle * limb.Sign, 0f, 0f);
                    }
                }
            }
            for (int i = 0; i < _figure.Spinners.Count; i++)
            {
                Figure.Spinner spinner = _figure.Spinners[i];
                spinner.Part.Rotate(spinner.DegreesPerSecond * deltaTime, Space.Self);
            }
            if (_figure.Bobbers.Count > 0)
            {
                _time += deltaTime;
                for (int i = 0; i < _figure.Bobbers.Count; i++)
                {
                    Figure.Bobber bobber = _figure.Bobbers[i];
                    Vector3 position = bobber.Part.localPosition;
                    position.y = bobber.BaseHeight + Mathf.Sin(_time * FullTurn * bobber.CyclesPerSecond) * bobber.Amplitude;
                    bobber.Part.localPosition = position;
                }
            }
        }
    }
}
