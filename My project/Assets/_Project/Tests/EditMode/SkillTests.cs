using System;
using Game.Server.Players;
using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;
using Game.Shared.Rules;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>03, 7.2 and questions 4, 5, 54: variants, mastery, cooldowns, casting.</summary>
    public sealed class SkillTests
    {
        [Test]
        public void Fireball_DealsDamageByFormula_AfterCastTime()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            EntityId dummy = h.NearestMob(mage, "training_dummy");
            h.PlaceNear(mage, dummy, 10f);
            PlayerCharacter player = h.Player(mage);
            float efficiency = MasteryMath.Efficiency(player.GetMastery("fireball"), 0.3f, 1.5f);
            float bonus = player.Stats.Get(StatId.DamageBonus);
            float health = h.Vitals(dummy).Health;
            int expected = (int)Math.Round(40f * efficiency * (1f + bonus) * (1f - h.Vitals(dummy).Defense), MidpointRounding.AwayFromZero);

            h.Send(mage, new UseSkillCommand { SkillId = "fireball", TargetId = dummy });
            Assert.That(h.World.Casts.Get(mage).IsCasting, Is.True);
            h.Run(1.0f);
            Assert.That(h.Vitals(dummy).Health, Is.EqualTo(health), "still casting");
            h.Run(0.6f);

            Assert.That(h.Vitals(dummy).Health, Is.EqualTo(health - expected).Within(0.01f));
        }

        [Test]
        public void SecondSkillWhileCasting_IsRejected_ThenCooldownApplies()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            EntityId dummy = h.NearestMob(mage, "training_dummy");
            h.PlaceNear(mage, dummy, 10f);

            h.Send(mage, new UseSkillCommand { SkillId = "fireball", TargetId = dummy });
            h.Send(mage, new UseSkillCommand { SkillId = "fireball", TargetId = dummy });
            Assert.That(h.Rejections(mage), Does.Contain(RejectReasons.AlreadyCasting));

            h.Run(1.6f);
            h.ClearOutbox(mage);
            h.Send(mage, new UseSkillCommand { SkillId = "fireball", TargetId = dummy });
            Assert.That(h.Rejections(mage), Does.Contain(RejectReasons.OnCooldown));
        }

        [Test]
        public void Modifiers_SelectVariantsUnlockedByMastery()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            PlayerCharacter player = h.Player(mage);
            SkillDefinition fireball = h.Content.Skills["fireball"];
            SkillDefinition mend = h.Content.Skills["arcane_mend"];

            h.Send(mage, new UseSkillCommand { SkillId = "fireball", TargetId = EntityId.None, Modifier = SkillModifier.Shift });
            PlayerStateSnapshot state = player.Outbox.State;
            SkillStateSnapshot fireballState = Array.Find(state.Skills, s => s.SkillId == "fireball");
            SkillStateSnapshot mendState = Array.Find(state.Skills, s => s.SkillId == "arcane_mend");

            Assert.That(fireballState.EmpoweredAvailable, Is.True, "mastery 35% ≥ 30%");
            Assert.That(fireballState.WeakenedAvailable, Is.True, "mastery 35% ≥ 10%");
            Assert.That(mendState.EmpoweredAvailable, Is.False, "mastery 20% < 40%");
            Assert.That(mendState.WeakenedAvailable, Is.False, "arcane_mend has no weakened variant");
            Assert.That(fireball.Weakened, Is.Not.Null);
            Assert.That(mend.Weakened, Is.Null);
        }

        [Test]
        public void ShiftCastsEmpoweredVariant_WithItsCost()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            EntityId dummy = h.NearestMob(mage, "training_dummy");
            h.PlaceNear(mage, dummy, 10f);
            float main = h.Mana(mage).Main;

            h.Send(mage, new UseSkillCommand { SkillId = "fireball", TargetId = dummy, Modifier = SkillModifier.Shift });
            Assert.That(h.World.Casts.Get(mage).Variant, Is.EqualTo(SkillVariantKind.Empowered));
            h.Run(2.1f);

            Assert.That(main - h.Mana(mage).Main, Is.EqualTo(34f).Within(1f), "empowered cost 34 (minus a little regen)");
        }

        [Test]
        public void MovingInterruptsInterruptibleCast()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            EntityId dummy = h.NearestMob(mage, "training_dummy");
            h.PlaceNear(mage, dummy, 10f);
            h.Send(mage, new UseSkillCommand { SkillId = "fireball", TargetId = dummy });

            h.Send(mage, new MoveCommand { DirX = 1f });

            Assert.That(h.World.Casts.Get(mage).IsCasting, Is.False);
        }

        [Test]
        public void OutOfRange_IsRejected()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            EntityId dummy = h.NearestMob(mage, "training_dummy");
            h.PlaceNear(mage, dummy, 30f);

            h.Send(mage, new UseSkillCommand { SkillId = "fireball", TargetId = dummy });

            Assert.That(h.Rejections(mage), Does.Contain(RejectReasons.OutOfRange));
        }

        [Test]
        public void MasteryGrowsFromUseInCombat()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            EntityId dummy = h.NearestMob(mage, "training_dummy");
            h.PlaceNear(mage, dummy, 10f);
            float before = h.Player(mage).GetMastery("fireball");

            h.Send(mage, new UseSkillCommand { SkillId = "fireball", TargetId = dummy });
            h.Run(1.6f);

            Assert.That(h.Player(mage).GetMastery("fireball"), Is.EqualTo(before + 0.01f).Within(0.0001f));
        }

        [Test]
        public void SelfHeal_RestoresHealth()
        {
            var h = new ServerHarness();
            EntityId warrior = h.AddPlayer("warrior");
            h.Vitals(warrior).Health = 50f;

            h.Send(warrior, new UseSkillCommand { SkillId = "second_wind" });

            Assert.That(h.Vitals(warrior).Health, Is.GreaterThan(50f));
        }

        [Test]
        public void KilledMob_RespawnsWithFullHealth()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            EntityId dummy = h.NearestMob(mage, "training_dummy");
            h.PlaceNear(mage, dummy, 5f);
            h.Vitals(dummy).Health = 1f;

            h.Send(mage, new UseSkillCommand { SkillId = "reserve_nova", TargetId = dummy });
            Assert.That(h.Vitals(dummy).IsDead, Is.True);

            h.Run(h.Content.Mobs["training_dummy"].RespawnSeconds + 0.1f);
            Assert.That(h.Vitals(dummy).IsDead, Is.False);
            Assert.That(h.Vitals(dummy).Health, Is.EqualTo(h.Vitals(dummy).MaxHealth));
        }
    }
}
