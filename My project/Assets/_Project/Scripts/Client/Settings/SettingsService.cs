using System;
using Game.Client.Input;
using Game.Shared.Content.Dto;
using Game.Shared.Protocol;

namespace Game.Client.Settings
{
    /// <summary>
    /// User settings of the client (02, 7.6; module 17). Device settings (graphics, FPS counter) are applied at once and
    /// saved to a local file; account settings (keys, camera, interface) come from the server after login and after every
    /// change — the player's change is applied at once and sent, the server's answer is applied again (the last change
    /// wins). Views subscribe to <see cref="AccountApplied"/> / <see cref="LocalApplied"/>; nothing here is game logic.
    /// </summary>
    public sealed class SettingsService
    {
        private readonly GameConfig _config;
        private readonly LocalSettingsStore _store;
        private Func<ClientCommands> _commands;

        private readonly Localization.ILanguageService _languages;

        public SettingsService(GameConfig config, LocalSettingsStore store, KeyRebinder keys, Localization.ILanguageService languages)
        {
            _config = config;
            _languages = languages;
            _store = store;
            Keys = keys;
            Account = DefaultAccount();
        }

        public KeyRebinder Keys { get; }
        public LocalSettings Local { get; private set; }
        public AccountSettingsSnapshot Account { get; private set; }

        public event Action<LocalSettings> LocalApplied;
        public event Action<AccountSettingsSnapshot> AccountApplied;

        /// <summary>Where commands go once the player is in the world (null in the start menu).</summary>
        public void Bind(Func<ClientCommands> commands) => _commands = commands;

        /// <summary>Start of the game: the saved device settings, or detected ones on the first start.</summary>
        public void LoadLocal()
        {
            LocalSettings saved = _store.Load();
            if (saved == null)
            {
                saved = GraphicsSettingsApplier.Detect(_config);
                _store.Save(saved);
            }
            ApplyLocal(saved, false);
        }

        public void ApplyLocal(LocalSettings settings, bool save)
        {
            Local = settings.Copy();
            GraphicsSettingsApplier.Apply(Local, _config);
            if (!string.IsNullOrEmpty(Local.Language) && _languages != null && !_languages.Select(Local.Language))
            {
                Local.Language = _languages.Current;
            }
            if (save)
            {
                _store.Save(Local);
            }
            LocalApplied?.Invoke(Local);
        }

        public LocalSettings DefaultLocal() => GraphicsSettingsApplier.Detect(_config);

        /// <summary>The server's document (after login or as the echo of a change).</summary>
        public void ApplyAccount(AccountSettingsSnapshot settings)
        {
            Account = settings ?? DefaultAccount();
            Keys.Apply(Account.Bindings);
            AccountApplied?.Invoke(Account);
        }

        /// <summary>The player pressed "Apply": use it now and send it to the account.</summary>
        public void SaveAccount(AccountSettingsSnapshot settings)
        {
            settings.SchemaVersion = _config.AccountSettings.SchemaVersion;
            settings.Bindings = Keys.Export();
            Account = settings;
            AccountApplied?.Invoke(Account);
            _commands?.Invoke()?.SetAccountSettings(settings);
        }

        public AccountSettingsSnapshot DefaultAccount()
        {
            AccountSettingsDefaultsDto d = _config.AccountSettings;
            return new AccountSettingsSnapshot
            {
                SchemaVersion = d.SchemaVersion,
                CameraSensitivity = d.CameraSensitivity,
                InvertY = d.InvertY,
                ZoomSpeed = d.ZoomSpeed,
                UiScale = d.UiScale,
                PanelOpacity = d.PanelOpacity,
                DamageNumbers = d.DamageNumbers,
                Nameplates = d.Nameplates,
                NameplateHealth = d.NameplateHealth,
            };
        }

        /// <summary>A copy to edit in the settings window.</summary>
        public static AccountSettingsSnapshot Copy(AccountSettingsSnapshot s) => new AccountSettingsSnapshot
        {
            SchemaVersion = s.SchemaVersion,
            Bindings = (KeyBindingSnapshot[])(s.Bindings ?? new KeyBindingSnapshot[0]).Clone(),
            CameraSensitivity = s.CameraSensitivity,
            InvertY = s.InvertY,
            ZoomSpeed = s.ZoomSpeed,
            UiScale = s.UiScale,
            PanelOpacity = s.PanelOpacity,
            DamageNumbers = s.DamageNumbers,
            Nameplates = s.Nameplates,
            NameplateHealth = s.NameplateHealth,
        };
    }
}
