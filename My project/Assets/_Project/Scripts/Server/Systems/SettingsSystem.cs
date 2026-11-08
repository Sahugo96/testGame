using System;
using System.Collections.Generic;
using Game.Server.Players;
using Game.Shared.Content.Dto;
using Game.Shared.Protocol;

namespace Game.Server.Systems
{
    /// <summary>
    /// Account settings (02, 7.6; module 17): the server keeps one versioned document per account and checks it — a newer
    /// schema than the server knows is refused, the number and length of bindings are bounded, numbers are clamped to the
    /// config limits. The last change wins. The client gets the document after login and after every change.
    /// </summary>
    public sealed class SettingsSystem
    {
        private readonly ServerContext _ctx;

        public SettingsSystem(ServerContext ctx) => _ctx = ctx;

        /// <summary>The account's settings changed (persistence saves the account).</summary>
        public event Action<PlayerCharacter> Changed;

        public AccountSettingsSnapshot Defaults()
        {
            AccountSettingsDefaultsDto d = _ctx.Content.Config.AccountSettings;
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

        public AccountSettingsSnapshot Of(PlayerCharacter player) => player.AccountSettings ?? Defaults();

        public void Set(PlayerCharacter player, SetAccountSettingsCommand command)
        {
            AccountSettingsSnapshot clean = Clean(command.Settings);
            if (clean == null)
            {
                CombatEvents.Reject(player, RejectReasons.SettingsInvalid);
                return;
            }
            player.AccountSettings = clean;
            player.SettingsToSend = true;
            Changed?.Invoke(player);
        }

        /// <summary>Settings read from the account record at login (already cleaned); null keeps the defaults.</summary>
        public void Load(PlayerCharacter player, AccountSettingsSnapshot saved)
        {
            player.AccountSettings = saved != null ? Clean(saved) : null;
            player.SettingsToSend = true;
        }

        /// <summary>Queues the document for players whose settings were loaded or changed (once).</summary>
        public void Flush(PlayerCharacter player)
        {
            if (!player.SettingsToSend)
            {
                return;
            }
            player.SettingsToSend = false;
            player.Outbox.Replies.Add(new AccountSettingsMessage { Settings = Of(player) });
        }

        /// <summary>A checked copy, or null when the document cannot be accepted (missing, or from a newer schema).</summary>
        public AccountSettingsSnapshot Clean(AccountSettingsSnapshot settings)
        {
            AccountSettingsDefaultsDto d = _ctx.Content.Config.AccountSettings;
            if (settings == null || settings.SchemaVersion <= 0 || settings.SchemaVersion > d.SchemaVersion)
            {
                return null;
            }
            var bindings = new List<KeyBindingSnapshot>();
            var seen = new HashSet<string>();
            foreach (KeyBindingSnapshot binding in settings.Bindings ?? new KeyBindingSnapshot[0])
            {
                string id = binding.BindingId ?? string.Empty;
                string path = binding.Path ?? string.Empty;
                if (id.Length == 0 || id.Length > d.MaxBindingIdLength || path.Length > d.MaxPathLength || HasControl(id) || HasControl(path) || !seen.Add(id))
                {
                    return null;
                }
                bindings.Add(new KeyBindingSnapshot { BindingId = id, Path = path });
            }
            if (bindings.Count > d.MaxBindings)
            {
                return null;
            }
            return new AccountSettingsSnapshot
            {
                SchemaVersion = d.SchemaVersion,
                Bindings = bindings.ToArray(),
                CameraSensitivity = Clamp(settings.CameraSensitivity, d.CameraSensitivityMin, d.CameraSensitivityMax, d.CameraSensitivity),
                InvertY = settings.InvertY,
                ZoomSpeed = Clamp(settings.ZoomSpeed, d.ZoomSpeedMin, d.ZoomSpeedMax, d.ZoomSpeed),
                UiScale = Clamp(settings.UiScale, d.UiScaleMin, d.UiScaleMax, d.UiScale),
                PanelOpacity = Clamp(settings.PanelOpacity, d.PanelOpacityMin, 1f, d.PanelOpacity),
                DamageNumbers = settings.DamageNumbers,
                Nameplates = settings.Nameplates,
                NameplateHealth = settings.NameplateHealth,
            };
        }

        private static float Clamp(float value, float min, float max, float fallback) =>
            float.IsNaN(value) || float.IsInfinity(value) ? fallback : Math.Max(min, Math.Min(max, value));

        private static bool HasControl(string text)
        {
            foreach (char c in text)
            {
                if (char.IsControl(c))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
