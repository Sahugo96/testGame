using System.Linq;
using Game.Network;
using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;
using Game.Shared.Rules;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Module 8 (03, 7.12): statuses apply, stack and go away by data rules; DoT/HoT, shields, control, potions, threat.</summary>
    public sealed class StatusEffectTests
    {
        private ServerHarness _h;
        private EntityId _mage;
        private EntityId _dummy;

        [SetUp]
        public void SetUp()
        {
            _h = new ServerHarness();
            _mage = _h.AddPlayer("mage");
            _dummy = _h.NearestMob(_mage, "training_dummy");
            _h.Vitals(_dummy).HealthRegen = 0f;
        }

        private Game.Server.Systems.StatusSystem Statuses => _h.Server.Statuses;

        private bool Apply(EntityId target, string statusId) => Statuses.Apply(_mage, target, statusId, 1f);

        [Test]
        public void DoT_TicksThroughCombat_AndStacksUpToMax()
        {
            Assert.That(Apply(_dummy, "burning"), Is.True);
            Apply(_dummy, "burning");
            Apply(_dummy, "burning");
            Apply(_dummy, "burning");
            Assert.That(Statuses.Find(_dummy, "burning").Stacks, Is.EqualTo(3), "MaxStacks 3");
            _h.Vitals(_dummy).HealthRegen = 0f; // the status change recalculated the mob from its data

            float health = _h.Vitals(_dummy).Health;
            StatusEffectDefinition burning = _h.Content.StatusEffects["burning"];
            float defense = _h.Vitals(_dummy).Defense;
            float bonus = _h.Player(_mage).Stats.Get(StatId.DamageBonus);
            int perTick = (int)System.Math.Round(burning.TickAmount * 3 * (1f + bonus) * (1f - defense), System.MidpointRounding.AwayFromZero);
            _h.Run(burning.TickInterval + 0.01f);
            Assert.That(health - _h.Vitals(_dummy).Health, Is.EqualTo(perTick).Within(0.01f), "one tick: TickAmount × stacks, then defense");

            _h.Run(burning.Duration);
            Assert.That(Statuses.Has(_dummy, "burning"), Is.False, "expired");
        }

        [Test]
        public void HoT_HealsEveryTick()
        {
            _h.Vitals(_mage).Health -= 50f;
            _h.Vitals(_mage).HealthRegen = 0f;
            float health = _h.Vitals(_mage).Health;
            Apply(_mage, "second_wind_regen");
            _h.Run(_h.Content.StatusEffects["second_wind_regen"].TickInterval + 0.01f);
            Assert.That(_h.Vitals(_mage).Health, Is.GreaterThan(health));
        }

        [Test]
        public void Shield_AbsorbsDamageAfterDefense_ThenBreaks()
        {
            EntityId warrior = _h.AddPlayer("warrior");
            Assert.That(Statuses.Apply(warrior, _mage, "arcane_ward", 1f), Is.True);
            float health = _h.Vitals(_mage).Health;

            _h.Server.Context.Players[warrior].TargetId = EntityId.None;
            Game.Server.Systems.CombatSystem combat = _h.Server.Combat;
            combat.DealDamage(warrior, _mage, null, 30f, 1f);
            Assert.That(_h.Vitals(_mage).Health, Is.EqualTo(health), "30 of 40 absorbed");
            Assert.That(Statuses.Find(_mage, "arcane_ward").ShieldLeft, Is.LessThan(40f));

            combat.DealDamage(warrior, _mage, null, 30f, 1f);
            Assert.That(_h.Vitals(_mage).Health, Is.LessThan(health), "the rest gets through");
            Assert.That(Statuses.Has(_mage, "arcane_ward"), Is.False, "an empty shield is removed");
        }

        [Test]
        public void Stun_StopsMovingAndSkills_AndInterruptsCast()
        {
            _h.PlaceNear(_mage, _dummy, 10f);
            _h.Send(_mage, new UseSkillCommand { SkillId = "fireball", TargetId = _dummy });
            Assert.That(_h.World.Casts.Get(_mage).IsCasting, Is.True);

            Apply(_mage, "stunned");
            Assert.That(_h.World.Casts.Get(_mage).IsCasting, Is.False, "stun interrupts the cast");

            Vec3 before = _h.World.Transforms.Get(_mage).Position;
            _h.Send(_mage, new MoveCommand { DirX = 1f, DirZ = 0f });
            _h.Run(0.5f);
            Assert.That(_h.World.Transforms.Get(_mage).Position.X, Is.EqualTo(before.X), "stunned: no moving");
            _h.Send(_mage, new UseSkillCommand { SkillId = "reserve_nova", TargetId = _dummy });
            Assert.That(_h.Rejections(_mage), Does.Contain(RejectReasons.Stunned));

            _h.Run(_h.Content.StatusEffects["stunned"].Duration);
            Assert.That(_h.World.Transforms.Get(_mage).Position.X, Is.GreaterThan(before.X), "moves again after the stun");
        }

        [Test]
        public void StatDebuff_LowersMobDefense_PerStack_AndComesBack()
        {
            float defense = _h.Vitals(_dummy).Defense;
            Apply(_dummy, "sundered");
            Apply(_dummy, "sundered");
            Assert.That(_h.Vitals(_dummy).Defense, Is.EqualTo(System.Math.Max(0f, defense - 0.1f)).Within(0.0001f));
            Statuses.Remove(_dummy, "sundered");
            Assert.That(_h.Vitals(_dummy).Defense, Is.EqualTo(defense).Within(0.0001f));
        }

        [Test]
        public void BuffPotion_ChangesStats_SameGroupReplacesTheOther()
        {
            PlayerCharacter player = _h.Player(_mage);
            float defense = player.Stats.Get(StatId.Defense);
            _h.Send(_mage, new UseConsumableCommand { BagIndex = _h.BagIndexOf(_mage, "iron_skin_elixir") });
            Assert.That(player.Stats.Get(StatId.Defense), Is.EqualTo(defense + 0.1f).Within(0.0001f));
            Assert.That(_h.Vitals(_mage).Defense, Is.EqualTo(defense + 0.1f).Within(0.0001f));

            Statuses.Apply(_mage, _mage, "fury", 1f);
            Assert.That(Statuses.Has(_mage, "iron_skin"), Is.False, "one elixir group: fury replaced iron skin");
            Assert.That(player.Stats.Get(StatId.Defense), Is.EqualTo(defense).Within(0.0001f));
        }

        [Test]
        public void Immunity_And_Dispel()
        {
            var ward = new StatusEffectDefinition
            {
                Id = "test_unstoppable", NameKey = "x", Kind = StatusKind.Buff, Category = StatusCategory.Special, Duration = 5f, MaxStacks = 1,
            };
            ward.ImmunityTags.Add("Control");
            ward.ImmunityTags.Add("Damage");
            _h.Content.StatusEffects.Add(ward.Id, ward);
            Apply(_mage, ward.Id);
            Assert.That(Apply(_mage, "stunned"), Is.False, "immune to Control");

            Apply(_dummy, "burning");
            Apply(_dummy, "sundered");
            Assert.That(Statuses.Dispel(_dummy, StatusKind.Debuff, "Magic", 5), Is.EqualTo(1));
            Assert.That(Statuses.Has(_dummy, "burning"), Is.False);
            Assert.That(Statuses.Has(_dummy, "sundered"), Is.True, "Physical is not Magic");
        }

        [Test]
        public void Death_ClearsStatuses()
        {
            Apply(_mage, "second_wind_regen");
            Apply(_mage, "sundered");
            _h.Vitals(_mage).Health = 1f;
            Apply(_mage, "burning");
            _h.Run(1.1f);
            Assert.That(_h.Vitals(_mage).IsDead, Is.True);
            Assert.That(Statuses.Snapshot(_mage), Is.Empty);
        }

        [Test]
        public void Skills_ApplyTheirStatuses_FromData()
        {
            EntityId warrior = _h.AddPlayer("warrior");
            _h.PlaceNear(warrior, _dummy, 2f);
            _h.Send(warrior, new UseSkillCommand { SkillId = "blood_strike", TargetId = _dummy });
            Assert.That(Statuses.Has(_dummy, "sundered"), Is.True);
            _h.Send(warrior, new UseSkillCommand { SkillId = "second_wind" });
            Assert.That(Statuses.Has(warrior, "second_wind_regen"), Is.True);
        }

        [Test]
        public void Threat_FromDamageAndHealing_TopIsTheBiggest()
        {
            EntityId healer = _h.AddPlayer("mage");
            _h.PlaceNear(_mage, _dummy, 10f);
            _h.PlaceNear(healer, _dummy, 10f);
            _h.Send(_mage, new UseSkillCommand { SkillId = "reserve_nova", TargetId = _dummy });
            float threat = _h.Server.Threat.ThreatOf(_dummy, _mage);
            Assert.That(threat, Is.GreaterThan(0f));
            Assert.That(_h.Server.Threat.TopThreat(_dummy), Is.EqualTo(_mage));

            _h.Vitals(_mage).Health -= 30f;
            _h.Send(healer, new UseSkillCommand { SkillId = "arcane_mend", TargetId = _mage });
            _h.Run(1.1f);
            Assert.That(_h.Server.Threat.ThreatOf(_dummy, healer), Is.GreaterThan(0f), "healing a fighter draws threat");

            _h.Server.Tick();
            EntitySnapshot dummy = _h.Player(_mage).Outbox.World.Entities.First(e => e.Id == _dummy);
            Assert.That(dummy.TargetOfId, Is.EqualTo(_h.Server.Threat.TopThreat(_dummy)), "target of target = top threat");
        }

        [Test]
        public void Codec_RoundTripsStatusesAndAbsorb()
        {
            Apply(_dummy, "burning");
            Apply(_dummy, "burning");
            _h.Server.Tick();
            var world = (WorldSnapshot)ProtocolCodec.Decode(ProtocolCodec.Encode(_h.Player(_mage).Outbox.World));
            StatusSnapshot status = world.Entities.First(e => e.Id == _dummy).Statuses.Single();
            Assert.That(status.StatusId, Is.EqualTo("burning"));
            Assert.That(status.Stacks, Is.EqualTo(2));
            var combat = (CombatEventMessage)ProtocolCodec.Decode(ProtocolCodec.Encode(new CombatEventMessage { Kind = CombatEventKind.Damage, Amount = 5, Absorbed = 7 }));
            Assert.That(combat.Absorbed, Is.EqualTo(7));
        }
    }
}
