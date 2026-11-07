using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>HUD colors. Health, main and reserve mana have distinct colors (02, 7.7 А).</summary>
    public static class HudPalette
    {
        public static readonly Color PanelBackground = new Color(0.08f, 0.08f, 0.1f, 0.85f);
        public static readonly Color WindowHeader = new Color(0.18f, 0.16f, 0.12f, 0.95f);
        public static readonly Color SlotBackground = new Color(0.15f, 0.15f, 0.18f, 0.9f);
        public static readonly Color SlotFilled = new Color(0.25f, 0.24f, 0.3f, 0.95f);
        public static readonly Color BarBackground = new Color(0f, 0f, 0f, 0.6f);
        public static readonly Color Health = new Color(0.8f, 0.15f, 0.15f);
        public static readonly Color MainMana = new Color(0.2f, 0.45f, 0.95f);
        public static readonly Color ReserveMana = new Color(0.6f, 0.3f, 0.9f);
        public static readonly Color Cast = new Color(0.95f, 0.75f, 0.2f);
        public static readonly Color CooldownOverlay = new Color(0f, 0f, 0f, 0.65f);
        public static readonly Color Text = new Color(0.95f, 0.93f, 0.88f);
        public static readonly Color TextDim = new Color(0.65f, 0.63f, 0.6f);
        public static readonly Color Warning = new Color(1f, 0.55f, 0.2f);
        public static readonly Color Error = new Color(1f, 0.35f, 0.3f);
        public static readonly Color Damage = new Color(1f, 0.9f, 0.3f);
        public static readonly Color Heal = new Color(0.4f, 1f, 0.45f);
        public static readonly Color Restore = new Color(0.45f, 0.7f, 1f);
        public static readonly Color Button = new Color(0.22f, 0.2f, 0.17f, 0.95f);
        public static readonly Color Minimap = new Color(0f, 0f, 0f, 0.8f);
        public static readonly Color Experience = new Color(0.45f, 0.3f, 0.85f);
        public static readonly Color Positive = new Color(0.55f, 1f, 0.55f);
        public static readonly Color Backdrop = new Color(0f, 0f, 0f, 0.75f);
        public static readonly Color StatusBuff = new Color(0.12f, 0.35f, 0.16f, 0.9f);
        public static readonly Color StatusDebuff = new Color(0.45f, 0.12f, 0.1f, 0.9f);

        public static Color ForRarity(Game.Shared.Content.Rarity rarity)
        {
            switch (rarity)
            {
                case Game.Shared.Content.Rarity.Uncommon:
                    return new Color(0.35f, 0.9f, 0.35f);
                case Game.Shared.Content.Rarity.Rare:
                    return new Color(0.3f, 0.55f, 1f);
                case Game.Shared.Content.Rarity.Mythic:
                    return new Color(0.75f, 0.35f, 1f);
                case Game.Shared.Content.Rarity.Legendary:
                    return new Color(1f, 0.6f, 0.15f);
                case Game.Shared.Content.Rarity.Divine:
                    return new Color(1f, 0.9f, 0.4f);
                default:
                    return Text;
            }
        }
    }
}
