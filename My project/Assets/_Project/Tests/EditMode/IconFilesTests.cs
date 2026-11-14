using System.Collections.Generic;
using System.IO;
using Game.Server;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using Game.Shared.Content.Items;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>
    /// Every `Icon` of the real content has its picture: Resources/Icons/&lt;group&gt;/&lt;icon&gt;.png (drawn by tools/IconGen).
    /// The game itself shows a name instead of a missing picture; this test keeps the base content complete.
    /// </summary>
    public sealed class IconFilesTests
    {
        [Test]
        public void EveryIconOfTheContent_HasAPicture()
        {
            var errors = new List<string>();
            ContentDatabase db = ServerContentFactory.Build(TestContentLoader.LoadFiles(), errors);
            Assert.That(errors, Is.Empty, string.Join("\n", errors));

            var missing = new List<string>();
            foreach (ItemDefinition item in db.Items.Values)
            {
                Check("items", item.Icon, "item " + item.Id, missing);
            }
            foreach (SkillDefinition skill in db.Skills.Values)
            {
                Check("skills", skill.Icon, "skill " + skill.Id, missing);
            }
            foreach (StatusEffectDefinition status in db.StatusEffects.Values)
            {
                Check("statuses", status.Icon, "status " + status.Id, missing);
            }
            foreach (HudWindowDto window in db.HudWindows)
            {
                Check("windows", window.Icon, "window " + window.Id, missing);
            }
            foreach (EmoteDto emote in db.Config.Emotes ?? new EmoteDto[0])
            {
                Check("emotes", emote.Icon, "emote " + emote.Id, missing);
            }
            Assert.That(missing, Is.Empty, "No picture for:\n" + string.Join("\n", missing));
        }

        private static void Check(string group, string icon, string owner, List<string> missing)
        {
            if (string.IsNullOrEmpty(icon))
            {
                missing.Add(owner + ": the Icon field is empty");
                return;
            }
            string path = Path.Combine(TestContentLoader.IconsRoot, group, icon + ".png");
            if (!File.Exists(path))
            {
                missing.Add(owner + ": " + group + "/" + icon + ".png");
            }
        }
    }
}
