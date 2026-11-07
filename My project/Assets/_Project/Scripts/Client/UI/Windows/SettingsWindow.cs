using System;
using Game.Client.Localization;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Windows
{
    /// <summary>
    /// Settings placeholder (full menu is module 17) with a developer tool: switch the character class to try
    /// class-dependent skills and equipment rules.
    /// </summary>
    public sealed class SettingsWindow : HudWindow
    {
        public SettingsWindow(HudWindowDto definition, RectTransform parent, Canvas canvas, ClientContext ctx, Tooltip tooltip,
            Action<HudWindow> focus, Action<HudWindow> close) : base(definition, parent, canvas, ctx, tooltip, focus, close)
        {
            RectTransform list = VerticalList("List", Content);
            Text info = Row("Info", list);
            info.text = ctx.Text.Get(UiKeys.SettingsInfo);
            info.color = HudPalette.TextDim;
            Text header = Row("SwitchHeader", list);
            header.text = ctx.Text.Get(UiKeys.SwitchClass);
            header.color = HudPalette.Cast;
            foreach (ClassDefinition cls in ctx.Content.Classes.Values)
            {
                string classId = cls.Id;
                Button button = UiFactory.Button($"Class_{classId}", list, ctx.Font, ctx.Text.Get(cls.NameKey), () => Ctx.RequestClassSwitch(classId));
                LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
                layout.minHeight = HudLayout.MenuButtonHeight;
                layout.preferredHeight = HudLayout.MenuButtonHeight;
            }
        }

        public override void Refresh(PlayerStateSnapshot state)
        {
        }
    }
}
