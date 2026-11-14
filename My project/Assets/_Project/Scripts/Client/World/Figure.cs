using System.Collections.Generic;
using UnityEngine;

namespace Game.Client.World
{
    /// <summary>
    /// A figure assembled from primitive shapes: the look of a player, mob, station or drop until real models arrive.
    /// Holds what the entity view needs: the model root (stands on the ground at its origin), the size for the click
    /// collider, and the parts that move (limbs swing while walking, some parts spin or bob all the time).
    /// </summary>
    public sealed class Figure
    {
        /// <summary>Root of all parts; its origin is the point on the ground under the figure.</summary>
        public Transform Model;
        /// <summary>Width, height and depth of the click volume.</summary>
        public Vector3 Size = Vector3.one;
        /// <summary>How high the model is lifted when it lies dead (about half of its thickness).</summary>
        public float LieHeight;
        /// <summary>False for things that do not fall over when "dead" (none yet; stations and drops never die).</summary>
        public bool FallsWhenDead = true;

        public readonly List<Limb> Limbs = new List<Limb>();
        public readonly List<Spinner> Spinners = new List<Spinner>();
        public readonly List<Bobber> Bobbers = new List<Bobber>();

        /// <summary>A joint that swings forward and back while the figure walks; opposite limbs have opposite signs.</summary>
        public struct Limb
        {
            public Transform Joint;
            public float Sign;
        }

        /// <summary>A part that turns all the time (a grinding wheel, a gem over a drop).</summary>
        public struct Spinner
        {
            public Transform Part;
            public Vector3 DegreesPerSecond;
        }

        /// <summary>A part that floats up and down.</summary>
        public struct Bobber
        {
            public Transform Part;
            public float BaseHeight;
            public float Amplitude;
            public float CyclesPerSecond;
        }
    }
}
