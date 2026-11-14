using UnityEngine;

namespace Game.Client.World.Motion
{
    /// <summary>Plays a <see cref="HumanClip"/> on one figure once: eases in from the rest pose, eases out at the end or when stopped.</summary>
    public sealed class HumanClipPlayer
    {
        private const float BlendSeconds = 0.25f;

        private readonly HumanRig _rig;
        private readonly Transform _hips;
        private readonly Vector3 _hipsRest;
        private HumanClip _clip;
        private float _time;
        private float _weight;
        private bool _stopping;

        public HumanClipPlayer(HumanRig rig)
        {
            _rig = rig;
            _hips = rig.Bones[(int)HumanBone.Hips];
            _hipsRest = _hips.localPosition;
        }

        public bool IsPlaying => _clip != null;

        public void Play(HumanClip clip)
        {
            _clip = clip;
            _time = 0f;
            _stopping = false;
        }

        /// <summary>Lets the figure ease back to its rest pose.</summary>
        public void Stop() => _stopping = _clip != null;

        /// <summary>`space` — the rotation of the figure's model in the world: the clip's rotations are relative to it.</summary>
        public void Tick(float deltaTime, Quaternion space)
        {
            if (_clip == null)
            {
                return;
            }
            _time += deltaTime;
            if (_time >= _clip.Duration - BlendSeconds)
            {
                _stopping = true;
            }
            _weight = Mathf.MoveTowards(_weight, _stopping ? 0f : 1f, deltaTime / BlendSeconds);
            if (_stopping && _weight <= 0f)
            {
                _clip = null;
                for (int bone = 0; bone < HumanRig.BoneCount; bone++)
                {
                    _rig.Bones[bone].localRotation = Quaternion.identity;
                }
                _hips.localPosition = _hipsRest;
                return;
            }
            // Parents come first in HumanBone, so setting world rotations in order gives every bone its own turn.
            for (int bone = 0; bone < HumanRig.BoneCount; bone++)
            {
                Quaternion pose = _clip.Rotation((HumanBone)bone, _time);
                _rig.Bones[bone].rotation = space * (_weight < 1f ? Quaternion.Slerp(Quaternion.identity, pose, _weight) : pose);
            }
            _hips.localPosition = _hipsRest + Vector3.up * (_clip.HipLift(_time) * _rig.HipHeight * _weight);
        }
    }
}
