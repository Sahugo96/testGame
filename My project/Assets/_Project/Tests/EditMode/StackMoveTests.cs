using System.Linq;
using Game.Network;
using Game.Server.Items;
using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Dragging bag stacks (decision of the developer): move, merge, swap, split half (Shift) or one (Ctrl), audited.</summary>
    public sealed class StackMoveTests
    {
        private ServerHarness _h;
        private EntityId _player;
        private BagSection _resources;

        [SetUp]
        public void SetUp()
        {
            _h = new ServerHarness();
            _player = _h.AddPlayer("warrior");
            _resources = _h.Content.BagSections.First(section => section.Id == "resources");
        }

        private ItemInstance At(int index) => _h.Player(_player).Bag.Get(index);

        private int FreeResourceCell() => _h.Player(_player).Bag.FindFreeSlot(_resources.Start, _resources.Size);

        [Test]
        public void Split_ThenMergeBack_KeepsTheTotalAndAudits()
        {
            int shards = _h.BagIndexOf(_player, "philosopher_stone_shard");
            string original = At(shards).SourceId;
            int free = FreeResourceCell();

            _h.Send(_player, new MoveBagItemCommand { FromIndex = shards, ToIndex = free, Count = 10 });
            Assert.That(At(shards).Count, Is.EqualTo(10));
            Assert.That(At(free).Count, Is.EqualTo(10));
            Assert.That(At(free).SourceId, Is.Not.EqualTo(original), "a split makes a new instance on the server");
            Assert.That(_h.Audit.Entries.Any(e => e.Action == AuditActions.StackSplit && e.InstanceId == At(free).SourceId && e.Reason == original), Is.True);

            int other = FreeResourceCell();
            _h.Send(_player, new MoveBagItemCommand { FromIndex = shards, ToIndex = other, Count = 1 });
            Assert.That(At(other).Count, Is.EqualTo(1));

            _h.Send(_player, new MoveBagItemCommand { FromIndex = other, ToIndex = free, Count = 0 });
            Assert.That(At(other), Is.Null, "fully merged");
            Assert.That(At(free).Count, Is.EqualTo(11));
            Assert.That(_h.Audit.Entries.Any(e => e.Action == AuditActions.StackMerge), Is.True);
            Assert.That(At(shards).Count + At(free).Count, Is.EqualTo(20), "nothing created or lost");
        }

        [Test]
        public void WholeStack_MovesOrSwaps_ButNeverLeavesItsSection()
        {
            int shards = _h.BagIndexOf(_player, "philosopher_stone_shard");
            int ore = _h.BagIndexOf(_player, "iron_ore");
            string shardId = At(shards).SourceId;
            string oreId = At(ore).SourceId;

            _h.Send(_player, new MoveBagItemCommand { FromIndex = shards, ToIndex = ore, Count = 0 });
            Assert.That(At(ore).SourceId, Is.EqualTo(shardId));
            Assert.That(At(shards).SourceId, Is.EqualTo(oreId), "different items swap");

            _h.Send(_player, new MoveBagItemCommand { FromIndex = ore, ToIndex = shards, Count = 5 });
            Assert.That(_h.Rejections(_player), Does.Contain(RejectReasons.InventoryMoveInvalid), "a part cannot go onto another item");

            BagSection consumables = _h.Content.BagSections.First(section => section.Id == "consumables");
            _h.Send(_player, new MoveBagItemCommand { FromIndex = ore, ToIndex = consumables.Start + consumables.Size - 1, Count = 0 });
            Assert.That(_h.Rejections(_player), Does.Contain(RejectReasons.InventoryWrongSection));

            int free = FreeResourceCell();
            _h.Send(_player, new MoveBagItemCommand { FromIndex = ore, ToIndex = free, Count = 0 });
            Assert.That(At(free).SourceId, Is.EqualTo(shardId));
            Assert.That(At(ore), Is.Null);
        }

        [Test]
        public void Codec_RoundTripsTheMove()
        {
            var move = (MoveBagItemCommand)ProtocolCodec.Decode(ProtocolCodec.Encode(new MoveBagItemCommand { FromIndex = 3, ToIndex = 7, Count = 2 }));
            Assert.That((move.FromIndex, move.ToIndex, move.Count), Is.EqualTo((3, 7, 2)));
        }
    }
}
