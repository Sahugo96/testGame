using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Content;
using Game.Shared.Protocol;

namespace Game.Server.Systems
{
    /// <summary>
    /// Experience and levels (module 9 basics). Level table is data (levels.json); each level gives
    /// AttributePointsPerLevel attribute points (question 52) and one class point (question 62).
    /// On level up health and mana are restored (ДЕФОЛТ, question 67).
    /// </summary>
    public sealed class ProgressionSystem
    {
        private const int ClassPointsPerLevel = 1;

        private readonly ServerContext _ctx;
        private readonly StatSystem _stats;

        public ProgressionSystem(ServerContext ctx, StatSystem stats)
        {
            _ctx = ctx;
            _stats = stats;
        }

        public void AwardExperience(PlayerCharacter player, int amount)
        {
            if (amount <= 0 || player.Level >= _ctx.Content.MaxLevel)
            {
                return;
            }
            player.Experience += amount;
            player.Outbox.Notifications.Add(new NotificationMessage { TextKey = NotificationKeys.ExperienceGained, Number = amount, IsPositive = true });
            bool leveled = false;
            int needed = _ctx.Content.ExperienceFor(player.Level);
            while (needed > 0 && player.Experience >= needed)
            {
                player.Experience -= needed;
                player.Level++;
                player.AttributePoints += _ctx.Content.Config.AttributePointsPerLevel;
                player.ClassPoints += ClassPointsPerLevel;
                leveled = true;
                player.Outbox.Notifications.Add(new NotificationMessage { TextKey = NotificationKeys.LevelUp, Number = player.Level, IsPositive = true });
                needed = _ctx.Content.ExperienceFor(player.Level);
            }
            if (player.Level >= _ctx.Content.MaxLevel)
            {
                player.Experience = 0;
            }
            if (leveled)
            {
                _stats.Recalculate(player);
                RestoreResources(player);
            }
        }

        public void SpendAttributePoint(PlayerCharacter player, SpendAttributePointCommand command)
        {
            if (player.AttributePoints <= 0)
            {
                CombatEvents.Reject(player, RejectReasons.NoAttributePoints);
                return;
            }
            if (command.Attribute != StatId.Strength && command.Attribute != StatId.Intelligence && command.Attribute != StatId.Stamina)
            {
                CombatEvents.Reject(player, RejectReasons.InvalidTarget);
                return;
            }
            player.AttributePoints--;
            player.Attributes.TryGetValue(command.Attribute, out float value);
            player.Attributes[command.Attribute] = value + 1f;
            _stats.Recalculate(player);
        }

        private void RestoreResources(PlayerCharacter player)
        {
            ref VitalsData vitals = ref _ctx.World.Vitals.Get(player.EntityId);
            vitals.Health = vitals.MaxHealth;
            ref ManaData mana = ref _ctx.World.Mana.Get(player.EntityId);
            mana.Main = mana.MaxMain;
            mana.Reserve = mana.MaxReserve;
            mana.MainLocked = false;
        }
    }
}
