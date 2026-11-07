using System.Linq;
using Game.Server.Items;
using Game.Shared.Core;
using Game.Shared.Protocol;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Ground loot, LootEntry (06, 7.8): who may pick up, the first one wins, it expires; solo full bag drops to the ground.</summary>
    public sealed class GroundLootTests
    {
        private ServerHarness _h;
        private EntityId _a;
        private EntityId _b;
        private EntityId _outsider;

        [SetUp]
        public void SetUp()
        {
            _h = new ServerHarness();
            _h.Content.Mobs["cube"].Drops[0].Chance = 1f;
            _a = _h.AddPlayer("mage");
            _b = _h.AddPlayer("mage");
            _outsider = _h.AddPlayer("mage");
            _h.Send(_a, new GroupInviteCommand { TargetId = _b });
            _h.Send(_b, new GroupInviteResponseCommand { Accept = true });
        }

        private EntityId KillCubeWithBoth()
        {
            EntityId cube = _h.NearestMob(_a, "cube");
            ref var vitals = ref _h.Vitals(cube);
            vitals.MaxHealth = 1000f;
            vitals.Health = 1000f;
            foreach (EntityId p in new[] { _b, _a })
            {
                _h.PlaceNear(p, cube, 5f);
                _h.Player(p).SkillCooldowns.Clear();
                _h.World.Casts.Get(p).GlobalCooldownRemaining = 0f;
                _h.Mana(p).Reserve = _h.Mana(p).MaxReserve;
                if (p == _a)
                {
                    _h.Vitals(cube).Health = 1f;
                }
                _h.Send(p, new UseSkillCommand { SkillId = "reserve_nova", TargetId = cube });
            }
            Assert.That(_h.Vitals(cube).IsDead, Is.True);
            return _h.World.Entities.First(id => _h.World.Loot.Has(id));
        }

        private int Shards(EntityId id) => _h.Player(id).Bag.Get(_h.BagIndexOf(id, "philosopher_stone_shard"))?.Count ?? 0;

        private void Go(EntityId player, EntityId loot) => _h.World.Transforms.Get(player).Position = _h.World.Transforms.Get(loot).Position;

        [Test]
        public void LootEntry_IsVisible_AsAnEntity()
        {
            EntityId loot = KillCubeWithBoth();
            _h.Server.Tick();
            EntitySnapshot entity = _h.Player(_a).Outbox.World.Entities.First(e => e.Id == loot);
            Assert.That(entity.Kind, Is.EqualTo(EntityKind.Loot));
            Assert.That(entity.DefinitionId, Is.EqualTo("philosopher_stone_shard"));
        }

        [Test]
        public void OnlyEligible_Near_FirstWins()
        {
            EntityId loot = KillCubeWithBoth();
            Go(_outsider, loot);
            _h.Send(_outsider, new PickUpLootCommand { LootId = loot });
            Assert.That(_h.Rejections(_outsider), Does.Contain(RejectReasons.LootNotYours));

            _h.PlaceNear(_b, loot, _h.Content.Config.InteractRange + 5f);
            _h.Send(_b, new PickUpLootCommand { LootId = loot });
            Assert.That(_h.Rejections(_b), Does.Contain(RejectReasons.LootTooFar));

            int a = Shards(_a);
            int b = Shards(_b);
            Go(_a, loot);
            Go(_b, loot);
            _h.Server.Submit(_a, new PickUpLootCommand { LootId = loot });
            _h.Server.Submit(_b, new PickUpLootCommand { LootId = loot });
            _h.Server.Tick();
            Assert.That(Shards(_a) - a + Shards(_b) - b, Is.EqualTo(1), "exactly one gets it");
            Assert.That(_h.Rejections(_a).Concat(_h.Rejections(_b)), Does.Contain(RejectReasons.LootGone));
        }

        [Test]
        public void ItemInstance_IsCreatedOnlyAtPickup()
        {
            EntityId loot = KillCubeWithBoth();
            int created = _h.Audit.Entries.Count(e => e.Action == AuditActions.Create || e.Action == AuditActions.StackAdd);
            Go(_a, loot);
            _h.Send(_a, new PickUpLootCommand { LootId = loot });
            Assert.That(_h.Audit.Entries.Count(e => e.Action == AuditActions.Create || e.Action == AuditActions.StackAdd), Is.EqualTo(created + 1));
        }

        [Test]
        public void Loot_Expires()
        {
            EntityId loot = KillCubeWithBoth();
            _h.Run(_h.Content.Config.LootSeconds + 0.1f);
            Assert.That(_h.World.Loot.Has(loot), Is.False);
            Assert.That(_h.World.Exists(loot), Is.False);
        }

        [Test]
        public void Solo_FullSection_DropsToTheGround_ForTheKiller()
        {
            EntityId solo = _h.AddPlayer("mage");
            BagSectionFill(solo);
            EntityId cube = _h.NearestMob(solo, "cube");
            _h.PlaceNear(solo, cube, 5f);
            _h.Send(solo, new UseSkillCommand { SkillId = "reserve_nova", TargetId = cube });
            EntityId loot = _h.World.Entities.First(id => _h.World.Loot.Has(id));
            Assert.That(_h.World.Loot.Get(loot).Eligible, Is.EqualTo(new[] { solo }));
            Assert.That(_h.Player(solo).Outbox.Notifications.Any(n => n.TextKey == NotificationKeys.LootOnGround), Is.True);
        }

        /// <summary>Fills the resources section (shards live there) with full stacks of other materials.</summary>
        private void BagSectionFill(EntityId player)
        {
            var section = _h.Content.BagSections.Find(s => s.Id == "resources");
            var bag = _h.Player(player).Bag;
            for (int i = section.Start; i < section.Start + section.Size; i++)
            {
                bag.Set(i, new ItemInstance { InstanceId = System.Guid.NewGuid(), DefinitionId = "iron_ore", Count = 999 });
            }
        }
    }
}
