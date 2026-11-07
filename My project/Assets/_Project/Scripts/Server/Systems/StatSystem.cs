using System;
using System.Collections.Generic;
using Game.Shared.Content.Dto;
using Game.Shared.Core;
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
        private readonly StatSheet _mobSheet = new StatSheet();

        public StatSystem(ServerContext ctx) => _ctx = ctx;

        /// <summary>Modifiers from other sources of an entity (statuses, 03 7.12); null — none.</summary>
        public Func<EntityId, IEnumerable<StatModifier>> ExtraModifiers;

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
                ItemDefinition definition = _ctx.Content.Items[item.DefinitionId];
                float upgrade = UpgradeMultiplier(definition, item.UpgradeLevel);
                StatsComponent stats = definition.Get<StatsComponent>();
                if (stats != null)
                {
                    foreach (StatModifier template in stats.Modifiers)
                    {
                        AddScaled(sheet, template, upgrade, item.SourceId);
                    }
                }
                // Rolled affixes grow with the upgrade level like base stats (04, 7.9 / 7.14.1).
                foreach (StatModifier affix in item.Affixes)
                {
                    AddScaled(sheet, affix, upgrade, item.SourceId);
                }
                // Inserted ingredients become part of the item (04, 7.9): their stats count while it is worn.
                foreach (ItemSocket socket in item.Sockets)
                {
                    if (socket.IngredientId == null || !_ctx.Content.Items.TryGetValue(socket.IngredientId, out ItemDefinition ingredient))
                    {
                        continue;
                    }
                    SocketIngredientComponent component = ingredient.Get<SocketIngredientComponent>();
                    foreach (StatModifier template in component?.Modifiers ?? new List<StatModifier>())
                    {
                        sheet.AddModifier(new StatModifier(template.Stat, template.Op, template.Value, item.SourceId));
                    }
                }
            }
            AddExtra(sheet, player.EntityId);
            sheet.SetCap(StatId.Defense, _ctx.Content.Config.DefenseCap);
            sheet.SetCap(StatId.CritChance, _ctx.Content.Config.CritChanceCap);
            Apply(player);
        }

        /// <summary>Mobs: defense and health regeneration from the mob definition plus status modifiers (e.g. armour break).</summary>
        public void RecalculateMob(EntityId id)
        {
            ServerWorld world = _ctx.World;
            if (!world.Mobs.Has(id) || !_ctx.Content.Mobs.TryGetValue(world.Identities.Get(id).DefinitionId, out MobDto mob))
            {
                return;
            }
            _mobSheet.Clear();
            _mobSheet.SetBase(StatId.Defense, mob.Defense);
            _mobSheet.SetBase(StatId.HealthRegen, mob.HealthRegen);
            AddExtra(_mobSheet, id);
            _mobSheet.SetCap(StatId.Defense, _ctx.Content.Config.DefenseCap);
            ref VitalsData vitals = ref world.Vitals.Get(id);
            vitals.Defense = Math.Max(0f, _mobSheet.Get(StatId.Defense));
            vitals.HealthRegen = _mobSheet.Get(StatId.HealthRegen);
        }

        private void AddExtra(StatSheet sheet, EntityId id)
        {
            if (ExtraModifiers == null)
            {
                return;
            }
            foreach (StatModifier modifier in ExtraModifiers(id))
            {
                sheet.AddModifier(modifier);
            }
        }

        private static void AddScaled(StatSheet sheet, StatModifier template, float upgrade, string sourceId)
        {
            float value = template.Op == ModifierOp.Multiply ? template.Value : template.Value * upgrade;
            sheet.AddModifier(new StatModifier(template.Stat, template.Op, value, sourceId));
        }

        /// <summary>Upgrade level strengthens the item's additive and percent bonuses (04, 7.14.1).</summary>
        public float UpgradeMultiplier(ItemDefinition definition, int upgradeLevel)
        {
            UpgradeableComponent upgradeable = definition.Get<UpgradeableComponent>();
            if (upgradeable == null || upgradeLevel <= 0 || !_ctx.Content.UpgradeRules.TryGetValue(upgradeable.RulesId, out var rule))
            {
                return 1f;
            }
            return UpgradeMath.StatMultiplier(upgradeLevel, rule.StatBonusPerLevel);
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
