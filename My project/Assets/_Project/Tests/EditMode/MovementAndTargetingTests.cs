using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;
using NUnit.Framework;

namespace Game.Tests
{
    public sealed class MovementAndTargetingTests
    {
        [Test]
        public void Movement_UsesMoveSpeed_AndClampsDirection()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            float speed = h.Player(mage).Stats.Get(StatId.MoveSpeed);

            h.Send(mage, new MoveCommand { DirX = 3f, DirZ = 0f });
            float start = h.World.Transforms.Get(mage).Position.X;
            h.Run(1f);

            Assert.That(h.World.Transforms.Get(mage).Position.X - start, Is.EqualTo(speed).Within(0.01f));
            Assert.That(h.World.Transforms.Get(mage).Yaw, Is.EqualTo(90f).Within(0.01f));
        }

        [Test]
        public void Movement_StaysInsideWorld()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            h.Send(mage, new MoveCommand { DirX = 0f, DirZ = -1f });
            h.Run(30f);
            Assert.That(h.World.Transforms.Get(mage).Position.Z, Is.EqualTo(-h.Content.Config.WorldHalfSize).Within(0.001f));
        }

        [Test]
        public void TabCycle_PicksNearestHostileFirst()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            EntityId nearest = h.NearestMob(mage, "training_dummy");

            h.Send(mage, new CycleTargetCommand());

            Assert.That(h.Player(mage).TargetId, Is.EqualTo(nearest));
        }

        [Test]
        public void EnemySkillOnFriendlyTarget_IsRejected()
        {
            var h = new ServerHarness();
            EntityId mage = h.AddPlayer("mage");
            EntityId warrior = h.AddPlayer("warrior");

            h.Send(mage, new UseSkillCommand { SkillId = "fireball", TargetId = warrior });

            Assert.That(h.Rejections(mage), Does.Contain(RejectReasons.InvalidTarget));
        }
    }
}
