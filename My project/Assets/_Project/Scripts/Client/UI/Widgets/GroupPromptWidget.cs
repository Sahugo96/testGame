using Game.Client.Localization;
using Game.Shared.Core;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.UI;
using EntityId = Game.Shared.Core.EntityId;

namespace Game.Client.UI.Widgets
{
    /// <summary>
    /// Questions to the player: an incoming group invite (accept / decline), a trade request (accept / decline; my own
    /// request can be withdrawn) and a loot-mode vote for voters (for / against, 06 7.8). Shown under the duel panel;
    /// the server decides everything.
    /// </summary>
    public sealed class GroupPromptWidget
    {
        private const float OffsetFromTop = -290f;
        private static readonly Vector2 Size = new Vector2(440f, 86f);
        private const float ButtonWidth = 130f;

        private readonly ClientContext _ctx;
        private readonly Image _panel;
        private readonly Text _text;
        private readonly Text _yesLabel;
        private readonly Text _noLabel;
        private enum Question
        {
            Invite,
            TradeIncoming,
            TradeOutgoing,
            Vote,
        }

        private Question _question;
        private int _proposalId;
        private readonly GameObject _yesButton;

        public GroupPromptWidget(RectTransform root, ClientContext ctx)
        {
            _ctx = ctx;
            _panel = UiFactory.Panel("GroupPrompt", root, HudPalette.PanelBackground, false);
            UiFactory.Place(_panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, OffsetFromTop), Size);
            _text = UiFactory.Label("Text", _panel.transform, ctx.Font, HudLayout.FontNormal, TextAnchor.MiddleCenter, HudPalette.Text);
            UiFactory.Place(_text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -HudLayout.Gap),
                new Vector2(Size.x - HudLayout.Margin * 2f, Size.y - HudLayout.MenuButtonHeight - HudLayout.Gap * 3f));
            _yesLabel = AddButton("Yes", -1f, () => Answer(true));
            _yesButton = _yesLabel.transform.parent.gameObject;
            _noLabel = AddButton("No", 1f, () => Answer(false));
            _panel.gameObject.SetActive(false);
        }

        public void Refresh(PlayerStateSnapshot state)
        {
            GroupSnapshot group = state?.Group;
            if (group == null)
            {
                _panel.gameObject.SetActive(false);
                return;
            }
            _yesButton.SetActive(true);
            if (group.InviteFromId.IsValid)
            {
                _question = Question.Invite;
                _text.text = _ctx.Text.Format(UiKeys.GroupInvite, EntityNames.Of(_ctx, group.InviteFromId), Mathf.CeilToInt(group.InviteRemaining));
                _yesLabel.text = _ctx.Text.Get(UiKeys.DuelAccept);
                _noLabel.text = _ctx.Text.Get(UiKeys.DuelDecline);
                _panel.gameObject.SetActive(true);
                return;
            }
            TradeSnapshot trade = state.Trade;
            if (trade != null && (trade.Phase == TradePhase.IncomingRequest || trade.Phase == TradePhase.OutgoingRequest))
            {
                bool incoming = trade.Phase == TradePhase.IncomingRequest;
                _question = incoming ? Question.TradeIncoming : Question.TradeOutgoing;
                _text.text = _ctx.Text.Format(incoming ? UiKeys.TradeRequestIncoming : UiKeys.TradeRequestOutgoing,
                    EntityNames.Of(_ctx, trade.PartnerId), Mathf.CeilToInt(trade.Remaining));
                _yesLabel.text = _ctx.Text.Get(UiKeys.DuelAccept);
                _noLabel.text = _ctx.Text.Get(incoming ? UiKeys.DuelDecline : UiKeys.TradeCancel);
                _yesButton.SetActive(incoming);
                _panel.gameObject.SetActive(true);
                return;
            }
            if (group.VoteActive && System.Array.IndexOf(group.Voters, state.EntityId) >= 0 && !Voted(group, state.EntityId))
            {
                _question = Question.Vote;
                _proposalId = group.VoteProposalId;
                _text.text = _ctx.Text.Format(UiKeys.LootVoteQuestion, EntityNames.Of(_ctx, group.VoteProposerId),
                    EntityNames.LootMode(_ctx.Text, group.VoteMode), Mathf.CeilToInt(group.VoteRemaining));
                _yesLabel.text = _ctx.Text.Get(UiKeys.VoteFor);
                _noLabel.text = _ctx.Text.Get(UiKeys.VoteAgainst);
                _panel.gameObject.SetActive(true);
                return;
            }
            _panel.gameObject.SetActive(false);
        }

        private static bool Voted(GroupSnapshot group, EntityId self)
        {
            foreach (EntityId id in group.VotesFor)
            {
                if (id == self)
                {
                    return true;
                }
            }
            return false;
        }

        private void Answer(bool yes)
        {
            if (_ctx.Commands == null)
            {
                return;
            }
            switch (_question)
            {
                case Question.Invite:
                    _ctx.Commands.RespondGroupInvite(yes);
                    break;
                case Question.TradeIncoming:
                    _ctx.Commands.RespondTrade(yes);
                    break;
                case Question.TradeOutgoing:
                    _ctx.Commands.CancelTrade();
                    break;
                default:
                    _ctx.Commands.VoteLootMode(_proposalId, yes);
                    break;
            }
        }

        /// <summary>side: -1 left, 1 right. Returns the caption label.</summary>
        private Text AddButton(string name, float side, UnityEngine.Events.UnityAction onClick)
        {
            Button button = UiFactory.Button(name, _panel.transform, _ctx.Font, string.Empty, onClick);
            UiFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(side * (ButtonWidth * 0.5f + HudLayout.Gap), HudLayout.Gap * 2f), new Vector2(ButtonWidth, HudLayout.MenuButtonHeight));
            return button.GetComponentInChildren<Text>();
        }
    }
}
