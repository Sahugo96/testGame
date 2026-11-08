using Game.Shared.Content.Packs;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Module 4, question 80: which way the editor syncs cards and JSON.</summary>
    public sealed class ContentSyncRuleTests
    {
        [TestCase(null, null, null, ContentSyncAction.Nothing)]
        [TestCase("a", null, null, ContentSyncAction.Import)]
        [TestCase(null, "a", "a", ContentSyncAction.Export)]
        [TestCase("a", "a", null, ContentSyncAction.InSync)]
        [TestCase("a", "b", "a", ContentSyncAction.Export)]
        [TestCase("b", "a", "a", ContentSyncAction.Import)]
        [TestCase("b", "c", "a", ContentSyncAction.Conflict)]
        [TestCase("b", "c", null, ContentSyncAction.Conflict)]
        public void Decide(string json, string cards, string last, ContentSyncAction expected) =>
            Assert.That(ContentSyncRule.Decide(json, cards, last), Is.EqualTo(expected));
    }
}
