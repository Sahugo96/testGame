using System.Linq;
using Game.Network;
using Game.Shared.Core;
using Game.Shared.Protocol;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Duels (question 70): request, countdown, hostility only between opponents, non-lethal end, arena, limits.</summary>
    public sealed class DuelTests
    {
        private ServerHarness _h;
        private EntityId _mage;
        private EntityId _warrior;

        [SetUp]
        public void SetUp()
        {
            _h = new ServerHarness();
            _mage = _h.AddPlayer("mage");
            _warrior = _h.AddPlayer("warrior");
            _h.Vitals(_warrior).HealthRegen = 0f;
        }

        private float Countdown => _h.Content.Config.DuelCountdownSeconds;

        private DuelSnapshot Duel(EntityId id) => _h.Player(id).Outbox.State.Duel;

        private bool Notified(EntityId id, string key) => _h.Player(id).Outbox.Notifications.Any(n => n.TextKey == key);

        private void StartDuel()
        {
            _h.Send(_mage, new DuelRequestCommand { TargetId = _warrior });
            _h.Send(_warrior, new DuelResponseCommand { Accept = true });
            _h.Run(Countdown + 0.1f);
            Assert.That(Duel(_mage).Phase, Is.EqualTo(DuelPhase.Active));
        }

        private void Nova(EntityId caster, EntityId target) =>
            _h.Send(caster, new UseSkillCommand { SkillId = "reserve_nova", TargetId = target });

        [Test]
        public void PlayersAreNotHostile_WithoutDuel_AndDuringCountdown()
        {
            Nova(_mage, _warrior);
            Assert.That(_h.Rejections(_mage), Does.Contain(RejectReasons.InvalidTarget));

            _h.Send(_mage, new DuelRequestCommand { TargetId = _warrior });
            Assert.That(Duel(_warrior).Phase, Is.EqualTo(DuelPhase.IncomingRequest));
            Assert.That(Duel(_mage).Phase, Is.EqualTo(DuelPhase.OutgoingRequest));
            Assert.That(Notified(_warrior, NotificationKeys.DuelRequested), Is.True);

            _h.Send(_warrior, new DuelResponseCommand { Accept = true });
            Assert.That(Duel(_mage).Phase, Is.EqualTo(DuelPhase.Countdown));
            _h.ClearOutbox(_mage);
            Nova(_mage, _warrior);
            Assert.That(_h.Rejections(_mage), Does.Contain(RejectReasons.InvalidTarget), "no hits during the countdown");
        }

        [Test]
        public void ActiveDuel_OpponentsCanHitEachOther_OutsidersCannot()
        {
            EntityId outsider = _h.AddPlayer("mage");
            StartDuel();
            float health = _h.Vitals(_warrior).Health;

            Nova(_mage, _warrior);
            Assert.That(_h.Vitals(_warrior).Health, Is.LessThan(health));

            Nova(outsider, _warrior);
            Assert.That(_h.Rejections(outsider), Does.Contain(RejectReasons.InvalidTarget));
        }

        [Test]
        public void OutsiderHeal_OnDuelist_FallsBackToSelf()
        {
            EntityId outsider = _h.AddPlayer("mage");
            StartDuel();
            _h.Vitals(_warrior).Health = 50f;

            _h.Send(outsider, new UseSkillCommand { SkillId = "arcane_mend", TargetId = _warrior });
            Assert.That(_h.World.Casts.Get(outsider).TargetId, Is.EqualTo(outsider));
            _h.Run(1.1f);
            Assert.That(_h.Vitals(_warrior).Health, Is.EqualTo(50f));
        }

        [Test]
        public void LethalHitFromOpponent_LeavesLoserAlive_AndEndsDuel()
        {
            StartDuel();
            _h.Vitals(_warrior).Health = 1f;

            Nova(_mage, _warrior);

            Assert.That(_h.Vitals(_warrior).IsDead, Is.False);
            Assert.That(_h.Vitals(_warrior).Health, Is.EqualTo(_h.Content.Config.DuelLoserHealth));
            Assert.That(Notified(_mage, NotificationKeys.DuelWon), Is.True);
            Assert.That(Notified(_warrior, NotificationKeys.DuelLost), Is.True);
            Assert.That(Duel(_mage).Phase, Is.EqualTo(DuelPhase.None));
            _h.ClearOutbox(_mage);
            _h.Run(_h.Content.Skills["reserve_nova"].Default.Cooldown);
            Nova(_mage, _warrior);
            Assert.That(_h.Rejections(_mage), Does.Contain(RejectReasons.InvalidTarget), "hostility ends with the duel");
        }

        [Test]
        public void LeavingTheArena_Loses()
        {
            StartDuel();
            DuelSnapshot duel = Duel(_warrior);
            _h.World.Transforms.Get(_warrior).Position = new Vec3(duel.CenterX + duel.Radius + 1f, 0f, duel.CenterZ);
            _h.Server.Tick();

            Assert.That(Notified(_warrior, NotificationKeys.DuelLostLeftArea), Is.True);
            Assert.That(Notified(_mage, NotificationKeys.DuelWon), Is.True);
        }

        [Test]
        public void TimeLimit_IsDraw()
        {
            StartDuel();
            _h.Run(_h.Content.Config.DuelMaxSeconds + 0.1f);
            Assert.That(Notified(_mage, NotificationKeys.DuelDraw), Is.True);
            Assert.That(Notified(_warrior, NotificationKeys.DuelDraw), Is.True);
        }

        [Test]
        public void Decline_And_Expire()
        {
            _h.Send(_mage, new DuelRequestCommand { TargetId = _warrior });
            _h.Send(_warrior, new DuelResponseCommand { Accept = false });
            Assert.That(Notified(_mage, NotificationKeys.DuelDeclined), Is.True);
            Assert.That(Duel(_mage).Phase, Is.EqualTo(DuelPhase.None));

            _h.Send(_mage, new DuelRequestCommand { TargetId = _warrior });
            _h.Run(_h.Content.Config.DuelRequestSeconds + 0.1f);
            Assert.That(Notified(_warrior, NotificationKeys.DuelExpired), Is.True);
            Assert.That(Duel(_warrior).Phase, Is.EqualTo(DuelPhase.None));
        }

        [Test]
        public void Request_IsValidated()
        {
            EntityId third = _h.AddPlayer("mage");
            _h.Send(_mage, new DuelRequestCommand { TargetId = _mage });
            _h.Send(_mage, new DuelRequestCommand { TargetId = _h.FindMob("training_dummy") });
            Assert.That(_h.Rejections(_mage).Count(r => r == RejectReasons.DuelNotPlayer), Is.EqualTo(2));

            _h.World.Transforms.Get(third).Position = new Vec3(_h.Content.Config.DuelRequestRange + 5f, 0f, 0f);
            _h.Send(_mage, new DuelRequestCommand { TargetId = third });
            Assert.That(_h.Rejections(_mage), Does.Contain(RejectReasons.OutOfRange));

            _h.Send(_mage, new DuelRequestCommand { TargetId = _warrior });
            _h.World.Transforms.Get(third).Position = Vec3.Zero;
            _h.Send(third, new DuelRequestCommand { TargetId = _warrior });
            Assert.That(_h.Rejections(third), Does.Contain(RejectReasons.DuelTargetBusy));
            _h.Send(_mage, new DuelRequestCommand { TargetId = third });
            Assert.That(_h.Rejections(_mage), Does.Contain(RejectReasons.DuelBusy));
        }

        [Test]
        public void Request_InCombat_IsRejected()
        {
            EntityId dummy = _h.NearestMob(_mage, "training_dummy");
            _h.PlaceNear(_mage, dummy, 10f);
            _h.World.Transforms.Get(_warrior).Position = _h.World.Transforms.Get(_mage).Position;
            Nova(_mage, dummy);
            _h.Send(_mage, new DuelRequestCommand { TargetId = _warrior });
            Assert.That(_h.Rejections(_mage), Does.Contain(RejectReasons.DuelInCombat));
        }

        [Test]
        public void Surrender_And_Disconnect_GiveTheWinToTheOpponent()
        {
            StartDuel();
            _h.Send(_warrior, new DuelSurrenderCommand());
            Assert.That(Notified(_mage, NotificationKeys.DuelWon), Is.True);
            Assert.That(Duel(_mage).Phase, Is.EqualTo(DuelPhase.None));

            _h.Player(_mage).Outbox.Notifications.Clear();
            _h.Run(_h.Content.Config.CombatExitSeconds + 0.5f);
            StartDuel();
            _h.Server.RemovePlayer(_warrior);
            _h.Server.Tick();
            Assert.That(Notified(_mage, NotificationKeys.DuelWon), Is.True);
            Assert.That(Duel(_mage).Phase, Is.EqualTo(DuelPhase.None));
        }

        [Test]
        public void Codec_RoundTripsDuelCommandsAndState()
        {
            StartDuel();
            var request = (DuelRequestCommand)ProtocolCodec.Decode(ProtocolCodec.Encode(new DuelRequestCommand { TargetId = _warrior }));
            var response = (DuelResponseCommand)ProtocolCodec.Decode(ProtocolCodec.Encode(new DuelResponseCommand { Accept = true }));
            Assert.That(ProtocolCodec.Decode(ProtocolCodec.Encode(new DuelSurrenderCommand())), Is.TypeOf<DuelSurrenderCommand>());
            var state = (PlayerStateSnapshot)ProtocolCodec.Decode(ProtocolCodec.Encode(_h.Player(_mage).Outbox.State));

            Assert.That(request.TargetId, Is.EqualTo(_warrior));
            Assert.That(response.Accept, Is.True);
            Assert.That(state.Duel.Phase, Is.EqualTo(DuelPhase.Active));
            Assert.That(state.Duel.OpponentId, Is.EqualTo(_warrior));
            Assert.That(state.Duel.Radius, Is.EqualTo(_h.Content.Config.DuelRadius));
        }
    }
}
