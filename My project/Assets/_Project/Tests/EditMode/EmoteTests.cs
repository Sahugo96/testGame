using System.Linq;
using Game.Network;
using Game.Shared.Core;
using Game.Shared.Protocol;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Emotes (02, 7.7 Г): a white list from the content, a rate limit, delivery to those who see the player.</summary>
    public sealed class EmoteTests
    {
        private const string Taunt = "taunt";

        private ServerHarness _h;
        private EntityId _player;

        [SetUp]
        public void SetUp()
        {
            _h = new ServerHarness();
            _player = _h.AddPlayer("warrior");
        }

        private int Emotes(EntityId observer) => _h.Player(observer).Outbox.Replies.OfType<EmoteMessage>().Count(e => e.EntityId == _player);

        [Test]
        public void Emote_GoesToThePlayerAndToThoseWhoSeeThem()
        {
            EntityId near = _h.AddPlayer("mage");
            EntityId far = _h.AddPlayer("mage");
            Vec3 at = _h.World.Transforms.Get(_player).Position;
            _h.World.Transforms.Get(near).Position = new Vec3(at.X + 3f, 0f, at.Z);
            _h.World.Transforms.Get(far).Position = new Vec3(at.X + _h.Content.Config.InterestRadius + 15f, 0f, at.Z);
            _h.Server.Tick();

            _h.Send(_player, new EmoteCommand { EmoteId = Taunt });

            Assert.That(Emotes(_player), Is.EqualTo(1));
            Assert.That(Emotes(near), Is.EqualTo(1));
            Assert.That(Emotes(far), Is.EqualTo(0), "a player out of sight is not told");
            Assert.That(_h.Player(near).Outbox.Replies.OfType<EmoteMessage>().Single().EmoteId, Is.EqualTo(Taunt));
        }

        [Test]
        public void UnknownEmote_IsIgnored()
        {
            _h.Send(_player, new EmoteCommand { EmoteId = "made_up" });
            _h.Send(_player, new EmoteCommand { EmoteId = null });

            Assert.That(Emotes(_player), Is.EqualTo(0));
            Assert.That(_h.Rejections(_player), Is.Empty);
        }

        [Test]
        public void Emotes_AreRateLimited()
        {
            _h.Send(_player, new EmoteCommand { EmoteId = Taunt });
            _h.Send(_player, new EmoteCommand { EmoteId = Taunt });

            Assert.That(Emotes(_player), Is.EqualTo(1));
            Assert.That(_h.Rejections(_player), Does.Contain(RejectReasons.EmoteTooFast));

            _h.Run(_h.Content.Config.EmoteCooldownSeconds);
            _h.Send(_player, new EmoteCommand { EmoteId = Taunt });
            Assert.That(Emotes(_player), Is.EqualTo(2));
        }

        [Test]
        public void TheDead_DoNotEmote()
        {
            _h.World.Vitals.Get(_player).IsDead = true;

            _h.Send(_player, new EmoteCommand { EmoteId = Taunt });

            Assert.That(Emotes(_player), Is.EqualTo(0));
            Assert.That(_h.Rejections(_player), Does.Contain(RejectReasons.Dead));
        }

        [Test]
        public void EmoteMessages_SurviveTheWire()
        {
            var command = (EmoteCommand)ProtocolCodec.Decode(ProtocolCodec.Encode(new EmoteCommand { EmoteId = Taunt }));
            Assert.That(command.EmoteId, Is.EqualTo(Taunt));

            var message = (EmoteMessage)ProtocolCodec.Decode(ProtocolCodec.Encode(new EmoteMessage { EntityId = _player, EmoteId = Taunt }));
            Assert.That(message.EntityId, Is.EqualTo(_player));
            Assert.That(message.EmoteId, Is.EqualTo(Taunt));
        }
    }
}
