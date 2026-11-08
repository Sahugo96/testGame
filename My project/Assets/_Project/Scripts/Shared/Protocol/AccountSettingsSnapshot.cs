namespace Game.Shared.Protocol
{
    /// <summary>
    /// Settings stored on the server per account (02, 7.6; question 11): controls, interface and game. A versioned
    /// document: SchemaVersion, bounded size (binding count and lengths), every value checked and clamped by the server
    /// against config limits. The last change wins (temporarily, DEFAULT). Graphics stay on the device.
    /// </summary>
    public sealed class AccountSettingsSnapshot
    {
        public int SchemaVersion;
        /// <summary>Only the bindings the player changed (overrides of the built-in keys).</summary>
        public KeyBindingSnapshot[] Bindings = new KeyBindingSnapshot[0];
        public float CameraSensitivity;
        public bool InvertY;
        public float ZoomSpeed;
        public float UiScale;
        public float PanelOpacity;
        public bool DamageNumbers;
        public bool Nameplates;
        public bool NameplateHealth;
    }

    /// <summary>One changed key: which binding (action name and binding index, "Action/2") and the control path ("&lt;Keyboard&gt;/q"; empty = unbound).</summary>
    public struct KeyBindingSnapshot
    {
        public string BindingId;
        public string Path;
    }

    public struct SetAccountSettingsCommand : IGameCommand
    {
        public AccountSettingsSnapshot Settings;
    }

    /// <summary>The account's settings as the server keeps them: sent after login and after every change.</summary>
    public sealed class AccountSettingsMessage
    {
        public AccountSettingsSnapshot Settings = new AccountSettingsSnapshot();
    }
}
