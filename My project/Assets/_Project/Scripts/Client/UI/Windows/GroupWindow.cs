using System;
using System.Text;
using Game.Client.Localization;
using Game.Client.UI.Widgets;
using Game.Shared.Content.Dto;
using Game.Shared.Core;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Windows
{
    /// <summary>
    /// Group window (06 7.8, module 14): members by join order with the leader mark, leader actions (make leader, kick),
    /// leave, the loot mode with proposals for the other modes, the vote state and the loot log.
    /// </summary>
    public sealed class GroupWindow : HudWindow
    {
        private static readonly Vector2 Size = new Vector2(500f, 640f);
        private const float ActionWidth = 110f;
        private const int LogLines = 6;
        private static readonly LootMode[] Modes = { LootMode.PickedUp, LootMode.Leader, LootMode.RoundRobin };

        private readonly Text _hint;
        private readonly Text _mode;
        private readonly RectTransform[] _rows;
        private readonly Text[] _names;
        private readonly Button[] _promote;
        private readonly Button[] _kick;
        private readonly EntityId[] _ids;
        private readonly Button _leave;
        private readonly Text _proposeTitle;
        private readonly Button[] _propose;
        private readonly Text _vote;
        private readonly Text _log;
        private readonly StringBuilder _sb = new StringBuilder();

        public GroupWindow(HudWindowDto definition, RectTransform parent, Canvas canvas, ClientContext ctx, Tooltip tooltip,
            Action<HudWindow> focus, Action<HudWindow> close) : base(definition, parent, canvas, ctx, tooltip, focus, close, Size)
        {
            float width = Size.x - HudLayout.Margin * 2f;
            float y = 0f;
            _hint = Line("Hint", width, ref y, HudPalette.TextDim);
            _mode = Line("Mode", width, ref y, HudPalette.Cast);

            int max = ctx.Content.Config.GroupMaxSize;
            _rows = new RectTransform[max];
            _names = new Text[max];
            _promote = new Button[max];
            _kick = new Button[max];
            _ids = new EntityId[max];
            for (int i = 0; i < max; i++)
            {
                int index = i;
                RectTransform row = UiFactory.Place(UiFactory.Rect($"Member_{i}", Content), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, y), new Vector2(width, HudLayout.MenuButtonHeight));
                _names[i] = UiFactory.Label("Name", row, ctx.Font, HudLayout.FontNormal, TextAnchor.MiddleLeft, HudPalette.Text);
                UiFactory.Place(_names[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                    new Vector2(width - ActionWidth * 2f - HudLayout.Gap * 2f, HudLayout.MenuButtonHeight));
                _promote[i] = ActionButton(row, UiKeys.GroupPromote, 1, () => Ctx.Commands?.PromoteToLeader(_ids[index]));
                _kick[i] = ActionButton(row, UiKeys.GroupKick, 0, () => Ctx.Commands?.KickFromGroup(_ids[index]));
                _rows[i] = row;
                y -= HudLayout.MenuButtonHeight + HudLayout.Gap;
            }

            _leave = UiFactory.Button("Leave", Content, ctx.Font, ctx.Text.Get(UiKeys.GroupLeave), () => Ctx.Commands?.LeaveGroup());
            UiFactory.Place((RectTransform)_leave.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y),
                new Vector2(ActionWidth * 2f, HudLayout.MenuButtonHeight));
            y -= HudLayout.MenuButtonHeight + HudLayout.Gap * 3f;

            _proposeTitle = Line("ProposeTitle", width, ref y, HudPalette.Text);
            _proposeTitle.text = ctx.Text.Get(UiKeys.LootPropose);
            _propose = new Button[Modes.Length];
            float modeWidth = (width - HudLayout.Gap * (Modes.Length - 1)) / Modes.Length;
            for (int i = 0; i < Modes.Length; i++)
            {
                LootMode mode = Modes[i];
                _propose[i] = UiFactory.Button($"Propose_{mode}", Content, ctx.Font, EntityNames.LootMode(ctx.Text, mode), () => Ctx.Commands?.ProposeLootMode(mode));
                UiFactory.Place((RectTransform)_propose[i].transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(i * (modeWidth + HudLayout.Gap), y), new Vector2(modeWidth, HudLayout.MenuButtonHeight));
            }
            y -= HudLayout.MenuButtonHeight + HudLayout.Gap;
            _vote = Line("Vote", width, ref y, HudPalette.Warning);
            y -= HudLayout.Gap * 2f;

            Text logTitle = Line("LogTitle", width, ref y, HudPalette.Text);
            logTitle.text = ctx.Text.Get(UiKeys.LootLog);
            _log = UiFactory.Label("Log", Content, ctx.Font, HudLayout.FontSmall, TextAnchor.UpperLeft, HudPalette.TextDim);
            UiFactory.Place(_log.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(width, HudLayout.RowHeight * LogLines));
        }

        public override void Refresh(PlayerStateSnapshot state)
        {
            GroupSnapshot group = state.Group;
            bool inGroup = group != null && group.InGroup;
            _hint.text = inGroup ? string.Empty : Ctx.Text.Get(UiKeys.GroupNone);
            _mode.text = inGroup ? Ctx.Text.Format(UiKeys.LootModeLine, EntityNames.LootMode(Ctx.Text, group.LootMode)) : string.Empty;
            bool isLeader = inGroup && group.LeaderId == state.EntityId;
            int count = inGroup ? group.Members.Length : 0;
            for (int i = 0; i < _rows.Length; i++)
            {
                bool shown = i < count;
                _rows[i].gameObject.SetActive(shown);
                if (!shown)
                {
                    continue;
                }
                EntityId member = group.Members[i];
                _ids[i] = member;
                string name = EntityNames.Of(Ctx, member);
                _names[i].text = member == group.LeaderId ? Ctx.Text.Format(UiKeys.GroupLeaderName, name) : name;
                bool canManage = isLeader && member != state.EntityId;
                _promote[i].gameObject.SetActive(canManage);
                _kick[i].gameObject.SetActive(canManage);
            }
            _leave.gameObject.SetActive(inGroup);
            _proposeTitle.gameObject.SetActive(inGroup);
            for (int i = 0; i < _propose.Length; i++)
            {
                _propose[i].gameObject.SetActive(inGroup);
                _propose[i].interactable = inGroup && !group.VoteActive && group.ProposeCooldownRemaining <= 0f && Modes[i] != group.LootMode;
            }
            _vote.text = VoteLine(group, inGroup);
            _log.text = LogText(group, inGroup);
        }

        private string VoteLine(GroupSnapshot group, bool inGroup)
        {
            if (!inGroup)
            {
                return string.Empty;
            }
            if (group.VoteActive)
            {
                return Ctx.Text.Format(UiKeys.LootVoteStatus, EntityNames.LootMode(Ctx.Text, group.VoteMode), group.VotesFor.Length,
                    group.Members.Length, Mathf.CeilToInt(group.VoteRemaining));
            }
            return group.ProposeCooldownRemaining > 0f ? Ctx.Text.Format(UiKeys.LootVoteCooldown, Mathf.CeilToInt(group.ProposeCooldownRemaining)) : string.Empty;
        }

        private string LogText(GroupSnapshot group, bool inGroup)
        {
            if (!inGroup || group.Log.Length == 0)
            {
                return inGroup ? Ctx.Text.Get(UiKeys.Empty) : string.Empty;
            }
            _sb.Clear();
            for (int i = group.Log.Length - 1; i >= 0 && i >= group.Log.Length - LogLines; i--)
            {
                GroupLogEntry entry = group.Log[i];
                string item = Ctx.Content.Items.TryGetValue(entry.ItemDefinitionId, out var definition) ? Ctx.Text.Get(definition.NameKey) : entry.ItemDefinitionId;
                _sb.Append(Ctx.Text.Format(UiKeys.LootLogLine, entry.RecipientName, item, entry.Count, EntityNames.LootMode(Ctx.Text, entry.Mode))).Append('\n');
            }
            return _sb.ToString();
        }

        private Text Line(string name, float width, ref float y, Color color)
        {
            Text text = UiFactory.Label(name, Content, Ctx.Font, HudLayout.FontNormal, TextAnchor.MiddleLeft, color);
            UiFactory.Place(text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(width, HudLayout.RowHeight));
            y -= HudLayout.RowHeight + HudLayout.Gap;
            return text;
        }

        /// <summary>slot 0 = rightmost.</summary>
        private Button ActionButton(RectTransform row, string key, int slot, UnityEngine.Events.UnityAction onClick)
        {
            Button button = UiFactory.Button(key, row, Ctx.Font, Ctx.Text.Get(key), onClick);
            UiFactory.Place((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-slot * (ActionWidth + HudLayout.Gap), 0f), new Vector2(ActionWidth, HudLayout.MenuButtonHeight));
            return button;
        }
    }
}
