using Game.Server.Systems;
using Game.Server.World;
using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Questions 1, 2, 3, 45: costs, mana lock and regeneration.</summary>
    public sealed class ResourceTests
    {
        [Test]
        public void SmartManaCost_TakesMainFirstThenReserve_AndLocksMain()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            EntityId dummy = h.NearestMob(mage, "training_dummy");
            h.PlaceNear(mage, dummy, 10f);
            ref ManaData mana = ref h.Mana(mage);
            mana.Main = 10f;
            mana.MainRegen = 0f;
            float reserveBefore = mana.Reserve;

            h.Send(mage, new UseSkillCommand { SkillId = "fireball", TargetId = dummy, Modifier = SkillModifier.None });
            h.Run(1.6f);

            ManaData after = h.Mana(mage);
            Assert.That(after.Reserve, Is.EqualTo(reserveBefore - 10f).Within(0.01f), "20 cost: 10 from main, 10 from reserve");
            Assert.That(after.MainLocked, Is.True, "main empty and reserve not full → locked");
            Assert.That(after.Main, Is.EqualTo(0f).Within(0.0001f), "locked main does not regenerate passively");
        }

        [Test]
        public void MixedCost_IsPaidFromEachPool()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            EntityId dummy = h.NearestMob(mage, "training_dummy");
            h.PlaceNear(mage, dummy, 5f);
            ManaData before = h.Mana(mage);

            h.Send(mage, new UseSkillCommand { SkillId = "reserve_nova", TargetId = dummy });

            ManaData after = h.Mana(mage);
            Assert.That(before.Main - after.Main, Is.EqualTo(25f).Within(0.5f));
            Assert.That(before.Reserve - after.Reserve, Is.EqualTo(25f).Within(0.01f));
        }

        [Test]
        public void NotEnoughMana_RejectsWithoutPaying()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            EntityId dummy = h.NearestMob(mage, "training_dummy");
            h.PlaceNear(mage, dummy, 5f);
            h.Mana(mage).Main = 5f;
            h.Mana(mage).Reserve = 5f;

            h.Send(mage, new UseSkillCommand { SkillId = "fireball", TargetId = dummy });

            Assert.That(h.Rejections(mage), Does.Contain(RejectReasons.NotEnoughMana));
            Assert.That(h.World.Casts.Get(mage).IsCasting, Is.False);
        }

        [Test]
        public void HealthCost_CanKillCaster_EffectOnTargetStillApplies()
        {
            var h = new ServerHarness();
            EntityId warrior = h.AddPlayer("warrior");
            EntityId dummy = h.NearestMob(warrior, "training_dummy");
            h.PlaceNear(warrior, dummy, 2f);
            h.Vitals(warrior).Health = 5f;
            float dummyBefore = h.Vitals(dummy).Health;

            h.Send(warrior, new UseSkillCommand { SkillId = "blood_strike", TargetId = dummy });

            Assert.That(h.Vitals(warrior).IsDead, Is.True);
            Assert.That(h.Vitals(dummy).Health, Is.LessThan(dummyBefore));
        }

        [Test]
        public void Reserve_RegeneratesOnlyOutOfCombat_AtTenPercent_AndUnlocksMainWhenFull()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            ref ManaData mana = ref h.Mana(mage);
            mana.Main = 0f;
            mana.Reserve = mana.MaxReserve - 0.2f;
            ResourceSystem.UpdateLock(ref mana);
            float rate = mana.MainRegen * h.Content.Config.ReserveRegenOutOfCombatFactor;

            h.Server.Tick();
            Assert.That(h.Mana(mage).MainLocked, Is.True);

            h.Run(0.2f / rate + 0.1f);
            Assert.That(h.Mana(mage).Reserve, Is.EqualTo(h.Mana(mage).MaxReserve).Within(0.0001f));
            Assert.That(h.Mana(mage).MainLocked, Is.False);

            h.Run(1f);
            Assert.That(h.Mana(mage).Main, Is.GreaterThan(0f), "main regenerates again after unlock");
        }

        [Test]
        public void Reserve_DoesNotRegenerateInCombat()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            EntityId dummy = h.NearestMob(mage, "training_dummy");
            h.PlaceNear(mage, dummy, 5f);
            h.Send(mage, new UseSkillCommand { SkillId = "reserve_nova", TargetId = dummy });
            float reserve = h.Mana(mage).Reserve;

            h.Run(2f);

            Assert.That(h.InCombat(mage), Is.True);
            Assert.That(h.Mana(mage).Reserve, Is.EqualTo(reserve).Within(0.0001f));
        }

        [Test]
        public void Health_RegeneratesInCombat()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            EntityId dummy = h.NearestMob(mage, "training_dummy");
            h.PlaceNear(mage, dummy, 5f);
            h.Send(mage, new UseSkillCommand { SkillId = "reserve_nova", TargetId = dummy });
            h.Vitals(mage).Health = 10f;

            h.Run(1f);

            Assert.That(h.InCombat(mage), Is.True);
            Assert.That(h.Vitals(mage).Health, Is.GreaterThan(10f), "question 63: health regenerates in combat too");
        }

        [Test]
        public void CombatState_EndsAfterQuietPeriod()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            EntityId dummy = h.NearestMob(mage, "training_dummy");
            h.PlaceNear(mage, dummy, 5f);
            h.Send(mage, new UseSkillCommand { SkillId = "reserve_nova", TargetId = dummy });
            Assert.That(h.InCombat(mage), Is.True);

            h.Run(h.Content.Config.CombatExitSeconds + 0.5f);

            Assert.That(h.InCombat(mage), Is.False);
        }
    }
}
