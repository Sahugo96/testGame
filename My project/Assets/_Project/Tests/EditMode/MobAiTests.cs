using Game.Server.World;
using Game.Shared.Core;
using Game.Shared.Protocol;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Module 10: aggro, threat-driven chase, abilities from data, leash home, passive and defensive mobs.</summary>
    public sealed class MobAiTests
    {
        private ServerHarness _h;
        private EntityId _player;

        [SetUp]
        public void SetUp()
        {
            _h = new ServerHarness();
            _player = _h.AddPlayer("warrior");
            _h.Vitals(_player).HealthRegen = 0f;
        }

        private Vec3 Position(EntityId id) => _h.World.Transforms.Get(id).Position;

        private MobAiData Ai(EntityId mob) => _h.World.MobAi.Get(mob);

        [Test]
        public void Wolf_NoticesPlayerInAggroRadius_ChasesAndBites()
        {
            EntityId wolf = _h.NearestMob(_player, "wolf");
            float aggro = _h.Content.AiProfiles["wolf"].AggroRadius;
            _h.PlaceNear(_player, wolf, aggro - 1f);
            float health = _h.Vitals(_player).Health;
            float start = Vec3.DistanceXZ(Position(wolf), Position(_player));

            _h.Run(2f);

            Assert.That(Ai(wolf).State, Is.EqualTo(MobAiState.Chase));
            Assert.That(_h.Server.Threat.TopThreat(wolf), Is.EqualTo(_player));
            Assert.That(Vec3.DistanceXZ(Position(wolf), Position(_player)), Is.LessThan(start), "it came closer");
            Assert.That(_h.Vitals(_player).Health, Is.LessThan(health), "and bit");
        }

        [Test]
        public void Wolf_IgnoresPlayerOutsideAggroRadius()
        {
            EntityId wolf = _h.NearestMob(_player, "wolf");
            _h.PlaceNear(_player, wolf, _h.Content.AiProfiles["wolf"].AggroRadius + 2f);
            Vec3 before = Position(wolf);
            _h.Run(2f);
            Assert.That(Ai(wolf).State, Is.EqualTo(MobAiState.Idle));
            Assert.That(Position(wolf).X, Is.EqualTo(before.X));
        }

        [Test]
        public void Passive_Dummy_NeverFightsBack()
        {
            EntityId dummy = _h.NearestMob(_player, "training_dummy");
            _h.PlaceNear(_player, dummy, 2f);
            _h.Send(_player, new UseSkillCommand { SkillId = "power_strike", TargetId = dummy });
            float health = _h.Vitals(_player).Health;
            Vec3 before = Position(dummy);
            _h.Run(3f);
            Assert.That(Position(dummy).X, Is.EqualTo(before.X));
            Assert.That(_h.Vitals(_player).Health, Is.EqualTo(health));
        }

        [Test]
        public void Golem_IsDefensive_AttacksOnlyAfterBeingHit()
        {
            EntityId golem = _h.NearestMob(_player, "stone_golem");
            _h.PlaceNear(_player, golem, 2.5f);
            _h.Run(1f);
            Assert.That(Ai(golem).State, Is.EqualTo(MobAiState.Idle), "standing next to it is fine");

            float health = _h.Vitals(_player).Health;
            _h.Send(_player, new UseSkillCommand { SkillId = "power_strike", TargetId = golem });
            _h.Run(1.5f);
            Assert.That(Ai(golem).State, Is.EqualTo(MobAiState.Chase));
            Assert.That(_h.Vitals(_player).Health, Is.LessThan(health));
            Assert.That(_h.Server.Statuses.Has(_player, "stunned"), Is.True, "its first ability (slam) stuns");
        }

        [Test]
        public void Leash_MobReturnsHome_ForgetsThreat_HealsUp()
        {
            EntityId wolf = _h.NearestMob(_player, "wolf");
            Vec3 home = _h.World.Mobs.Get(wolf).SpawnPosition;
            _h.PlaceNear(_player, wolf, 3f);
            _h.Run(0.5f);
            Assert.That(Ai(wolf).State, Is.EqualTo(MobAiState.Chase));
            _h.Vitals(wolf).Health -= 40f;

            // The player runs far beyond the leash: the wolf follows until it is too far from home.
            float leash = _h.Content.AiProfiles["wolf"].LeashRadius;
            _h.World.Transforms.Get(_player).Position = new Vec3(home.X - leash - 10f, 0f, home.Z);
            _h.Run(leash / _h.Content.AiProfiles["wolf"].MoveSpeed + 1f);
            Assert.That(Ai(wolf).State, Is.Not.EqualTo(MobAiState.Chase));
            Assert.That(_h.Server.Threat.TopThreat(wolf).IsValid, Is.False);

            _h.Run(leash / _h.Content.AiProfiles["wolf"].MoveSpeed * 2f);
            Assert.That(Ai(wolf).State, Is.EqualTo(MobAiState.Idle));
            Assert.That(Vec3.DistanceXZ(Position(wolf), home), Is.LessThanOrEqualTo(_h.Content.Config.MobReturnArriveDistance));
            Assert.That(_h.Vitals(wolf).Health, Is.EqualTo(_h.Vitals(wolf).MaxHealth));
        }

        [Test]
        public void StunnedMob_DoesNotMoveOrAttack()
        {
            EntityId wolf = _h.NearestMob(_player, "wolf");
            _h.PlaceNear(_player, wolf, 6f);
            _h.Server.Statuses.Apply(_player, wolf, "stunned", 1f);
            Vec3 before = Position(wolf);
            float health = _h.Vitals(_player).Health;
            _h.Run(1f);
            Assert.That(Vec3.DistanceXZ(Position(wolf), before), Is.LessThan(0.001f));
            Assert.That(_h.Vitals(_player).Health, Is.EqualTo(health));
        }

        [Test]
        public void PlayerDeath_SendsTheMobHome()
        {
            EntityId wolf = _h.NearestMob(_player, "wolf");
            _h.PlaceNear(_player, wolf, 3f);
            _h.Run(0.5f);
            _h.Vitals(_player).Health = 1f;
            _h.Run(2f);
            Assert.That(_h.Vitals(_player).IsDead, Is.True);
            Assert.That(Ai(wolf).State, Is.Not.EqualTo(MobAiState.Chase));
        }

        private int Chasing(string mobId)
        {
            int count = 0;
            foreach (EntityId id in _h.World.Entities)
            {
                if (_h.World.MobAi.Has(id) && _h.World.Identities.Get(id).DefinitionId == mobId && _h.World.MobAi.Get(id).State == MobAiState.Chase)
                {
                    count++;
                }
            }
            return count;
        }

        [Test]
        public void HerdMobs_CallKin_LonersFightAlone()
        {
            // Make wolves defensive so only the one we hit reacts by itself (question 75: herd vs loners).
            _h.Content.AiProfiles["wolf"].Behavior = Game.Shared.Content.AiBehavior.Defensive;
            EntityId wolf = _h.NearestMob(_player, "wolf");
            _h.Server.Threat.Add(wolf, _player, 1f);
            _h.Run(0.1f);
            Assert.That(Chasing("wolf"), Is.GreaterThan(1), "the pack joins");

            var h2 = new ServerHarness();
            h2.Content.AiProfiles["wolf"].Behavior = Game.Shared.Content.AiBehavior.Defensive;
            h2.Content.AiProfiles["wolf"].Herd = false;
            EntityId p2 = h2.AddPlayer("warrior");
            EntityId wolf2 = h2.NearestMob(p2, "wolf");
            h2.Server.Threat.Add(wolf2, p2, 1f);
            h2.Run(0.1f);
            int alone = 0;
            foreach (EntityId id in h2.World.Entities)
            {
                if (h2.World.MobAi.Has(id) && h2.World.MobAi.Get(id).State == MobAiState.Chase)
                {
                    alone++;
                }
            }
            Assert.That(alone, Is.EqualTo(1), "a loner fights alone");
        }

        [Test]
        public void KillingAWolf_GivesExperience_FromData()
        {
            EntityId wolf = _h.NearestMob(_player, "wolf");
            _h.PlaceNear(_player, wolf, 2f);
            int xp = _h.Player(_player).Experience;
            _h.Vitals(wolf).Health = 1f;
            _h.Send(_player, new UseSkillCommand { SkillId = "power_strike", TargetId = wolf });
            Assert.That(_h.Vitals(wolf).IsDead, Is.True);
            Assert.That(_h.Player(_player).Experience, Is.EqualTo(xp + _h.Content.Mobs["wolf"].ExperienceReward));
        }
    }
}
