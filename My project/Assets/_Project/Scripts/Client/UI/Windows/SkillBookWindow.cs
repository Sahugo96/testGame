using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Client.Localization;
using Game.Client.UI.Widgets;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Windows
{
    /// <summary>Skill book (02, 7.13): class skills with mastery, efficiency and unlocked variants; tooltip with details.</summary>
    public sealed class SkillBookWindow : HudWindow
    {
        private const float PercentFactor = 100f;

        private readonly RectTransform _list;
        private readonly List<Text> _rows = new List<Text>();

        public SkillBookWindow(HudWindowDto definition, RectTransform parent, Canvas canvas, ClientContext ctx, Tooltip tooltip,
            Action<HudWindow> focus, Action<HudWindow> close) : base(definition, parent, canvas, ctx, tooltip, focus, close)
        {
            _list = VerticalList("List", Content);
        }

        public override void Refresh(PlayerStateSnapshot state)
        {
            while (_rows.Count < state.Skills.Length)
            {
                Text row = Row("Skill", _list);
                row.raycastTarget = true;
                HoverTrigger hover = row.gameObject.AddComponent<HoverTrigger>();
                int index = _rows.Count;
                hover.Entered = () => ShowTooltip(index);
                hover.Exited = Tooltip.Hide;
                // Module 18: drag a skill onto the skill panel.
                DragTrigger drag = row.gameObject.AddComponent<DragTrigger>();
                drag.Began = () => BeginDrag(index);
                drag.Ended = EndDrag;
                _rows.Add(row);
            }
            for (int i = 0; i < _rows.Count; i++)
            {
                bool visible = i < state.Skills.Length;
                _rows[i].gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }
                SkillStateSnapshot skill = state.Skills[i];
                string variants = (skill.EmpoweredAvailable ? Ctx.Text.Get(UiKeys.VariantEmpowered) + " " : string.Empty)
                    + (skill.WeakenedAvailable ? Ctx.Text.Get(UiKeys.VariantWeakened) : string.Empty);
                _rows[i].text = Ctx.Text.Get(Ctx.Content.Skills[skill.SkillId].NameKey) + " — "
                    + Ctx.Text.Format(UiKeys.MasteryLine,
                        (skill.Mastery * PercentFactor).ToString("0", CultureInfo.InvariantCulture),
                        (skill.Efficiency * PercentFactor).ToString("0", CultureInfo.InvariantCulture))
                    + (variants.Length > 0 ? "  [" + variants.Trim() + "]" : string.Empty);
            }
        }

        private void BeginDrag(int index)
        {
            PlayerStateSnapshot state = Ctx.State.Player;
            if (state == null || index >= state.Skills.Length)
            {
                return;
            }
            string skillId = state.Skills[index].SkillId;
            Ctx.Drag.Begin(DragKind.Skill, skillId, -1);
            Tooltip.Show(Ctx.Text.Format(UiKeys.DragSkill, Ctx.Text.Get(Ctx.Content.Skills[skillId].NameKey)));
        }

        private void EndDrag()
        {
            Ctx.Drag.End();
            Tooltip.Hide();
        }

        private void ShowTooltip(int index)
        {
            PlayerStateSnapshot state = Ctx.State.Player;
            if (state == null || index >= state.Skills.Length)
            {
                return;
            }
            SkillStateSnapshot skill = state.Skills[index];
            Tooltip.Show(TooltipText.Skill(Ctx, Ctx.Content.Skills[skill.SkillId], SkillVariantKind.Default, skill));
        }
    }
}
