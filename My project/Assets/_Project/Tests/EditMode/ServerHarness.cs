using System.Collections.Generic;
using Game.Server;
using Game.Server.Items;
using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Real content + server with deterministic random; helpers to submit commands and advance time.</summary>
    public sealed class ServerHarness
    {
        public readonly ContentDatabase Content;
        public readonly GameServer Server;
        public readonly FixedRandom Random = new FixedRandom(0.99f);
        public readonly InMemoryAuditLog Audit = new InMemoryAuditLog();

        public ServerHarness()
        {
            var errors = new List<string>();
            Content = ServerContentFactory.Build(TestContentLoader.LoadFiles(), errors);
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
            Server = new GameServer(Content, Random, Audit);
        }

        public ServerWorld World => Server.Context.World;

        public EntityId AddPlayer(string classId) => Server.AddPlayer(classId);

        public PlayerCharacter Player(EntityId id) => Server.GetPlayer(id);

        public void Send(EntityId player, IGameCommand command)
        {
            Server.Submit(player, command);
            Server.Tick();
        }

        public void Run(float seconds)
        {
            int ticks = (int)System.Math.Ceiling(seconds / Server.TickInterval);
            for (int i = 0; i < ticks; i++)
            {
                Server.Tick();
            }
        }

        public EntityId FindMob(string mobId)
        {
            foreach (EntityId id in World.Entities)
            {
                if (World.Mobs.Has(id) && World.Identities.Get(id).DefinitionId == mobId)
                {
                    return id;
                }
            }
            return EntityId.None;
        }

        /// <summary>Nearest mob of the given type to the player.</summary>
        public EntityId NearestMob(EntityId player, string mobId)
        {
            EntityId best = EntityId.None;
            float bestDistance = float.MaxValue;
            Vec3 origin = World.Transforms.Get(player).Position;
            foreach (EntityId id in World.Entities)
            {
                if (!World.Mobs.Has(id) || World.Identities.Get(id).DefinitionId != mobId)
                {
                    continue;
                }
                float distance = Vec3.DistanceXZ(origin, World.Transforms.Get(id).Position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = id;
                }
            }
            return best;
        }

        public void PlaceNear(EntityId player, EntityId target, float distance)
        {
            Vec3 targetPosition = World.Transforms.Get(target).Position;
            World.Transforms.Get(player).Position = new Vec3(targetPosition.X, 0f, targetPosition.Z - distance);
        }

        public List<string> Rejections(EntityId player)
        {
            var reasons = new List<string>();
            foreach (CommandRejectedMessage message in Player(player).Outbox.Rejections)
            {
                reasons.Add(message.ReasonKey);
            }
            return reasons;
        }

        public void ClearOutbox(EntityId player)
        {
            Player(player).Outbox.Rejections.Clear();
            Player(player).Outbox.CombatEvents.Clear();
        }

        public int BagIndexOf(EntityId player, string definitionId)
        {
            PlayerCharacter character = Player(player);
            for (int i = 0; i < character.Bag.Size; i++)
            {
                ItemInstance item = character.Bag.Get(i);
                if (item != null && item.DefinitionId == definitionId)
                {
                    return i;
                }
            }
            return -1;
        }

        public ref ManaData Mana(EntityId id) => ref World.Mana.Get(id);

        public ref VitalsData Vitals(EntityId id) => ref World.Vitals.Get(id);

        public bool InCombat(EntityId id) => World.CombatStates.Get(id).InCombat;
    }
}
