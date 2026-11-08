using System;
using Game.Client.Localization;
using Game.Client.UI.Widgets;
using Game.Shared.Content.Dto;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Windows
{
    /// <summary>
    /// Friends (module 14; mutual by decision of the developer): send a request by character name; incoming requests on top
    /// with accept / decline, then friends with online state, whisper and remove, then my requests with "withdraw". The
    /// lists live on the server in the account settings; the server checks names, limits and both sides.
    /// </summary>
    public sealed class FriendsWindow : HudWindow
    {
        private static readonly Vector2 Size = new Vector2(520f, 600f);
        private const int RowsPerPage = 10;
        private const float AddWidth = 170f;
        private const float StateWidth = 100f;
        private const float ActionWidth = 100f;
        private const int NameLimit = 32;

        private readonly InputField _name;
        private readonly Text _count;
        private readonly RectTransform[] _rows = new RectTransform[RowsPerPage];
        private readonly Text[] _names = new Text[RowsPerPage];
        private readonly Text[] _states = new Text[RowsPerPage];
        private readonly Button[] _primary = new Button[RowsPerPage];
        private readonly Text[] _primaryLabels = new Text[RowsPerPage];
        private readonly Text[] _secondaryLabels = new Text[RowsPerPage];
        private readonly string[] _rowNames = new string[RowsPerPage];
        private readonly FriendSnapshot[] _rowEntries = new FriendSnapshot[RowsPerPage];
        private readonly PagerWidget _pager;

        public FriendsWindow(HudWindowDto definition, RectTransform parent, Canvas canvas, ClientContext ctx, Tooltip tooltip,
            Action<HudWindow> focus, Action<HudWindow> close) : base(definition, parent, canvas, ctx, tooltip, focus, close, Size)
        {
            float width = Size.x - HudLayout.Margin * 2f;
            float y = 0f;
            _name = UiFactory.Input("Name", Content, ctx.Font, HudLayout.FontNormal, NameLimit, ctx.Text.Get(UiKeys.FriendsNamePlaceholder));
            UiFactory.Place((RectTransform)_name.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y),
                new Vector2(width - AddWidth - HudLayout.Gap, HudLayout.MenuButtonHeight));
            Button add = UiFactory.Button("Add", Content, ctx.Font, ctx.Text.Get(UiKeys.FriendsAdd), Add);
            UiFactory.Place((RectTransform)add.transform, Vector2.one, Vector2.one, new Vector2(0f, y), new Vector2(AddWidth, HudLayout.MenuButtonHeight));
            y -= HudLayout.MenuButtonHeight + HudLayout.Gap;

            _count = UiFactory.Label("Count", Content, ctx.Font, HudLayout.FontNormal, TextAnchor.MiddleLeft, HudPalette.TextDim);
            UiFactory.Place(_count.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(width, HudLayout.RowHeight));
            y -= HudLayout.RowHeight + HudLayout.Gap;

            for (int i = 0; i < RowsPerPage; i++)
            {
                int index = i;
                RectTransform row = UiFactory.Place(UiFactory.Rect($"Friend_{i}", Content), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, y), new Vector2(width, HudLayout.MenuButtonHeight));
                _names[i] = UiFactory.Label("Name", row, ctx.Font, HudLayout.FontNormal, TextAnchor.MiddleLeft, HudPalette.Text);
                UiFactory.Place(_names[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                    new Vector2(width - StateWidth - ActionWidth * 2f - HudLayout.Gap * 3f, HudLayout.MenuButtonHeight));
                _states[i] = UiFactory.Label("State", row, ctx.Font, HudLayout.FontSmall, TextAnchor.MiddleLeft, HudPalette.TextDim);
                UiFactory.Place(_states[i].rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(-(ActionWidth + HudLayout.Gap) * 2f, 0f), new Vector2(StateWidth, HudLayout.MenuButtonHeight));
                _primary[i] = RowButton(row, UiKeys.FriendsWhisper, 1, () => Primary(index));
                _primaryLabels[i] = _primary[i].GetComponentInChildren<Text>();
                _secondaryLabels[i] = RowButton(row, UiKeys.FriendsRemove, 0, () => Secondary(index)).GetComponentInChildren<Text>();
                _rows[i] = row;
                y -= HudLayout.MenuButtonHeight + HudLayout.Gap;
            }
            _pager = new PagerWidget(Content, ctx, new Vector2(0.5f, 0f), new Vector2(0f, HudLayout.MenuButtonHeight * 0.5f), () =>
            {
                if (Ctx.State.Player != null)
                {
                    Refresh(Ctx.State.Player);
                }
            });
        }

        public override void Refresh(PlayerStateSnapshot state)
        {
            FriendSnapshot[] friends = state.Friends ?? new FriendSnapshot[0];
            int accepted = 0;
            foreach (FriendSnapshot entry in friends)
            {
                accepted += entry.State == FriendState.Friend ? 1 : 0;
            }
            _count.text = Ctx.Text.Format(UiKeys.FriendsCount, accepted, Ctx.Content.Config.FriendsMax);
            int first = _pager.Apply(friends.Length, RowsPerPage);
            for (int i = 0; i < RowsPerPage; i++)
            {
                int index = first + i;
                bool shown = index < friends.Length;
                _rows[i].gameObject.SetActive(shown);
                if (!shown)
                {
                    continue;
                }
                FriendSnapshot friend = friends[index];
                _rowNames[i] = friend.Name;
                _rowEntries[i] = friend;
                _names[i].text = friend.Name;
                switch (friend.State)
                {
                    case FriendState.Incoming:
                        _states[i].text = Ctx.Text.Get(UiKeys.FriendsIncoming);
                        _states[i].color = HudPalette.Warning;
                        _primaryLabels[i].text = Ctx.Text.Get(UiKeys.FriendsAccept);
                        _secondaryLabels[i].text = Ctx.Text.Get(UiKeys.FriendsDecline);
                        break;
                    case FriendState.Outgoing:
                        _states[i].text = Ctx.Text.Get(UiKeys.FriendsOutgoing);
                        _states[i].color = HudPalette.TextDim;
                        _secondaryLabels[i].text = Ctx.Text.Get(UiKeys.FriendsWithdraw);
                        break;
                    default:
                        _states[i].text = Ctx.Text.Get(friend.Online ? UiKeys.FriendsOnline : UiKeys.FriendsOffline);
                        _states[i].color = friend.Online ? HudPalette.Positive : HudPalette.TextDim;
                        _primaryLabels[i].text = Ctx.Text.Get(UiKeys.FriendsWhisper);
                        _secondaryLabels[i].text = Ctx.Text.Get(UiKeys.FriendsRemove);
                        break;
                }
                _primary[i].gameObject.SetActive(friend.State != FriendState.Outgoing);
                _primary[i].interactable = friend.State == FriendState.Incoming || friend.Online;
            }
        }

        /// <summary>Right-hand pair of buttons: whisper / accept (primary) and remove / decline / withdraw (secondary).</summary>
        private void Primary(int row)
        {
            if (_rowEntries[row].State == FriendState.Incoming)
            {
                Ctx.Commands?.RespondFriend(_rowNames[row], true);
            }
            else
            {
                Ctx.RequestWhisper(_rowNames[row]);
            }
        }

        private void Secondary(int row)
        {
            if (_rowEntries[row].State == FriendState.Incoming)
            {
                Ctx.Commands?.RespondFriend(_rowNames[row], false);
            }
            else
            {
                Ctx.Commands?.RemoveFriend(_rowNames[row]);
            }
        }

        private void Add()
        {
            string name = _name.text;
            if (string.IsNullOrWhiteSpace(name) || Ctx.Commands == null)
            {
                return;
            }
            Ctx.Commands.AddFriend(name.Trim());
            _name.text = string.Empty;
        }

        /// <summary>slot 0 = rightmost.</summary>
        private Button RowButton(RectTransform row, string key, int slot, UnityEngine.Events.UnityAction onClick)
        {
            Button button = UiFactory.Button(key, row, Ctx.Font, Ctx.Text.Get(key), onClick);
            UiFactory.Place((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-slot * (ActionWidth + HudLayout.Gap), 0f), new Vector2(ActionWidth, HudLayout.MenuButtonHeight));
            return button;
        }
    }
}
