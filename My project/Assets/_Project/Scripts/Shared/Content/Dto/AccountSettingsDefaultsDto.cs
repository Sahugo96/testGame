using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Default account settings (02, 7.6) and the limits the server clamps to (config.AccountSettings).</summary>
    [Serializable]
    public class AccountSettingsDefaultsDto
    {
        public int SchemaVersion;
        public int MaxBindings;
        public int MaxBindingIdLength;
        public int MaxPathLength;
        public float CameraSensitivity;
        public float CameraSensitivityMin;
        public float CameraSensitivityMax;
        public float ZoomSpeed;
        public float ZoomSpeedMin;
        public float ZoomSpeedMax;
        public float UiScale;
        public float UiScaleMin;
        public float UiScaleMax;
        public float PanelOpacity;
        public float PanelOpacityMin;
        public bool InvertY;
        public bool DamageNumbers;
        public bool Nameplates;
        public bool NameplateHealth;
    }
}
