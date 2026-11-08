using System.Linq;
using Game.Network;
using Game.Shared.Core;
using Game.Shared.Protocol;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Module 17: account settings are a versioned document the server checks, sends after login and after a change.</summary>
    public sealed class SettingsTests
    {
        private ServerHarness _h;

        [SetUp]
        public void SetUp() => _h = new ServerHarness();

        private AccountSettingsSnapshot LastSent(EntityId player) =>
            _h.Player(player).Outbox.Replies.OfType<AccountSettingsMessage>().LastOrDefault()?.Settings;

        private AccountSettingsSnapshot Valid() => new AccountSettingsSnapshot
        {
            SchemaVersion = _h.Content.Config.AccountSettings.SchemaVersion,
            Bindings = new[] { new KeyBindingSnapshot { BindingId = "SkillSlot1/0", Path = "<Keyboard>/q" } },
            CameraSensitivity = 1.5f,
            InvertY = true,
            ZoomSpeed = 1f,
            UiScale = 1.25f,
            PanelOpacity = 0.8f,
            DamageNumbers = false,
            Nameplates = true,
            NameplateHealth = false,
        };

        [Test]
        public void Defaults_AreSentOnce_ThenChangesAreCheckedAndEchoed()
        {
            EntityId a = _h.AddPlayer("mage");
            _h.Run(_h.Server.TickInterval * 2f);
            Assert.That(_h.Player(a).Outbox.Replies.OfType<AccountSettingsMessage>().Count(), Is.EqualTo(1), "once after joining");
            Assert.That(LastSent(a).UiScale, Is.EqualTo(_h.Content.Config.AccountSettings.UiScale));

            AccountSettingsSnapshot wanted = Valid();
            wanted.UiScale = 99f;
            wanted.CameraSensitivity = float.NaN;
            _h.Send(a, new SetAccountSettingsCommand { Settings = wanted });
            AccountSettingsSnapshot echoed = LastSent(a);
            Assert.That(echoed.UiScale, Is.EqualTo(_h.Content.Config.AccountSettings.UiScaleMax), "clamped to the config limit");
            Assert.That(echoed.CameraSensitivity, Is.EqualTo(_h.Content.Config.AccountSettings.CameraSensitivity), "NaN becomes the default");
            Assert.That(echoed.Bindings.Single().Path, Is.EqualTo("<Keyboard>/q"));
            Assert.That((echoed.InvertY, echoed.DamageNumbers, echoed.NameplateHealth), Is.EqualTo((true, false, false)));

            var limits = _h.Content.Config.AccountSettings;
            AccountSettingsSnapshot newer = Valid();
            newer.SchemaVersion = limits.SchemaVersion + 1;
            AccountSettingsSnapshot tooMany = Valid();
            tooMany.Bindings = Enumerable.Range(0, limits.MaxBindings + 1).Select(i => new KeyBindingSnapshot { BindingId = "A/" + i, Path = "<Keyboard>/a" }).ToArray();
            AccountSettingsSnapshot longPath = Valid();
            longPath.Bindings = new[] { new KeyBindingSnapshot { BindingId = "A/0", Path = new string('x', limits.MaxPathLength + 1) } };
            AccountSettingsSnapshot twice = Valid();
            twice.Bindings = new[] { new KeyBindingSnapshot { BindingId = "A/0", Path = "<Keyboard>/a" }, new KeyBindingSnapshot { BindingId = "A/0", Path = "<Keyboard>/b" } };
            foreach (AccountSettingsSnapshot bad in new[] { newer, tooMany, longPath, twice, null })
            {
                _h.ClearOutbox(a);
                _h.Send(a, new SetAccountSettingsCommand { Settings = bad });
                Assert.That(_h.Rejections(a), Does.Contain(RejectReasons.SettingsInvalid));
            }
            Assert.That(LastSent(a).UiScale, Is.EqualTo(limits.UiScaleMax), "a refused document changes nothing");
        }

        [Test]
        public void Codec_RoundTripsSettings()
        {
            var message = (AccountSettingsMessage)ProtocolCodec.Decode(ProtocolCodec.Encode(new AccountSettingsMessage { Settings = Valid() }));
            Assert.That((message.Settings.UiScale, message.Settings.InvertY, message.Settings.Bindings.Single().BindingId), Is.EqualTo((1.25f, true, "SkillSlot1/0")));
            var command = (SetAccountSettingsCommand)ProtocolCodec.Decode(ProtocolCodec.Encode(new SetAccountSettingsCommand { Settings = Valid() }));
            Assert.That(command.Settings.PanelOpacity, Is.EqualTo(0.8f));
        }
    }
}
