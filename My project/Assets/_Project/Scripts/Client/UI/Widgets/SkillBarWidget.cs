using System;
using System.Collections.Generic;
using Game.Client.Input;
using Game.Client.Localization;
using Game.Shared.Content;
using Game.Shared.Protocol;
using Game.Shared.Rules;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Client.UI.Widgets
{
    /// <summary>
    /// Bottom-center skill panels (02, 7.7 А): rows × slots from config. Row 1 is filled with the class skills and bound to
    /// keys 1…=. Holding Shift/Ctrl previews the variant (mark ▲/▼, tooltip with its cost). A slot is tinted by the pool
    /// that is short: main mana, reserve mana or health (when the health cost would kill).
    /// </summary>
    public sealed class SkillBarWidget
    {
        private const string EmpoweredMark = "▲";
        private const string WeakenedMark = "▼";

        private readonly ClientContext _ctx;
        private readonly Tooltip _tooltip;
        private readonly List<SlotView> _slots = new List<SlotView>();
        private readonly string[] _skillBySlot;
        private readonly int _slotsPerRow;
        private int _hovered = -1;
        private SkillModifier _lastModifier;

        public SkillBarWidget(RectTransform zone, ClientContext ctx, Tooltip tooltip)
        {
            _ctx = ctx;
            _tooltip = tooltip;
            int rows = Math.Max(1, ctx.Content.Config.SkillBarRows);
            _slotsPerRow = Math.Max(1, ctx.Content.Config.SkillBarSlotsPerRow);
            _skillBySlot = new string[rows * _slotsPerRow];
            float step = HudLayout.SkillSlotSize + HudLayout.Gap;
            float rowWidth = step * _slotsPerRow - HudLayout.Gap;
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < _slotsPerRow; column++)
                {
                    int index = row * _slotsPerRow + column;
                    var slot = new SlotView($"SkillSlot{index}", zone, ctx.Font, HudLayout.SkillSlotSize);
                    UiFactory.Place(slot.Rect, new Vector2(0.5f, 0f), new Vector2(0f, 0f),
                        new Vector2(-rowWidth * 0.5f + column * step, row * step), new Vector2(HudLayout.SkillSlotSize, HudLayout.SkillSlotSize));
                    if (row == 0)
                    {
                        int key = column;
                        slot.SetHotkey(GameInput.SkillKey(ctx, key));
                        if (ctx.Settings != null)
                        {
                            ctx.Settings.Keys.KeysChanged += () => slot.SetHotkey(GameInput.SkillKey(ctx, key));
                        }
                    }
                    int captured = index;
                    slot.Bind(() => OnEnter(captured), OnExit, button => OnClick(captured, button));
                    _slots.Add(slot);
                }
            }
        }

        public int VisibleRowSlots => _slotsPerRow;

        /// <summary>Width of one row of slots in reference pixels (used to place the window buttons to its left).</summary>
        public float RowWidth => (HudLayout.SkillSlotSize + HudLayout.Gap) * _slotsPerRow - HudLayout.Gap;

        public string SkillAt(int index) => index >= 0 && index < _skillBySlot.Length ? _skillBySlot[index] : null;

        /// <summary>Puts the class skills into the first row (until drag & drop from the skill book, module 18).</summary>
        public void AssignClassSkills(ClassDefinition cls)
        {
            Array.Clear(_skillBySlot, 0, _skillBySlot.Length);
            for (int i = 0; i < cls.Skills.Count && i < _slotsPerRow; i++)
            {
                _skillBySlot[i] = cls.Skills[i].SkillId;
            }
            for (int i = 0; i < _slots.Count; i++)
            {
                string skillId = _skillBySlot[i];
                bool filled = skillId != null;
                _slots[i].SetFilled(filled);
                _slots[i].SetCaption(filled ? _ctx.Text.Get(_ctx.Content.Skills[skillId].NameKey) : string.Empty, HudPalette.Text);
                _slots[i].SetCooldown(0f);
                _slots[i].SetMark(string.Empty);
            }
        }

        public void Refresh(PlayerStateSnapshot state)
        {
            if (state == null)
            {
                return;
            }
            SkillModifier modifier = _ctx.Modifiers.Current;
            for (int i = 0; i < _slots.Count; i++)
            {
                string skillId = _skillBySlot[i];
                if (skillId == null)
                {
                    continue;
                }
                SkillDefinition skill = _ctx.Content.Skills[skillId];
                SkillStateSnapshot skillState = FindSkill(state, skillId);
                SkillVariantKind kind = Resolve(skill, skillState, modifier);
                SkillVariant variant = skill.GetVariant(kind);
                SlotView slot = _slots[i];
                slot.SetMark(kind == SkillVariantKind.Empowered ? EmpoweredMark : kind == SkillVariantKind.Weakened ? WeakenedMark : string.Empty);
                slot.SetCooldown(CooldownFraction(state, skill));
                slot.SetTint(ShortageColor(state, variant));
            }
            if (_hovered >= 0 && modifier != _lastModifier)
            {
                OnEnter(_hovered);
            }
            _lastModifier = modifier;
        }

        private static SkillVariantKind Resolve(SkillDefinition skill, SkillStateSnapshot state, SkillModifier modifier)
        {
            if (modifier == SkillModifier.Shift && state.EmpoweredAvailable)
            {
                return SkillVariantKind.Empowered;
            }
            if (modifier == SkillModifier.Ctrl && state.WeakenedAvailable)
            {
                return SkillVariantKind.Weakened;
            }
            return SkillVariantKind.Default;
        }

        private static float CooldownFraction(PlayerStateSnapshot state, SkillDefinition skill)
        {
            float fraction = 0f;
            foreach (SkillCooldownSnapshot cooldown in state.Cooldowns)
            {
                if (cooldown.SkillId == skill.Id && cooldown.Duration > 0f)
                {
                    fraction = cooldown.Remaining / cooldown.Duration;
                }
            }
            if (skill.UsesGlobalCooldown && state.GlobalCooldownDuration > 0f)
            {
                fraction = Math.Max(fraction, state.GlobalCooldownRemaining / state.GlobalCooldownDuration);
            }
            return fraction;
        }

        private static Color ShortageColor(PlayerStateSnapshot state, SkillVariant variant)
        {
            CostTotals totals = CostMath.Sum(variant.Costs, state.MaxMainMana, state.MaxReserveMana, state.MaxHealth);
            if (state.MainMana < totals.Main)
            {
                return Dim(HudPalette.MainMana);
            }
            if (!CostMath.CanPay(totals, state.MainMana, state.ReserveMana))
            {
                return Dim(HudPalette.ReserveMana);
            }
            if (totals.Health > 0f && totals.Health >= state.Health)
            {
                return Dim(HudPalette.Health);
            }
            return HudPalette.SlotFilled;
        }

        private static Color Dim(Color color) => new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.5f, 0.95f);

        private static SkillStateSnapshot FindSkill(PlayerStateSnapshot state, string skillId)
        {
            foreach (SkillStateSnapshot skill in state.Skills)
            {
                if (skill.SkillId == skillId)
                {
                    return skill;
                }
            }
            return default;
        }

        private void OnEnter(int index)
        {
            _hovered = index;
            string skillId = _skillBySlot[index];
            PlayerStateSnapshot state = _ctx.State.Player;
            if (skillId == null || state == null)
            {
                return;
            }
            SkillDefinition skill = _ctx.Content.Skills[skillId];
            SkillStateSnapshot skillState = FindSkill(state, skillId);
            _tooltip.Show(TooltipText.Skill(_ctx, skill, Resolve(skill, skillState, _ctx.Modifiers.Current), skillState));
        }

        private void OnExit()
        {
            _hovered = -1;
            _tooltip.Hide();
        }

        private void OnClick(int index, PointerEventData.InputButton button)
        {
            string skillId = _skillBySlot[index];
            if (skillId != null && button == PointerEventData.InputButton.Left)
            {
                _ctx.Commands?.UseSkill(skillId, default, _ctx.Modifiers.Current);
            }
        }

    }
}
