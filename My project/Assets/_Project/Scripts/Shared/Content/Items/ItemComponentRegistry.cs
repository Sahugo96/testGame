using System.Collections.Generic;
using Game.Shared.Content.Dto;
using Game.Shared.Rules;

namespace Game.Shared.Content.Items
{
    /// <summary>
    /// Registry of item component types and their dependencies (01, 7.10: new behaviour = registration, not core edits).
    /// </summary>
    public sealed class ItemComponentRegistry
    {
        private readonly Dictionary<string, ItemComponentFactory> _factories = new Dictionary<string, ItemComponentFactory>();
        private readonly Dictionary<string, string[]> _requires = new Dictionary<string, string[]>();

        public void Register(string type, string[] requires, ItemComponentFactory factory)
        {
            _factories[type] = factory;
            _requires[type] = requires ?? new string[0];
        }

        public bool IsRegistered(string type) => type != null && _factories.ContainsKey(type);

        public IReadOnlyList<string> GetRequirements(string type) =>
            _requires.TryGetValue(type, out string[] requires) ? requires : new string[0];

        public ItemComponent Create(ItemComponentDto dto, string context, List<string> errors)
        {
            if (!IsRegistered(dto.Type))
            {
                errors.Add($"{context}: unknown item component type '{dto.Type}'");
                return null;
            }
            return _factories[dto.Type](dto, context, errors);
        }

        /// <summary>Registry with the built-in components.</summary>
        public static ItemComponentRegistry CreateDefault()
        {
            var registry = new ItemComponentRegistry();
            string[] needsEquipment = { ItemComponentTypes.Equipment };
            string[] needsSlot = { ItemComponentTypes.Equipment, ItemComponentTypes.EquipSlot };

            registry.Register(ItemComponentTypes.Equipment, null, (dto, ctx, err) => new EquipmentComponent());
            registry.Register(ItemComponentTypes.EquipSlot, needsEquipment, (dto, ctx, err) =>
            {
                if (string.IsNullOrEmpty(dto.SlotType))
                {
                    err.Add($"{ctx}: EquipSlot.SlotType is empty");
                }
                return new EquipSlotComponent { SlotType = dto.SlotType, Hands = dto.Hands > 0 ? dto.Hands : 1 };
            });
            registry.Register(ItemComponentTypes.Armor, needsSlot, (dto, ctx, err) => new ArmorComponent { ArmorType = dto.ArmorType });
            registry.Register(ItemComponentTypes.Weapon, needsSlot, (dto, ctx, err) => new WeaponComponent { WeaponType = dto.WeaponType });
            registry.Register(ItemComponentTypes.Stats, needsEquipment, (dto, ctx, err) =>
            {
                var component = new StatsComponent();
                foreach (StatModifierDto stat in dto.Stats ?? new StatModifierDto[0])
                {
                    component.Modifiers.Add(new StatModifier(
                        EnumParser.Parse<StatId>(stat.Stat, ctx, err),
                        EnumParser.Parse<ModifierOp>(stat.Op, ctx, err),
                        stat.Value,
                        null));
                }
                return component;
            });
            registry.Register(ItemComponentTypes.Consumable, null, (dto, ctx, err) => new ConsumableComponent
            {
                EffectKey = dto.Effect,
                StatusId = dto.StatusId,
                Resource = string.IsNullOrEmpty(dto.Resource) ? ResourceKind.Health : EnumParser.Parse<ResourceKind>(dto.Resource, ctx, err),
                Amount = dto.Amount,
                CooldownGroup = dto.CooldownGroup,
                Cooldown = dto.Cooldown,
            });
            registry.Register(ItemComponentTypes.Stackable, null, (dto, ctx, err) =>
            {
                if (dto.MaxStack < 1)
                {
                    err.Add($"{ctx}: Stackable.MaxStack must be >= 1");
                }
                return new StackableComponent { MaxStack = dto.MaxStack };
            });
            registry.Register(ItemComponentTypes.Upgradeable, needsEquipment, (dto, ctx, err) =>
            {
                if (string.IsNullOrEmpty(dto.Rules))
                {
                    err.Add($"{ctx}: Upgradeable.Rules is empty");
                }
                return new UpgradeableComponent { RulesId = dto.Rules };
            });
            return registry;
        }
    }
}
