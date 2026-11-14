using UnityEngine;

namespace Game.Client.World
{
    /// <summary>Figures of players and mobs.</summary>
    public static partial class WorldFigures
    {
        private const float LegX = 0.15f;
        private const float HipY = 0.88f;
        private const float ShoulderX = 0.42f;
        private const float ShoulderY = 1.46f;
        private const float HandY = -0.62f;
        private static readonly Vector3 HumanSize = new Vector3(0.95f, 2f, 0.7f);
        private const float HumanLieHeight = 0.2f;

        /// <summary>Legs, torso, arms and head; returns the right arm joint (weapons are held in it), `left` — the left one.</summary>
        private static Transform HumanBody(FigureBuilder b, Color tunic, Color trousers, out Transform left)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                b.Limb(side < 0 ? "LegL" : "LegR", V(LegX * side, HipY, 0f), side);
                b.Box(V(0f, -0.4f, 0f), V(0.24f, 0.8f, 0.26f), trousers);
                b.Box(V(0f, -0.81f, 0.05f), V(0.26f, 0.14f, 0.38f), WorldPalette.Boots);
            }
            b.ToModel();
            b.Box(V(0f, 1.2f, 0f), V(0.64f, 0.66f, 0.36f), tunic);
            b.Box(V(0f, 0.9f, 0f), V(0.66f, 0.1f, 0.38f), WorldPalette.Leather);
            b.Box(V(0f, 1.56f, 0f), V(0.2f, 0.08f, 0.2f), WorldPalette.Skin);
            b.Ball(V(0f, 1.76f, 0f), V(0.44f, 0.46f, 0.44f), WorldPalette.Skin);
            b.Box(V(-0.09f, 1.79f, 0.2f), V(0.06f, 0.07f, 0.04f), WorldPalette.Eye);
            b.Box(V(0.09f, 1.79f, 0.2f), V(0.06f, 0.07f, 0.04f), WorldPalette.Eye);
            left = HumanArm(b, "ArmL", -1f, tunic);
            Transform right = HumanArm(b, "ArmR", 1f, tunic);
            b.ToModel();
            b.Figure.Size = HumanSize;
            b.Figure.LieHeight = HumanLieHeight;
            return right;
        }

        private static Transform HumanArm(FigureBuilder b, string name, float side, Color sleeve)
        {
            // Arms swing opposite to the leg of the same side.
            Transform joint = b.Limb(name, V(ShoulderX * side, ShoulderY, 0f), -side);
            b.Box(V(0f, -0.26f, 0f), V(0.2f, 0.56f, 0.22f), sleeve);
            b.Ball(V(0f, HandY, 0f), Cube(0.2f), WorldPalette.Skin);
            return joint;
        }

        private static void PlainHumanoid(FigureBuilder b, Color tint)
        {
            HumanBody(b, tint, WorldPalette.Trousers, out _);
            b.Ball(V(0f, 1.87f, -0.04f), V(0.47f, 0.3f, 0.47f), WorldPalette.Hair);
        }

        /// <summary>A robe, a pointed hat and a staff with a glowing orb.</summary>
        private static void CasterHumanoid(FigureBuilder b, Color tint)
        {
            Color dark = WorldPalette.Tone(tint, 0.7f);
            HumanBody(b, tint, dark, out _);
            b.Box(V(0f, 0.74f, -0.02f), V(0.6f, 0.3f, 0.4f), tint);
            b.Tube(V(0f, 1.96f, 0f), V(0.78f, 0.05f, 0.78f), dark);
            b.Tube(V(0f, 2.08f, 0f), V(0.42f, 0.22f, 0.42f), dark);
            b.Tube(V(0f, 2.27f, -0.02f), V(0.28f, 0.2f, 0.28f), V(-8f, 0f, 0f), dark);
            b.Tube(V(0f, 2.44f, -0.06f), V(0.14f, 0.18f, 0.14f), V(-16f, 0f, 0f), dark);
            b.Tube(V(0f, 2f, 0f), V(0.44f, 0.05f, 0.44f), WorldPalette.Gold);
            b.Joint("Staff", V(ShoulderX + 0.14f, 0f, 0.16f));
            b.Tube(V(0f, 1f, 0f), V(0.07f, 2f, 0.07f), WorldPalette.Wood);
            b.Tube(V(0f, 1.98f, 0f), V(0.16f, 0.08f, 0.16f), WorldPalette.Gold);
            Transform orb = b.GlowBall(V(0f, 2.14f, 0f), Cube(0.22f), WorldPalette.MagicGlow);
            b.Bob(orb, 0.03f, 0.6f);
            b.ToModel();
            b.Figure.Size = V(1.1f, 2.4f, 0.8f);
        }

        /// <summary>A helmet with a crest, shoulder plates, a sword and a round shield.</summary>
        private static void FighterHumanoid(FigureBuilder b, Color tint)
        {
            Transform right = HumanBody(b, tint, WorldPalette.Iron, out Transform left);
            b.Ball(V(0f, 1.85f, -0.01f), V(0.5f, 0.38f, 0.5f), WorldPalette.Steel);
            b.Box(V(0f, 2.03f, -0.03f), V(0.07f, 0.14f, 0.38f), WorldPalette.Red);
            b.Box(V(0f, 1.25f, 0.19f), V(0.5f, 0.5f, 0.04f), WorldPalette.Steel);
            b.Ball(V(-ShoulderX, ShoulderY + 0.02f, 0f), V(0.32f, 0.26f, 0.34f), WorldPalette.Steel);
            b.Ball(V(ShoulderX, ShoulderY + 0.02f, 0f), V(0.32f, 0.26f, 0.34f), WorldPalette.Steel);
            // Sword in the right hand: the blade points forward.
            WorldPrimitives.Part("Blade", right, PrimitiveType.Cube, V(0f, HandY, 0.5f), V(0.05f, 0.1f, 0.8f), Vector3.zero, WorldPrimitives.Colored(WorldPalette.Steel));
            WorldPrimitives.Part("Guard", right, PrimitiveType.Cube, V(0f, HandY, 0.1f), V(0.24f, 0.06f, 0.05f), Vector3.zero, WorldPrimitives.Colored(WorldPalette.Gold));
            // Shield on the left arm: a disc facing outwards.
            WorldPrimitives.Part("Shield", left, PrimitiveType.Cylinder, V(-0.15f, -0.36f, 0.02f), V(0.62f, 0.035f, 0.62f), V(0f, 0f, 90f), WorldPrimitives.Colored(WorldPalette.Wood));
            WorldPrimitives.Part("ShieldRim", left, PrimitiveType.Cylinder, V(-0.14f, -0.36f, 0.02f), V(0.68f, 0.02f, 0.68f), V(0f, 0f, 90f), WorldPrimitives.Colored(WorldPalette.Steel));
            WorldPrimitives.Part("ShieldBoss", left, PrimitiveType.Sphere, V(-0.19f, -0.36f, 0.02f), V(0.1f, 0.2f, 0.2f), Vector3.zero, WorldPrimitives.Colored(WorldPalette.Gold));
            b.Figure.Size = V(1.2f, 2.1f, 1f);
        }

        /// <summary>A player's shop: the seller with a pack behind a rug with goods.</summary>
        private static void Merchant(FigureBuilder b, Color tint)
        {
            HumanBody(b, tint, WorldPalette.Trousers, out _);
            b.Figure.Limbs.Clear();   // the seller stands still
            b.Tube(V(0f, 1.95f, 0f), V(0.7f, 0.05f, 0.7f), WorldPalette.Straw);
            b.Tube(V(0f, 2.02f, 0f), V(0.36f, 0.14f, 0.36f), WorldPalette.Straw);
            b.Box(V(0f, 1.25f, -0.3f), V(0.52f, 0.62f, 0.3f), WorldPalette.Leather);
            b.Tube(V(0f, 1.62f, -0.3f), V(0.2f, 0.56f, 0.2f), V(0f, 0f, 90f), WorldPalette.Cream);
            b.Box(V(0f, 0.02f, 1f), V(1.7f, 0.04f, 1.1f), WorldPalette.Red);
            b.Box(V(0f, 0.045f, 1f), V(1.4f, 0.02f, 0.8f), WorldPalette.Cream);
            b.Box(V(-0.5f, 0.2f, 1f), V(0.34f, 0.3f, 0.34f), V(0f, 15f, 0f), WorldPalette.Wood);
            b.Ball(V(0f, 0.17f, 0.9f), Cube(0.24f), WorldPalette.GoodsRed);
            b.Ball(V(0.2f, 0.15f, 1.12f), Cube(0.2f), WorldPalette.GoodsGreen);
            b.Tube(V(0.55f, 0.2f, 1f), V(0.2f, 0.3f, 0.2f), WorldPalette.GoodsBlue);
            b.Figure.Size = V(1.8f, 2.1f, 1.8f);
        }

        /// <summary>A training dummy: a stuffed sack on a post with a target on its chest.</summary>
        private static void Dummy(FigureBuilder b, Color tint)
        {
            b.Tube(V(0f, 0.05f, 0f), V(0.9f, 0.1f, 0.9f), WorldPalette.WoodDark);
            b.Tube(V(0f, 0.65f, 0f), V(0.14f, 1.2f, 0.14f), WorldPalette.Wood);
            b.Pill(V(0f, 1.15f, 0f), V(0.62f, 0.9f, 0.5f), Vector3.zero, tint);
            b.Tube(V(0f, 1.38f, 0f), V(0.1f, 1.3f, 0.1f), V(0f, 0f, 90f), WorldPalette.Wood);
            b.Ball(V(-0.68f, 1.38f, 0f), Cube(0.2f), WorldPalette.Straw);
            b.Ball(V(0.68f, 1.38f, 0f), Cube(0.2f), WorldPalette.Straw);
            b.Ball(V(0f, 1.78f, 0f), V(0.4f, 0.42f, 0.4f), WorldPalette.Straw);
            b.Tube(V(0f, 1.98f, 0f), V(0.38f, 0.14f, 0.38f), WorldPalette.Iron);
            b.Tube(V(0f, 1.18f, 0.245f), V(0.36f, 0.03f, 0.36f), V(90f, 0f, 0f), WorldPalette.Cream);
            b.Tube(V(0f, 1.18f, 0.257f), V(0.2f, 0.03f, 0.2f), V(90f, 0f, 0f), WorldPalette.Red);
            b.Figure.Size = V(1.1f, 2.05f, 0.9f);
            b.Figure.LieHeight = 0.25f;
        }

        private static void Wolf(FigureBuilder b, Color tint)
        {
            Color dark = WorldPalette.Tone(tint, 0.7f);
            Color light = WorldPalette.Tone(tint, 1.25f);
            b.Pill(V(0f, 0.62f, -0.05f), V(0.46f, 1.15f, 0.46f), V(90f, 0f, 0f), tint);
            b.Ball(V(0f, 0.66f, 0.3f), V(0.54f, 0.56f, 0.6f), tint);
            b.Ball(V(0f, 0.86f, 0.64f), V(0.4f, 0.38f, 0.42f), tint);
            b.Box(V(0f, 0.8f, 0.88f), V(0.17f, 0.15f, 0.26f), light);
            b.Box(V(0f, 0.84f, 1.02f), V(0.08f, 0.07f, 0.05f), WorldPalette.Eye);
            b.Box(V(-0.12f, 1.07f, 0.58f), V(0.1f, 0.17f, 0.06f), V(-15f, 0f, 10f), dark);
            b.Box(V(0.12f, 1.07f, 0.58f), V(0.1f, 0.17f, 0.06f), V(-15f, 0f, -10f), dark);
            b.GlowBall(V(-0.1f, 0.92f, 0.81f), Cube(0.06f), WorldPalette.BeastEye);
            b.GlowBall(V(0.1f, 0.92f, 0.81f), Cube(0.06f), WorldPalette.BeastEye);
            b.Box(V(0f, 0.72f, -0.74f), V(0.1f, 0.1f, 0.5f), V(-35f, 0f, 0f), dark);
            for (int side = -1; side <= 1; side += 2)
            {
                // Diagonal pairs move together.
                b.Limb("LegFront", V(0.16f * side, 0.5f, 0.38f), side);
                b.Box(V(0f, -0.25f, 0f), V(0.13f, 0.5f, 0.14f), dark);
                b.Limb("LegBack", V(0.16f * side, 0.5f, -0.42f), -side);
                b.Box(V(0f, -0.25f, 0f), V(0.13f, 0.5f, 0.14f), dark);
            }
            b.ToModel();
            b.Figure.Size = V(0.8f, 1.2f, 1.9f);
            b.Figure.LieHeight = 0.25f;
        }

        private static void Golem(FigureBuilder b, Color tint)
        {
            Color dark = WorldPalette.Tone(tint, 0.85f);
            tint = WorldPalette.Tone(tint, 1.2f);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Limb("Leg", V(0.34f * side, 0.86f, 0f), side);
                b.Box(V(0f, -0.43f, 0f), V(0.42f, 0.86f, 0.5f), dark);
                b.Limb("Arm", V(0.82f * side, 1.82f, 0f), -side);
                b.Ball(V(0f, 0f, 0f), Cube(0.6f), dark);
                b.Box(V(0f, -0.62f, 0f), V(0.36f, 1.1f, 0.42f), tint);
                b.Ball(V(0f, -1.25f, 0f), V(0.55f, 0.5f, 0.55f), dark);
            }
            b.ToModel();
            b.Box(V(0f, 1.42f, 0f), V(1.25f, 1f, 0.75f), tint);
            b.Box(V(0f, 0.95f, 0f), V(0.9f, 0.3f, 0.6f), dark);
            b.GlowBox(V(0f, 1.46f, 0.37f), V(0.28f, 0.28f, 0.06f), V(0f, 0f, 45f), WorldPalette.GolemCore);
            b.Box(V(0f, 2.14f, 0.05f), V(0.52f, 0.44f, 0.5f), tint);
            b.GlowBox(V(-0.12f, 2.17f, 0.3f), V(0.1f, 0.06f, 0.03f), Vector3.zero, WorldPalette.GolemCore);
            b.GlowBox(V(0.12f, 2.17f, 0.3f), V(0.1f, 0.06f, 0.03f), Vector3.zero, WorldPalette.GolemCore);
            b.Ball(V(0.3f, 1.93f, -0.1f), V(0.5f, 0.12f, 0.45f), WorldPalette.Moss);
            b.Ball(V(-0.82f, 2.1f, 0f), V(0.4f, 0.1f, 0.4f), WorldPalette.Moss);
            b.Figure.Size = V(2.2f, 2.5f, 1.1f);
            b.Figure.LieHeight = 0.4f;
        }

        /// <summary>The cube monster: a cube with a face.</summary>
        private static void CubeCreature(FigureBuilder b, Color tint)
        {
            Transform body = b.Joint("Body", V(0f, 0.5f, 0f));
            b.Box(Vector3.zero, Cube(0.94f), tint);
            b.Box(V(0f, 0.475f, 0f), V(0.7f, 0.02f, 0.7f), WorldPalette.Tone(tint, 1.3f));
            b.Box(V(-0.2f, 0.12f, 0.47f), V(0.14f, 0.2f, 0.03f), WorldPalette.Eye);
            b.Box(V(0.2f, 0.12f, 0.47f), V(0.14f, 0.2f, 0.03f), WorldPalette.Eye);
            b.Box(V(0f, -0.16f, 0.47f), V(0.34f, 0.06f, 0.03f), WorldPalette.Eye);
            b.ToModel();
            b.Bob(body, 0.04f, 0.5f);
            b.Figure.Size = Cube(1f);
            b.Figure.LieHeight = 0.47f;
        }
    }
}
