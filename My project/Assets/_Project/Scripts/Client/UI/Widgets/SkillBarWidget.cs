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
    /// keys 1…=. Holding Shift/Ctrl previews the variant (mark ▲/▼, tooltip with its cost). A slot and its picture are
    /// tinted by the pool that is short: main mana, reserve mana or health (when the health cost would kill).
    /// </summary>
    public sealed class SkillBarWidget
    {
        private const string EmpoweredMark = "▲";
        private const string WeakenedMark = "▼";
        /// <summary>How much of the short pool's colour goes into the slot frame and into the skill picture.</summary>
        private const float ShortageFrameShade = 0.5f;
        private const float ShortageIconShade = 0.85f;

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
                    slot.BindDrag(() => BeginDrag(captured), () => EndDrag(captured), () => Drop(captured));
                    _slots.Add(slot);
                }
            }
        }

        public int VisibleRowSlots => _slotsPerRow;

        /// <summary>Width of one row of slots in reference pixels (used to place the window buttons to its left).</summary>
        public float RowWidth => (HudLayout.SkillSlotSize + HudLayout.Gap) * _slotsPerRow - HudLayout.Gap;

        public string SkillAt(int index) => index >= 0 && index < _skillBySlot.Length ? _skillBySlot[index] : null;

        /// <summary>Puts the class skills into the first row: only until the server's layout arrives (module 18).</summary>
        public void AssignClassSkills(ClassDefinition cls)
        {
            Array.Clear(_skillBySlot, 0, _skillBySlot.Length);
            for (int i = 0; i < cls.Skills.Count && i < _slotsPerRow; i++)
            {
                _skillBySlot[i] = cls.Skills[i].SkillId;
            }
            ShowSkills();
        }

        /// <summary>The character's layout from the server (module 18, 02 7.7 А); unknown skills are shown as empty slots.</summary>
        public void ApplyPanels(PanelsMessage panels)
        {
            Array.Clear(_skillBySlot, 0, _skillBySlot.Length);
            for (int i = 0; i < panels.SkillBar.Length && i < _skillBySlot.Length; i++)
            {
                string skillId = panels.SkillBar[i];
                _skillBySlot[i] = !string.IsNullOrEmpty(skillId) && _ctx.Content.Skills.ContainsKey(skillId) ? skillId : null;
            }
            ShowSkills();
        }

        private void ShowSkills()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                string skillId = _skillBySlot[i];
                bool filled = skillId != null;
                SkillDefinition skill = filled ? _ctx.Content.Skills[skillId] : null;
                _slots[i].SetFilled(filled);
                _slots[i].SetCaption(filled ? _ctx.Text.Get(skill.NameKey) : string.Empty, HudPalette.Text);
                _slots[i].SetIcon(filled ? _ctx.Icons.Skill(skill.Icon) : null);
                _slots[i].SetIconTint(Color.white);
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
                bool shortage = ShortPool(state, variant, out Color pool);
                slot.SetTint(shortage ? Shade(pool, ShortageFrameShade) : HudPalette.SlotFilled);
                slot.SetIconTint(shortage ? Shade(pool, ShortageIconShade) : Color.white);
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

        /// <summary>True when the variant cannot be paid; `pool` is the colour of the pool that is short.</summary>
        private static bool ShortPool(PlayerStateSnapshot state, SkillVariant variant, out Color pool)
        {
            CostTotals totals = CostMath.Sum(variant.Costs, state.MaxMainMana, state.MaxReserveMana, state.MaxHealth);
            if (state.MainMana < totals.Main)
            {
                pool = HudPalette.MainMana;
                return true;
            }
            if (!CostMath.CanPay(totals, state.MainMana, state.ReserveMana))
            {
                pool = HudPalette.ReserveMana;
                return true;
            }
            if (totals.Health > 0f && totals.Health >= state.Health)
            {
                pool = HudPalette.Health;
                return true;
            }
            pool = default;
            return false;
        }

        private static Color Shade(Color color, float shade) => new Color(color.r * shade, color.g * shade, color.b * shade, HudPalette.SlotFilled.a);

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

        // ---- Drag and drop (module 18): from the skill book onto a slot, between slots (swap), out of the panel (clear) ----

        private void BeginDrag(int index)
        {
            string skillId = _skillBySlot[index];
            if (skillId != null)
            {
                _ctx.Drag.Begin(DragKind.SkillSlot, skillId, index);
                _tooltip.Show(_ctx.Text.Format(UiKeys.DragSkill, _ctx.Text.Get(_ctx.Content.Skills[skillId].NameKey)));
            }
        }

        private void EndDrag(int index)
        {
            if (_ctx.Drag.Kind == DragKind.SkillSlot && _ctx.Drag.Index == index && !_ctx.Drag.Dropped)
            {
                _ctx.Commands?.SetSkillSlot(index, null);
            }
            _ctx.Drag.End();
            _tooltip.Hide();
        }

        private void Drop(int index)
        {
            DragDropService drag = _ctx.Drag;
            if (_ctx.Commands == null)
            {
                return;
            }
            if (drag.Kind == DragKind.Skill)
            {
                _ctx.Commands.SetSkillSlot(index, drag.Id);
                drag.MarkDropped();
            }
            else if (drag.Kind == DragKind.SkillSlot)
            {
                drag.MarkDropped();
                if (drag.Index != index)
                {
                    string here = _skillBySlot[index];
                    _ctx.Commands.SetSkillSlot(index, drag.Id);
                    _ctx.Commands.SetSkillSlot(drag.Index, here);
                }
            }
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
