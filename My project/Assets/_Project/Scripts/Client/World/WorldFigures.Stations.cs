using UnityEngine;

namespace Game.Client.World
{
    /// <summary>Figures of stations: grinding wheel, anvil, market stall, mailbox, a place for a player's shop.</summary>
    public static partial class WorldFigures
    {
        private static void Whetstone(FigureBuilder b, Color tint)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(V(0.42f * side, 0.43f, 0.2f), V(0.08f, 0.92f, 0.08f), V(-16f, 0f, 0f), WorldPalette.Wood);
                b.Box(V(0.42f * side, 0.43f, -0.2f), V(0.08f, 0.92f, 0.08f), V(16f, 0f, 0f), WorldPalette.Wood);
            }
            b.Box(V(0f, 0.2f, 0f), V(0.92f, 0.06f, 0.06f), WorldPalette.WoodDark);
            b.Box(V(0f, 0.3f, 0f), V(0.5f, 0.14f, 1f), WorldPalette.WoodDark);
            b.Box(V(0f, 0.375f, 0f), V(0.4f, 0.01f, 0.9f), WorldPalette.Water);
            b.Tube(V(0f, 0.84f, 0f), V(0.07f, 1.08f, 0.07f), V(0f, 0f, 90f), WorldPalette.Iron);
            b.Box(V(0.56f, 0.72f, 0f), V(0.04f, 0.3f, 0.05f), WorldPalette.Iron);
            // The wheel turns on its axle; the darker patches make the turning visible.
            Transform wheel = b.Joint("Wheel", V(0f, 0.84f, 0f), V(0f, 0f, 90f));
            b.Tube(Vector3.zero, V(0.9f, 0.2f, 0.9f), tint);
            b.Box(V(0.3f, 0f, 0f), V(0.14f, 0.21f, 0.2f), WorldPalette.Tone(tint, 0.75f));
            b.Box(V(-0.2f, 0f, 0.24f), V(0.12f, 0.21f, 0.12f), WorldPalette.Tone(tint, 0.8f));
            b.Tube(Vector3.zero, V(0.16f, 0.24f, 0.16f), WorldPalette.Iron);
            b.ToModel();
            b.Spin(wheel, V(0f, 70f, 0f));
            b.Figure.Size = V(1.3f, 1.4f, 1.2f);
        }

        private static void Anvil(FigureBuilder b, Color tint)
        {
            b.Tube(V(0f, 0.28f, 0f), V(0.8f, 0.56f, 0.8f), WorldPalette.Wood);
            b.Tube(V(0f, 0.565f, 0f), V(0.7f, 0.01f, 0.7f), WorldPalette.WoodLight);
            b.Box(V(0f, 0.62f, 0f), V(0.6f, 0.1f, 0.4f), tint);
            b.Box(V(0f, 0.76f, 0f), V(0.32f, 0.2f, 0.26f), tint);
            b.Box(V(0f, 0.94f, 0f), V(0.9f, 0.18f, 0.4f), WorldPalette.Tone(tint, 1.35f));
            b.Ball(V(0.56f, 0.95f, 0f), V(0.56f, 0.15f, 0.3f), WorldPalette.Tone(tint, 1.2f));
            b.Tube(V(-0.08f, 1.06f, 0.04f), V(0.05f, 0.56f, 0.05f), V(0f, 25f, 90f), WorldPalette.Wood);
            b.Box(V(-0.33f, 1.07f, 0.16f), V(0.14f, 0.12f, 0.24f), V(0f, 25f, 0f), WorldPalette.Steel);
            // A bucket of glowing coals beside the anvil.
            b.Tube(V(-0.85f, 0.2f, 0.3f), V(0.44f, 0.4f, 0.44f), WorldPalette.Iron);
            b.GlowDisc(V(-0.85f, 0.405f, 0.3f), 0.36f, 0.01f, Vector3.zero, WorldPalette.GolemCore);
            b.Figure.Size = V(1.9f, 1.2f, 1.1f);
        }

        private static void MarketStall(FigureBuilder b, Color tint)
        {
            const int strips = 6;
            const float stripWidth = 0.44f;
            b.Box(V(0f, 0.5f, 0.3f), V(2.4f, 1f, 0.7f), WorldPalette.Wood);
            b.Box(V(0f, 1.02f, 0.3f), V(2.54f, 0.06f, 0.82f), WorldPalette.WoodLight);
            b.Box(V(0f, 1.2f, -0.55f), V(2.4f, 1.7f, 0.06f), WorldPalette.WoodDark);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(V(1.2f * side, 1.22f, -0.55f), V(0.1f, 2.44f, 0.1f), WorldPalette.WoodDark);
                b.Box(V(1.2f * side, 1.05f, 0.72f), V(0.1f, 2.1f, 0.1f), WorldPalette.WoodDark);
            }
            for (int i = 0; i < strips; i++)
            {
                float x = (i - (strips - 1) * 0.5f) * stripWidth;
                b.Box(V(x, 2.3f, 0.1f), V(stripWidth, 0.06f, 1.7f), V(13f, 0f, 0f), i % 2 == 0 ? tint : WorldPalette.Cream);
            }
            b.Box(V(-0.85f, 1.15f, 0.3f), V(0.3f, 0.2f, 0.3f), V(0f, 12f, 0f), WorldPalette.GoodsBlue);
            b.Ball(V(-0.35f, 1.16f, 0.3f), Cube(0.22f), WorldPalette.GoodsRed);
            b.Ball(V(-0.12f, 1.15f, 0.38f), Cube(0.2f), WorldPalette.GoodsGreen);
            b.Tube(V(0.4f, 1.2f, 0.3f), V(0.2f, 0.3f, 0.2f), WorldPalette.Cream);
            b.Box(V(0.85f, 1.12f, 0.3f), V(0.36f, 0.14f, 0.26f), WorldPalette.Leather);
            b.Tube(V(0f, 0.55f, 0.66f), V(0.5f, 0.04f, 0.5f), V(90f, 0f, 0f), WorldPalette.Gold);
            b.Box(V(1.6f, 0.3f, 0.2f), Cube(0.6f), V(0f, -10f, 0f), WorldPalette.Wood);
            b.Box(V(1.62f, 0.8f, 0.2f), V(0.45f, 0.4f, 0.45f), V(0f, 20f, 0f), WorldPalette.WoodLight);
            b.Figure.Size = V(2.8f, 2.6f, 1.9f);
        }

        private static void Mailbox(FigureBuilder b, Color tint)
        {
            b.Ball(V(0f, 0.05f, 0f), V(0.55f, 0.18f, 0.55f), WorldPalette.Stone);
            b.Tube(V(0f, 0.5f, 0f), V(0.14f, 1f, 0.14f), WorldPalette.Wood);
            b.Box(V(0f, 1.2f, 0f), V(0.5f, 0.4f, 0.75f), tint);
            b.Tube(V(0f, 1.4f, 0f), V(0.5f, 0.75f, 0.5f), V(90f, 0f, 0f), tint);
            b.Box(V(0f, 1.3f, 0.38f), V(0.3f, 0.04f, 0.02f), WorldPalette.Eye);
            b.Box(V(0.04f, 1.33f, 0.4f), V(0.22f, 0.14f, 0.02f), V(-20f, 0f, 8f), WorldPalette.Paper);
            b.Box(V(0.27f, 1.42f, 0.1f), V(0.03f, 0.4f, 0.03f), WorldPalette.Iron);
            b.Box(V(0.27f, 1.55f, 0.2f), V(0.03f, 0.14f, 0.2f), WorldPalette.Red);
            b.Figure.Size = V(0.9f, 1.8f, 1f);
        }

        /// <summary>A free place for a player's shop: a rug and a signpost.</summary>
        private static void ShopSpot(FigureBuilder b, Color tint)
        {
            b.Box(V(0f, 0.02f, 0f), V(1.7f, 0.04f, 1.3f), tint);
            b.Box(V(0f, 0.045f, 0f), V(1.4f, 0.02f, 1f), WorldPalette.Tone(tint, 0.8f));
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    b.Tube(V(0.82f * x, 0.1f, 0.62f * z), V(0.06f, 0.2f, 0.06f), WorldPalette.Wood);
                }
            }
            b.Tube(V(-1f, 0.5f, -0.6f), V(0.07f, 1f, 0.07f), WorldPalette.Wood);
            b.Box(V(-1f, 0.95f, -0.6f), V(0.5f, 0.3f, 0.05f), WorldPalette.WoodLight);
            b.Tube(V(-1f, 0.95f, -0.57f), V(0.16f, 0.02f, 0.16f), V(90f, 0f, 0f), WorldPalette.Gold);
            b.Figure.Size = V(1.9f, 1.1f, 1.4f);
        }
    }
}
