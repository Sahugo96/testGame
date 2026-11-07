using UnityEngine;

namespace Game.Client.World
{
    /// <summary>Placeholder visuals until art arrives (primitives with colors).</summary>
    public static class WorldPalette
    {
        public static readonly Color LocalPlayer = new Color(0.25f, 0.55f, 1f);
        public static readonly Color OtherPlayer = new Color(0.3f, 0.85f, 0.45f);
        public static readonly Color DefaultMob = new Color(0.8f, 0.3f, 0.3f);
        public static readonly Color Dead = new Color(0.35f, 0.35f, 0.35f);
        public static readonly Color Ground = new Color(0.33f, 0.45f, 0.3f);
        public static readonly Color TargetMarker = new Color(1f, 0.85f, 0.2f);
        public static readonly Color DuelArena = new Color(0.9f, 0.2f, 0.2f);
        public static readonly Color DuelArenaCountdown = new Color(1f, 0.75f, 0.2f);
    }
}
