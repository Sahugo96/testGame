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
    /// leave, the loot mode with proposals for the other modes, the vote state and the loot log. Raid: the leader turns
    /// the group into a raid; members are listed by pages with their subgroup, the leader and assistants move members
    /// between subgroups, the leader appoints assistants. Only voters (leader + first of every subgroup) vote.
    /// </summary>
    public sealed class GroupWindow : HudWindow
    {
        private static readonly Vector2 Size = new Vector2(640f, 820f);
        private const float ActionWidth = 92f;
        private const int RowsPerPage = 10;
        private const int LogLines = 5;
        private static readonly LootMode[] Modes = { LootMode.PickedUp, LootMode.Leader, LootMode.RoundRobin };

        private readonly Text _hint;
        private readonly Text _mode;
        private readonly RectTransform[] _rows;
        private readonly Text[] _names;
        private readonly Button[] _promote;
        private readonly Button[] _kick;
        private readonly Button[] _assistant;
        private readonly Text[] _assistantLabels;
        private readonly Button[] _moveUp;
        private readonly Button[] _moveDown;
        private readonly EntityId[] _ids;
        private readonly int[] _subgroups;
        private readonly bool[] _isAssistant;
        private readonly PagerWidget _pager;
        private readonly Button _leave;
        private readonly Button _convert;
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

            int max = RowsPerPage;
            _rows = new RectTransform[max];
            _names = new Text[max];
            _promote = new Button[max];
            _kick = new Button[max];
            _assistant = new Button[max];
            _assistantLabels = new Text[max];
            _moveUp = new Button[max];
            _moveDown = new Button[max];
            _ids = new EntityId[max];
            _subgroups = new int[max];
            _isAssistant = new bool[max];
            for (int i = 0; i < max; i++)
            {
                int index = i;
                RectTransform row = UiFactory.Place(UiFactory.Rect($"Member_{i}", Content), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, y), new Vector2(width, HudLayout.MenuButtonHeight));
                _names[i] = UiFactory.Label("Name", row, ctx.Font, HudLayout.FontNormal, TextAnchor.MiddleLeft, HudPalette.Text);
                UiFactory.Place(_names[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                    new Vector2(width - (ActionWidth + HudLayout.Gap) * 5f, HudLayout.MenuButtonHeight));
                _promote[i] = ActionButton(row, UiKeys.GroupPromote, 1, () => Ctx.Commands?.PromoteToLeader(_ids[index]));
                _kick[i] = ActionButton(row, UiKeys.GroupKick, 0, () => Ctx.Commands?.KickFromGroup(_ids[index]));
                _assistant[i] = ActionButton(row, UiKeys.RaidAssistantOn, 2, () => Ctx.Commands?.SetAssistant(_ids[index], !_isAssistant[index]));
                _assistantLabels[i] = _assistant[i].GetComponentInChildren<Text>();
                _moveDown[i] = ActionButton(row, UiKeys.RaidMoveDown, 3, () => Ctx.Commands?.MoveToSubgroup(_ids[index], _subgroups[index] + 1));
                _moveUp[i] = ActionButton(row, UiKeys.RaidMoveUp, 4, () => Ctx.Commands?.MoveToSubgroup(_ids[index], _subgroups[index] - 1));
                _rows[i] = row;
                y -= HudLayout.MenuButtonHeight + HudLayout.Gap;
            }

            _leave = UiFactory.Button("Leave", Content, ctx.Font, ctx.Text.Get(UiKeys.GroupLeave), () => Ctx.Commands?.LeaveGroup());
            UiFactory.Place((RectTransform)_leave.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y),
                new Vector2(ActionWidth * 2f, HudLayout.MenuButtonHeight));
            _convert = UiFactory.Button("ConvertToRaid", Content, ctx.Font, ctx.Text.Get(UiKeys.GroupConvertRaid), () => Ctx.Commands?.ConvertToRaid());
            UiFactory.Place((RectTransform)_convert.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(ActionWidth * 2f + HudLayout.Gap, y),
                new Vector2(ActionWidth * 2f, HudLayout.MenuButtonHeight));
            _pager = new PagerWidget(Content, ctx, new Vector2(1f, 1f), new Vector2(0f, y), () =>
            {
                if (Ctx.State.Player != null)
                {
                    Refresh(Ctx.State.Player);
                }
            });
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
            string mode = inGroup ? Ctx.Text.Format(UiKeys.LootModeLine, EntityNames.LootMode(Ctx.Text, group.LootMode)) : string.Empty;
            _mode.text = inGroup && group.IsRaid
                ? mode + "  " + Ctx.Text.Format(UiKeys.RaidLine, group.Members.Length, Ctx.Content.Config.RaidMaxSize)
                : mode;
            bool isLeader = inGroup && group.LeaderId == state.EntityId;
            bool raid = inGroup && group.IsRaid;
            bool canMove = raid && (isLeader || Contains(group.Assistants, state.EntityId));
            int subgroupCount = Ctx.Content.Config.RaidMaxSize / Ctx.Content.Config.RaidSubgroupSize;
            int count = inGroup ? group.Members.Length : 0;
            int first = _pager.Apply(count, RowsPerPage);
            for (int i = 0; i < _rows.Length; i++)
            {
                int index = first + i;
                bool shown = index < count;
                _rows[i].gameObject.SetActive(shown);
                if (!shown)
                {
                    continue;
                }
                EntityId member = group.Members[index];
                _ids[i] = member;
                _subgroups[i] = index < group.Subgroups.Length ? group.Subgroups[index] : 0;
                _isAssistant[i] = Contains(group.Assistants, member);
                _names[i].text = MemberName(group, member, _subgroups[i], _isAssistant[i]);
                bool other = member != state.EntityId;
                _promote[i].gameObject.SetActive(isLeader && other);
                _kick[i].gameObject.SetActive(isLeader && other);
                _assistant[i].gameObject.SetActive(raid && isLeader && other);
                _assistantLabels[i].text = Ctx.Text.Get(_isAssistant[i] ? UiKeys.RaidAssistantOff : UiKeys.RaidAssistantOn);
                _moveUp[i].gameObject.SetActive(canMove);
                _moveDown[i].gameObject.SetActive(canMove);
                _moveUp[i].interactable = _subgroups[i] > 0;
                _moveDown[i].interactable = _subgroups[i] < subgroupCount - 1;
            }
            _leave.gameObject.SetActive(inGroup);
            _convert.gameObject.SetActive(isLeader && !group.IsRaid);
            _proposeTitle.gameObject.SetActive(inGroup);
            bool voter = inGroup && Contains(group.Voters, state.EntityId);
            for (int i = 0; i < _propose.Length; i++)
            {
                _propose[i].gameObject.SetActive(inGroup);
                _propose[i].interactable = voter && !group.VoteActive && group.ProposeCooldownRemaining <= 0f && Modes[i] != group.LootMode;
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
                    group.Voters.Length, Mathf.CeilToInt(group.VoteRemaining));
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

        /// <summary>Name with the leader / assistant mark; in a raid prefixed with the subgroup number (1-based for people).</summary>
        private string MemberName(GroupSnapshot group, EntityId member, int subgroup, bool assistant)
        {
            string name = EntityNames.Of(Ctx, member);
            if (member == group.LeaderId)
            {
                name = Ctx.Text.Format(UiKeys.GroupLeaderName, name);
            }
            else if (assistant)
            {
                name = Ctx.Text.Format(UiKeys.RaidAssistantName, name);
            }
            return group.IsRaid ? Ctx.Text.Format(UiKeys.RaidMemberName, subgroup + 1, name) : name;
        }

        private static bool Contains(EntityId[] ids, EntityId id) => ids != null && Array.IndexOf(ids, id) >= 0;

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
