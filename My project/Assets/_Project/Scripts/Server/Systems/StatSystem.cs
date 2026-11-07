using System;
using Game.Server.Items;
using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Content;
using Game.Shared.Content.Items;
using Game.Shared.Rules;

namespace Game.Server.Systems
{
    /// <summary>
    /// Rebuilds a character's stats from class base values, primary attributes (question 52) and equipped items
    /// (SourceId = item instance), then pushes maxima into simulation data. Current values are clipped to the new
    /// maximum and never raised (02, 7.7 В).
    /// </summary>
    public sealed class StatSystem
    {
        private readonly ServerContext _ctx;

        public StatSystem(ServerContext ctx) => _ctx = ctx;

        public void Recalculate(PlayerCharacter player)
        {
            StatSheet sheet = player.Stats;
            sheet.Clear();
            foreach (var pair in player.Class.BaseStats)
            {
                sheet.SetBase(pair.Key, pair.Value);
            }
            foreach (var pair in player.Attributes)
            {
                sheet.AddBase(pair.Key, pair.Value);
            }
            foreach (AttributeEffect effect in player.Class.AttributeEffects)
            {
                player.Attributes.TryGetValue(effect.Attribute, out float points);
                sheet.AddBase(effect.Stat, points * effect.PerPoint);
            }
            foreach (ItemInstance item in player.Equipment.Values)
            {
                StatsComponent stats = _ctx.Content.Items[item.DefinitionId].Get<StatsComponent>();
                if (stats == null)
                {
                    continue;
                }
                foreach (StatModifier template in stats.Modifiers)
                {
                    sheet.AddModifier(new StatModifier(template.Stat, template.Op, template.Value, item.SourceId));
                }
            }
            sheet.SetCap(StatId.Defense, _ctx.Content.Config.DefenseCap);
            sheet.SetCap(StatId.CritChance, _ctx.Content.Config.CritChanceCap);
            Apply(player);
        }

        private void Apply(PlayerCharacter player)
        {
            ServerWorld world = _ctx.World;
            ref VitalsData vitals = ref world.Vitals.Get(player.EntityId);
            vitals.MaxHealth = player.Stats.Get(StatId.MaxHealth);
            vitals.Health = Math.Min(vitals.Health, vitals.MaxHealth);
            vitals.HealthRegen = player.Stats.Get(StatId.HealthRegen);
            vitals.Defense = player.Stats.Get(StatId.Defense);

            ref ManaData mana = ref world.Mana.Get(player.EntityId);
            mana.MaxMain = player.Stats.Get(StatId.MaxMainMana);
            mana.MaxReserve = player.Stats.Get(StatId.MaxReserveMana);
            mana.Main = Math.Min(mana.Main, mana.MaxMain);
            mana.Reserve = Math.Min(mana.Reserve, mana.MaxReserve);
            mana.MainRegen = player.Stats.Get(StatId.MainManaRegen);
            ResourceSystem.UpdateLock(ref mana);
        }
    }
}
