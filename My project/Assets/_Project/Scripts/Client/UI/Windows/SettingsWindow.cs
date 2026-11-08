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
    /// Settings placeholder (full menu is module 17) with a developer tool (switch the character class to try
    /// class-dependent skills and equipment rules) and "leave to the start menu".
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
            _autoLoot = UiFactory.Button("AutoLoot", list, ctx.Font, string.Empty, ToggleAutoLoot);
            LayoutElement autoLayout = _autoLoot.gameObject.AddComponent<LayoutElement>();
            autoLayout.minHeight = HudLayout.MenuButtonHeight;
            autoLayout.preferredHeight = HudLayout.MenuButtonHeight;
            _autoLootLabel = _autoLoot.GetComponentInChildren<Text>();
            Button tabs = UiFactory.Button("ChatTabs", list, ctx.Font, ctx.Text.Get(UiKeys.ChatTabsSettingsLong), () => Ctx.RequestWindow(WindowFactory.ChatTabs));
            LayoutElement tabsLayout = tabs.gameObject.AddComponent<LayoutElement>();
            tabsLayout.minHeight = HudLayout.MenuButtonHeight;
            tabsLayout.preferredHeight = HudLayout.MenuButtonHeight;
            Button leave = UiFactory.Button("Leave", list, ctx.Font, ctx.Text.Get(UiKeys.LeaveGame), () => Ctx.RequestLeave());
            LayoutElement leaveLayout = leave.gameObject.AddComponent<LayoutElement>();
            leaveLayout.minHeight = HudLayout.MenuButtonHeight;
            leaveLayout.preferredHeight = HudLayout.MenuButtonHeight;
        }

        private readonly Button _autoLoot;
        private readonly Text _autoLootLabel;
        private bool _autoLootOn;

        /// <summary>Account setting (question 77): auto-pickup of drops or manual pickup from the ground.</summary>
        private void ToggleAutoLoot() => Ctx.Commands?.SetAutoLoot(!_autoLootOn);

        public override void Refresh(PlayerStateSnapshot state)
        {
            _autoLootOn = state.AutoLoot;
            _autoLootLabel.text = Ctx.Text.Get(_autoLootOn ? UiKeys.AutoLootOn : UiKeys.AutoLootOff);
        }
    }
}
