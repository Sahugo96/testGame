using UnityEngine;

namespace Game.Client.World
{
    /// <summary>Short-hand for assembling a <see cref="Figure"/>: shapes are added to the current parent (the model or a joint).</summary>
    public sealed class FigureBuilder
    {
        private const string BoxName = "Box";
        private const string BallName = "Ball";
        private const string TubeName = "Tube";
        private const string PillName = "Pill";

        public readonly Figure Figure = new Figure();
        private Transform _parent;

        public FigureBuilder(string name)
        {
            Figure.Model = new GameObject(name).transform;
            _parent = Figure.Model;
        }

        /// <summary>Following shapes are children of the model again.</summary>
        public void ToModel() => _parent = Figure.Model;

        /// <summary>An empty joint at a point of the model; following shapes are its children (positions relative to it).</summary>
        public Transform Joint(string name, Vector3 position) => Joint(name, position, Vector3.zero);

        public Transform Joint(string name, Vector3 position, Vector3 euler)
        {
            var joint = new GameObject(name).transform;
            joint.SetParent(Figure.Model, false);
            joint.localPosition = position;
            joint.localRotation = Quaternion.Euler(euler);
            _parent = joint;
            return joint;
        }

        /// <summary>A joint that swings while walking (a leg, an arm).</summary>
        public Transform Limb(string name, Vector3 position, float sign)
        {
            Transform joint = Joint(name, position);
            Figure.Limbs.Add(new Figure.Limb { Joint = joint, Sign = sign });
            return joint;
        }

        public Transform Box(Vector3 position, Vector3 size, Color color) => Add(BoxName, PrimitiveType.Cube, position, size, Vector3.zero, WorldPrimitives.Colored(color));

        public Transform Box(Vector3 position, Vector3 size, Vector3 euler, Color color) => Add(BoxName, PrimitiveType.Cube, position, size, euler, WorldPrimitives.Colored(color));

        public Transform Ball(Vector3 position, Vector3 size, Color color) => Add(BallName, PrimitiveType.Sphere, position, size, Vector3.zero, WorldPrimitives.Colored(color));

        /// <summary>A cylinder: `size.y` is its full height (Unity's cylinder mesh is 2 units tall).</summary>
        public Transform Tube(Vector3 position, Vector3 size, Vector3 euler, Color color) =>
            Add(TubeName, PrimitiveType.Cylinder, position, new Vector3(size.x, size.y * 0.5f, size.z), euler, WorldPrimitives.Colored(color));

        public Transform Tube(Vector3 position, Vector3 size, Color color) => Tube(position, size, Vector3.zero, color);

        /// <summary>A capsule: `size.y` is its full height (Unity's capsule mesh is 2 units tall).</summary>
        public Transform Pill(Vector3 position, Vector3 size, Vector3 euler, Color color) =>
            Add(PillName, PrimitiveType.Capsule, position, new Vector3(size.x, size.y * 0.5f, size.z), euler, WorldPrimitives.Colored(color));

        public Transform GlowBox(Vector3 position, Vector3 size, Vector3 euler, Color color) => Add(BoxName, PrimitiveType.Cube, position, size, euler, WorldPrimitives.Glow(color));

        public Transform GlowBall(Vector3 position, Vector3 size, Color color) => Add(BallName, PrimitiveType.Sphere, position, size, Vector3.zero, WorldPrimitives.Glow(color));

        public Transform GlowDisc(Vector3 position, float diameter, float thickness, Vector3 euler, Color color) =>
            Add(TubeName, PrimitiveType.Cylinder, position, new Vector3(diameter, thickness * 0.5f, diameter), euler, WorldPrimitives.Glow(color));

        public void Spin(Transform part, Vector3 degreesPerSecond) =>
            Figure.Spinners.Add(new Figure.Spinner { Part = part, DegreesPerSecond = degreesPerSecond });

        public void Bob(Transform part, float amplitude, float cyclesPerSecond) =>
            Figure.Bobbers.Add(new Figure.Bobber { Part = part, BaseHeight = part.localPosition.y, Amplitude = amplitude, CyclesPerSecond = cyclesPerSecond });

        private Transform Add(string name, PrimitiveType type, Vector3 position, Vector3 size, Vector3 euler, Material material) =>
            WorldPrimitives.Part(name, _parent, type, position, size, euler, material);
    }
}
