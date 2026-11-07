using System;

namespace Game.Shared.Core
{
    /// <summary>Engine-independent vector for server simulation (Game.Shared has no UnityEngine reference).</summary>
    [Serializable]
    public struct Vec3 : IEquatable<Vec3>
    {
        public float X;
        public float Y;
        public float Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vec3 Zero => new Vec3(0f, 0f, 0f);

        public float Length => (float)Math.Sqrt(X * X + Y * Y + Z * Z);

        public float LengthXZ => (float)Math.Sqrt(X * X + Z * Z);

        public Vec3 Normalized
        {
            get
            {
                float length = Length;
                return length > 0f ? this / length : Zero;
            }
        }

        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(Vec3 a, float k) => new Vec3(a.X * k, a.Y * k, a.Z * k);
        public static Vec3 operator /(Vec3 a, float k) => new Vec3(a.X / k, a.Y / k, a.Z / k);

        public static float DistanceXZ(Vec3 a, Vec3 b) => (a - b).LengthXZ;

        public bool Equals(Vec3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        public override bool Equals(object obj) => obj is Vec3 other && Equals(other);
        public override int GetHashCode() => X.GetHashCode() ^ (Y.GetHashCode() << 2) ^ (Z.GetHashCode() >> 2);
        public override string ToString() => $"({X:0.##}, {Y:0.##}, {Z:0.##})";
    }
}
