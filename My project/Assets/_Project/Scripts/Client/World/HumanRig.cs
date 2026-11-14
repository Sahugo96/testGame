using UnityEngine;

namespace Game.Client.World
{
    /// <summary>Joints of the humanoid figure, parents before children (motion clips are applied in this order).</summary>
    public enum HumanBone
    {
        Hips,
        Waist,
        Chest,
        Head,
        UpperArmL,
        ForearmL,
        HandL,
        UpperArmR,
        ForearmR,
        HandR,
        UpperLegL,
        LowerLegL,
        FootL,
        UpperLegR,
        LowerLegR,
        FootR,
    }

    /// <summary>
    /// The skeleton of a humanoid figure: one joint per <see cref="HumanBone"/>. In the rest pose the figure stands
    /// upright facing +Z with its arms and legs straight down and every joint unrotated — motion clips rely on that.
    /// </summary>
    public sealed class HumanRig
    {
        public const int BoneCount = (int)HumanBone.FootR + 1;

        public readonly Transform[] Bones = new Transform[BoneCount];
        /// <summary>Height of the hips above the ground in the rest pose: the scale of a clip's up-and-down motion.</summary>
        public float HipHeight;
    }
}
