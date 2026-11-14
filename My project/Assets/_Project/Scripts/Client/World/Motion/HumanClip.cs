using UnityEngine;

namespace Game.Client.World.Motion
{
    /// <summary>
    /// A motion ready to play on the humanoid figure: per frame, the rotation of every <see cref="HumanBone"/> in the
    /// figure's own space (identity = the rest pose) and the height of the hips. Made once from a BVH file by
    /// <see cref="BvhRetarget"/> and shared by all figures.
    /// </summary>
    public sealed class HumanClip
    {
        private readonly Quaternion[] _rotations;
        /// <summary>Hips above (+) or below (−) their rest height, as a fraction of that height.</summary>
        private readonly float[] _hipLift;
        private readonly int _frameCount;
        private readonly float _frameTime;

        public HumanClip(int frameCount, float frameTime, Quaternion[] rotations, float[] hipLift)
        {
            _frameCount = frameCount;
            _frameTime = frameTime;
            _rotations = rotations;
            _hipLift = hipLift;
        }

        public float Duration => (_frameCount - 1) * _frameTime;

        public Quaternion Rotation(HumanBone bone, float time)
        {
            Locate(time, out int frame, out int next, out float blend);
            return Quaternion.Slerp(_rotations[frame * HumanRig.BoneCount + (int)bone], _rotations[next * HumanRig.BoneCount + (int)bone], blend);
        }

        public float HipLift(float time)
        {
            Locate(time, out int frame, out int next, out float blend);
            return Mathf.Lerp(_hipLift[frame], _hipLift[next], blend);
        }

        private void Locate(float time, out int frame, out int next, out float blend)
        {
            float position = Mathf.Clamp(time / _frameTime, 0f, _frameCount - 1);
            frame = Mathf.Min((int)position, _frameCount - 1);
            next = Mathf.Min(frame + 1, _frameCount - 1);
            blend = position - frame;
        }
    }
}
