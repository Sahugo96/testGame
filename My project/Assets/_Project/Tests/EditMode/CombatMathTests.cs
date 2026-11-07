using Game.Shared.Rules;
using NUnit.Framework;

namespace Game.Tests
{
    public sealed class CombatMathTests
    {
        [TestCase(0.5f, 0.4f, 1, 1.5f)]
        [TestCase(0.5f, 0.6f, 0, 1f)]
        [TestCase(1.5f, 0.4f, 2, 3f)]
        [TestCase(1.5f, 0.6f, 1, 1.5f)]
        [TestCase(3f, 0.0f, 3, 4.5f)]
        [TestCase(5f, 0.0f, 3, 4.5f)]
        public void MultiTierCrit_FollowsQuestion25(float chance, float roll, int tier, float multiplier)
        {
            CritRoll crit = CombatMath.RollCrit(chance, 1.5f, 3f, new FixedRandom(roll));
            Assert.That(crit.Tier, Is.EqualTo(tier));
            Assert.That(crit.Multiplier, Is.EqualTo(multiplier).Within(0.0001f));
        }

        [Test]
        public void Damage_AppliesBonusCritDefenseAndMinimum()
        {
            var crit = new CritRoll { Tier = 1, Multiplier = 1.5f };
            Assert.That(CombatMath.CalculateDamage(40f, 1f, 0.25f, crit, 0.1f, 1f, 1), Is.EqualTo(68));
            Assert.That(CombatMath.CalculateDamage(40f, 1f, 0f, new CritRoll { Multiplier = 1f }, 1f, 1f, 1), Is.EqualTo(1));
            Assert.That(CombatMath.CalculateDamage(40f, 1f, 0f, new CritRoll { Multiplier = 1f }, 0.9f, 0.75f, 1), Is.EqualTo(10));
        }

        [Test]
        public void Heal_IgnoresDefense()
        {
            Assert.That(CombatMath.CalculateHeal(35f, 1.2f, new CritRoll { Tier = 1, Multiplier = 1.5f }), Is.EqualTo(63));
        }

        [Test]
        public void Mastery_MapsToEfficiencyRange()
        {
            Assert.That(MasteryMath.Efficiency(0f, 0.3f, 1.5f), Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(MasteryMath.Efficiency(1f, 0.3f, 1.5f), Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(MasteryMath.Efficiency(0.5f, 0.3f, 1.5f), Is.EqualTo(0.9f).Within(0.0001f));
        }
    }
}
