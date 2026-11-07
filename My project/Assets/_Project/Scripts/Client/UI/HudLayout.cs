using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>HUD sizes in reference pixels (CanvasScaler 1920×1080, 02 7.7: anchors + Canvas Scaler for all resolutions).</summary>
    public static class HudLayout
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        public const float ScalerMatch = 0.5f;
        public const float Margin = 12f;
        public const float Gap = 4f;
        public const int FontSmall = 13;
        public const int FontNormal = 16;
        public const int FontLarge = 20;
        public const int FontHuge = 28;

        public static readonly Vector2 PlayerFrameSize = new Vector2(340f, 118f);
        public static readonly Vector2 TargetFrameSize = new Vector2(300f, 58f);
        public const float BarHeight = 20f;
        public const float CastBarHeight = 14f;

        public const float SkillSlotSize = 52f;
        public const float ConsumableSlotSize = 44f;
        public const float MenuButtonWidth = 118f;
        public const float MenuButtonHeight = 36f;
        public const float MinimapSize = 220f;
        public const float MinimapWorldSize = 50f;
        public const float MinimapCameraHeight = 60f;
        public const int MinimapTextureSize = 256;

        public static readonly Vector2 WindowSize = new Vector2(460f, 520f);
        public const float WindowHeaderHeight = 32f;
        public const float BagSlotSize = 64f;
        public const int BagColumns = 6;
        public const float RowHeight = 24f;
        public const float TooltipWidth = 300f;
        public const float TooltipOffset = 18f;

        public const float MessageSeconds = 2.5f;
        public const float CombatTextSeconds = 1.2f;
        public const float CombatTextRise = 60f;
        public const float NameplateHeight = 2.4f;
        public static readonly Vector2 NameplateSize = new Vector2(120f, 10f);
    }
}
