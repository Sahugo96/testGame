using Game.Shared.Content;
using Game.Shared.Rules;
using NUnit.Framework;

namespace Game.Tests
{
    public sealed class StatSheetTests
    {
        [Test]
        public void ModifiersApplyInOrder_BaseAddPercentMultiplyCap()
        {
            var sheet = new StatSheet();
            sheet.SetBase(StatId.MaxHealth, 100f);
            sheet.AddModifier(new StatModifier(StatId.MaxHealth, ModifierOp.Add, 20f, "a"));
            sheet.AddModifier(new StatModifier(StatId.MaxHealth, ModifierOp.PercentAdd, 0.1f, "b"));
            sheet.AddModifier(new StatModifier(StatId.MaxHealth, ModifierOp.PercentAdd, 0.2f, "c"));
            sheet.AddModifier(new StatModifier(StatId.MaxHealth, ModifierOp.Multiply, 2f, "d"));
            Assert.That(sheet.Get(StatId.MaxHealth), Is.EqualTo(312f).Within(0.001f));

            sheet.SetCap(StatId.MaxHealth, 300f);
            Assert.That(sheet.Get(StatId.MaxHealth), Is.EqualTo(300f));
        }

        [Test]
        public void RemoveSource_DropsOnlyThatSource()
        {
            var sheet = new StatSheet();
            sheet.AddModifier(new StatModifier(StatId.Defense, ModifierOp.Add, 0.1f, "helm"));
            sheet.AddModifier(new StatModifier(StatId.Defense, ModifierOp.Add, 0.2f, "armor"));
            sheet.RemoveSource("helm");
            Assert.That(sheet.Get(StatId.Defense), Is.EqualTo(0.2f).Within(0.0001f));
        }
    }
}
