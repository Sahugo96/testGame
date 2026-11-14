using UnityEngine;

namespace Game.Client.World.Motion
{
    /// <summary>
    /// Moves a BVH motion onto the humanoid figure. The two skeletons differ (the file has fifty joints and rests in
    /// a T-pose, the figure has sixteen and rests with its arms down), so joint angles cannot be copied. Instead, for
    /// every bone of the figure the direction and turn of the matching BVH joint in the character's space is taken:
    /// in a BVH every joint is unrotated in the rest pose, so a joint's rotation in that space is exactly how far it
    /// has turned from rest. Limbs are then corrected for the different rest directions (a T-pose arm points sideways,
    /// ours points down); the body, the head and the feet rest the same way in both and need no correction.
    /// The character does not travel: only the up-and-down motion of the hips is kept, because the server, not the
    /// animation, decides where the player stands.
    /// </summary>
    public static class BvhRetarget
    {
        /// <summary>Which BVH joint drives a bone, and the joint whose offset gives the bone's rest direction (null — no correction).</summary>
        private struct Link
        {
            public HumanBone Bone;
            public string Joint;
            public string Next;

            public Link(HumanBone bone, string joint, string next)
            {
                Bone = bone;
                Joint = joint;
                Next = next;
            }
        }

        /// <summary>Joint names of DeepMotion's skeleton. Another exporter needs its own table (the joints are the same, the names differ).</summary>
        private static readonly Link[] DeepMotion =
        {
            new Link(HumanBone.Hips, "hips_JNT", null),
            new Link(HumanBone.Waist, "spine_JNT", null),
            new Link(HumanBone.Chest, "spine2_JNT", null),
            new Link(HumanBone.Head, "head_JNT", null),
            new Link(HumanBone.UpperArmL, "l_arm_JNT", "l_forearm_JNT"),
            new Link(HumanBone.ForearmL, "l_forearm_JNT", "l_hand_JNT"),
            new Link(HumanBone.HandL, "l_hand_JNT", "l_handMiddle1_JNT"),
            new Link(HumanBone.UpperArmR, "r_arm_JNT", "r_forearm_JNT"),
            new Link(HumanBone.ForearmR, "r_forearm_JNT", "r_hand_JNT"),
            new Link(HumanBone.HandR, "r_hand_JNT", "r_handMiddle1_JNT"),
            new Link(HumanBone.UpperLegL, "l_upleg_JNT", "l_leg_JNT"),
            new Link(HumanBone.LowerLegL, "l_leg_JNT", "l_foot_JNT"),
            new Link(HumanBone.FootL, "l_foot_JNT", null),
            new Link(HumanBone.UpperLegR, "r_upleg_JNT", "r_leg_JNT"),
            new Link(HumanBone.LowerLegR, "r_leg_JNT", "r_foot_JNT"),
            new Link(HumanBone.FootR, "r_foot_JNT", null),
        };

        /// <summary>The chain from the hips to the ground: its length is the height of the hips when standing.</summary>
        private static readonly string[] LegChain = { "l_upleg_JNT", "l_leg_JNT", "l_foot_JNT", "l_toebase_JNT" };

        /// <summary>In the figure's rest pose arms and legs point straight down.</summary>
        private static readonly Vector3 LimbRestDirection = Vector3.down;

        /// <summary>The clip for the humanoid figure; null (with the reason in `error`) if the skeleton is not the expected one.</summary>
        public static HumanClip ToHumanClip(BvhMotion motion, out string error)
        {
            int jointCount = motion.Names.Count;
            var jointOf = new int[HumanRig.BoneCount];
            var correction = new Quaternion[HumanRig.BoneCount];
            var needed = new bool[jointCount];
            foreach (Link link in DeepMotion)
            {
                int joint = motion.IndexOf(link.Joint);
                int next = link.Next == null ? joint : motion.IndexOf(link.Next);
                if (joint < 0 || next < 0)
                {
                    error = $"joint '{(joint < 0 ? link.Joint : link.Next)}' is missing (only DeepMotion's skeleton is known)";
                    return null;
                }
                jointOf[(int)link.Bone] = joint;
                correction[(int)link.Bone] = link.Next == null
                    ? Quaternion.identity
                    : Quaternion.FromToRotation(LimbRestDirection, ToUnity(motion.Offsets[next]).normalized);
                for (int j = joint; j >= 0; j = motion.Parents[j])
                {
                    needed[j] = true;
                }
            }
            float standHeight = 0f;
            foreach (string name in LegChain)
            {
                int joint = motion.IndexOf(name);
                standHeight -= joint < 0 ? 0f : motion.Offsets[joint].y;
            }
            if (standHeight <= 0f)
            {
                error = "the legs have no length";
                return null;
            }

            int root = jointOf[(int)HumanBone.Hips];
            var world = new Quaternion[jointCount];
            var rotations = new Quaternion[motion.FrameCount * HumanRig.BoneCount];
            var hipLift = new float[motion.FrameCount];
            for (int frame = 0; frame < motion.FrameCount; frame++)
            {
                for (int joint = 0; joint < jointCount; joint++)
                {
                    if (needed[joint])
                    {
                        Quaternion local = ToUnity(motion.LocalRotation(joint, frame));
                        int parent = motion.Parents[joint];
                        world[joint] = parent < 0 ? local : world[parent] * local;
                    }
                }
                int row = frame * HumanRig.BoneCount;
                for (int bone = 0; bone < HumanRig.BoneCount; bone++)
                {
                    rotations[row + bone] = world[jointOf[bone]] * correction[bone];
                }
                hipLift[frame] = (motion.Position(root, frame).y - standHeight) / standHeight;
            }
            error = null;
            return new HumanClip(motion.FrameCount, motion.FrameTime, rotations, hipLift);
        }

        // BVH is right-handed with +X to the character's left; Unity is left-handed with +X to the right: mirror X.
        private static Vector3 ToUnity(Vector3 v) => new Vector3(-v.x, v.y, v.z);

        private static Quaternion ToUnity(Quaternion q) => new Quaternion(q.x, -q.y, -q.z, q.w);
    }
}
