using System.Globalization;
using System.Text;
using Game.Client.Localization;
using Game.Shared.Content;
using Game.Shared.Content.Items;
using Game.Shared.Protocol;
using Game.Shared.Rules;
using UnityEngine;

namespace Game.Client.UI.Widgets
{
    /// <summary>Builds tooltip texts for items and skills from content definitions and the player's state.</summary>
    public static class TooltipText
    {
        private const float PercentFactor = 100f;

        public static string Item(ClientContext ctx, ItemDefinition item, int count, string hintKey)
        {
            ILocalizationService text = ctx.Text;
            var sb = new StringBuilder();
            sb.Append("<b><color=#").Append(ColorUtility.ToHtmlStringRGB(HudPalette.ForRarity(item.Rarity))).Append('>')
                .Append(text.Get(item.NameKey)).Append("</color></b>\n");
            sb.Append(text.Get(UiKeys.RarityPrefix + item.Rarity)).Append('\n');
            EquipSlotComponent slot = item.Get<EquipSlotComponent>();
            if (slot != null)
            {
                sb.Append(text.Get("slot_type." + slot.SlotType));
                if (slot.Hands > 1)
                {
                    sb.Append(", ").Append(text.Get("ui.two_handed"));
                }
                sb.Append('\n');
            }
            ArmorComponent armor = item.Get<ArmorComponent>();
            if (armor != null)
            {
                sb.Append(text.Get("armor_type." + armor.ArmorType)).Append('\n');
            }
            WeaponComponent weapon = item.Get<WeaponComponent>();
            if (weapon != null)
            {
                sb.Append(text.Get("weapon_type." + weapon.WeaponType)).Append('\n');
            }
            StatsComponent stats = item.Get<StatsComponent>();
            if (stats != null)
            {
                foreach (StatModifier modifier in stats.Modifiers)
                {
                    string value = modifier.Op == ModifierOp.Add
                        ? StatFormat.Value(text, modifier.Stat, modifier.Value)
                        : text.Format(UiKeys.Percent, (modifier.Value * PercentFactor).ToString("0.#", CultureInfo.InvariantCulture));
                    sb.Append("<color=#7CFC7C>+").Append(value).Append(' ').Append(StatFormat.Name(text, modifier.Stat)).Append("</color>\n");
                }
            }
            ConsumableComponent consumable = item.Get<ConsumableComponent>();
            if (consumable != null)
            {
                sb.Append(text.Format("ui.restores", consumable.Amount.ToString("0", CultureInfo.InvariantCulture), text.Get(UiKeys.ResourcePrefix + consumable.Resource))).Append('\n');
            }
            if (item.RequiredLevel > 1)
            {
                sb.Append(text.Format("ui.required_level", item.RequiredLevel)).Append('\n');
            }
            if (item.ClassRestriction.Count > 0)
            {
                sb.Append(text.Get("ui.classes")).Append(':');
                foreach (string classId in item.ClassRestriction)
                {
                    sb.Append(' ').Append(text.Get(ctx.Content.Classes[classId].NameKey));
                }
                sb.Append('\n');
            }
            if (count > 1)
            {
                sb.Append(text.Format(UiKeys.Count, count)).Append('\n');
            }
            if (!string.IsNullOrEmpty(item.DescriptionKey))
            {
                sb.Append("<i>").Append(text.Get(item.DescriptionKey)).Append("</i>\n");
            }
            if (hintKey != null)
            {
                sb.Append("<color=#A0A0A0>").Append(text.Get(hintKey)).Append("</color>");
            }
            return sb.ToString().TrimEnd('\n');
        }

        public static string Skill(ClientContext ctx, SkillDefinition skill, SkillVariantKind kind, SkillStateSnapshot state)
        {
            ILocalizationService text = ctx.Text;
            SkillVariant variant = skill.GetVariant(kind);
            var sb = new StringBuilder();
            sb.Append("<b>").Append(text.Get(skill.NameKey)).Append("</b>");
            if (kind == SkillVariantKind.Empowered)
            {
                sb.Append(" — ").Append(text.Get(UiKeys.VariantEmpowered));
            }
            else if (kind == SkillVariantKind.Weakened)
            {
                sb.Append(" — ").Append(text.Get(UiKeys.VariantWeakened));
            }
            sb.Append('\n');
            foreach (CostEntry cost in variant.Costs)
            {
                string amount = cost.Mode == CostMode.PercentOfMax
                    ? text.Format(UiKeys.Percent, cost.Amount.ToString("0.#", CultureInfo.InvariantCulture))
                    : cost.Amount.ToString("0.#", CultureInfo.InvariantCulture);
                sb.Append(text.Format(UiKeys.CostLine, amount, text.Get(UiKeys.ResourcePrefix + cost.Resource))).Append('\n');
            }
            sb.Append(text.Format(UiKeys.CastLine, variant.CastTime.ToString("0.#", CultureInfo.InvariantCulture))).Append('\n');
            sb.Append(text.Format(UiKeys.CooldownLine, variant.Cooldown.ToString("0.#", CultureInfo.InvariantCulture))).Append('\n');
            sb.Append(text.Format(UiKeys.MasteryLine,
                (state.Mastery * PercentFactor).ToString("0", CultureInfo.InvariantCulture),
                (state.Efficiency * PercentFactor).ToString("0", CultureInfo.InvariantCulture))).Append('\n');
            if (!string.IsNullOrEmpty(skill.DescriptionKey))
            {
                sb.Append("<i>").Append(text.Get(skill.DescriptionKey)).Append("</i>");
            }
            return sb.ToString().TrimEnd('\n');
        }
    }
}
