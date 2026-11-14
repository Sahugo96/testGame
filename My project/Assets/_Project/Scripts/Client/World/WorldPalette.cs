using UnityEngine;

namespace Game.Client.World
{
    /// <summary>Colours of the world's look: figures assembled from primitives (until real models arrive), ground, scenery.</summary>
    public static class WorldPalette
    {
        // Materials of the figures.
        public static readonly Color Skin = new Color(0.93f, 0.76f, 0.62f);
        public static readonly Color Hair = new Color(0.32f, 0.2f, 0.12f);
        public static readonly Color Eye = new Color(0.08f, 0.07f, 0.09f);
        public static readonly Color Trousers = new Color(0.3f, 0.27f, 0.25f);
        public static readonly Color Boots = new Color(0.24f, 0.16f, 0.1f);
        public static readonly Color Leather = new Color(0.56f, 0.38f, 0.22f);
        public static readonly Color Wood = new Color(0.55f, 0.37f, 0.2f);
        public static readonly Color WoodDark = new Color(0.36f, 0.24f, 0.13f);
        public static readonly Color WoodLight = new Color(0.72f, 0.54f, 0.33f);
        public static readonly Color Straw = new Color(0.87f, 0.75f, 0.42f);
        public static readonly Color Iron = new Color(0.36f, 0.38f, 0.42f);
        public static readonly Color Steel = new Color(0.72f, 0.76f, 0.82f);
        public static readonly Color Gold = new Color(0.95f, 0.76f, 0.25f);
        public static readonly Color Stone = new Color(0.55f, 0.55f, 0.56f);
        public static readonly Color Moss = new Color(0.33f, 0.5f, 0.24f);
        public static readonly Color Cream = new Color(0.96f, 0.93f, 0.85f);
        public static readonly Color Paper = new Color(0.98f, 0.96f, 0.9f);
        public static readonly Color Red = new Color(0.8f, 0.2f, 0.18f);
        public static readonly Color Water = new Color(0.35f, 0.62f, 0.85f);
        public static readonly Color MagicGlow = new Color(0.55f, 0.85f, 1f);
        public static readonly Color BeastEye = new Color(1f, 0.85f, 0.25f);
        public static readonly Color GolemCore = new Color(1f, 0.62f, 0.2f);
        public static readonly Color GoodsRed = new Color(0.85f, 0.25f, 0.22f);
        public static readonly Color GoodsGreen = new Color(0.45f, 0.72f, 0.3f);
        public static readonly Color GoodsBlue = new Color(0.3f, 0.5f, 0.85f);

        // Ground and scenery.
        public static readonly Color GrassDark = new Color(0.24f, 0.4f, 0.2f);
        public static readonly Color GrassLight = new Color(0.44f, 0.6f, 0.3f);
        public static readonly Color Outskirts = new Color(0.31f, 0.47f, 0.24f);
        public static readonly Color Sand = new Color(0.72f, 0.63f, 0.46f);
        public static readonly Color Trunk = new Color(0.4f, 0.28f, 0.17f);
        public static readonly Color LeafDark = new Color(0.17f, 0.4f, 0.2f);
        public static readonly Color LeafLight = new Color(0.32f, 0.56f, 0.24f);
        public static readonly Color Pine = new Color(0.13f, 0.33f, 0.24f);
        public static readonly Color Rock = new Color(0.5f, 0.5f, 0.52f);
        public static readonly Color FlowerYellow = new Color(0.98f, 0.85f, 0.3f);
        public static readonly Color FlowerWhite = new Color(0.96f, 0.96f, 0.98f);
        public static readonly Color FlowerPink = new Color(0.93f, 0.5f, 0.68f);

        /// <summary>The same colour darker (factor below 1) or lighter (above 1).</summary>
        public static Color Tone(Color color, float factor) =>
            new Color(Mathf.Clamp01(color.r * factor), Mathf.Clamp01(color.g * factor), Mathf.Clamp01(color.b * factor), color.a);

        public static readonly Color LocalPlayer = new Color(0.25f, 0.55f, 1f);
        public static readonly Color OtherPlayer = new Color(0.3f, 0.85f, 0.45f);
        public static readonly Color DefaultMob = new Color(0.8f, 0.3f, 0.3f);
        public static readonly Color Dead = new Color(0.35f, 0.35f, 0.35f);
        public static readonly Color TargetMarker = new Color(1f, 0.85f, 0.2f);
        public static readonly Color DuelArena = new Color(0.9f, 0.2f, 0.2f);
        public static readonly Color DuelArenaCountdown = new Color(1f, 0.75f, 0.2f);
    }
}
