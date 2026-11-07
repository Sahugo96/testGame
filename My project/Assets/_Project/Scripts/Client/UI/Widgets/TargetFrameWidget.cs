using Game.Client.Localization;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Widgets
{
    /// <summary>Top-center target frame (02, 7.13): name and health of the selected target.</summary>
    public sealed class TargetFrameWidget
    {
        private readonly ClientContext _ctx;
        private readonly Image _panel;
        private readonly Text _name;
        private readonly UiBar _health;
        private readonly Button _duel;
        private readonly Button _invite;
        private readonly Button _trade;
        private readonly Text _targetOf;
        private readonly StatusRowWidget _statuses;

        public TargetFrameWidget(RectTransform zone, ClientContext ctx, Tooltip tooltip)
        {
            _ctx = ctx;
            _panel = UiFactory.Panel("TargetFrame", zone, HudPalette.PanelBackground, false);
            UiFactory.Place(_panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, HudLayout.TargetFrameSize);
            float width = HudLayout.TargetFrameSize.x - HudLayout.Margin * 2f;
            _name = UiFactory.Label("Name", _panel.transform, ctx.Font, HudLayout.FontNormal, TextAnchor.MiddleCenter, HudPalette.Text);
            UiFactory.Place(_name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -HudLayout.Gap), new Vector2(width, HudLayout.BarHeight));
            _health = UiFactory.Bar("Health", _panel.transform, ctx.Font, HudPalette.Health);
            UiFactory.Place(_health.Rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -HudLayout.BarHeight - HudLayout.Gap * 2f), new Vector2(width, HudLayout.BarHeight));
            _duel = UiFactory.Button("Duel", _panel.transform, ctx.Font, ctx.Text.Get(UiKeys.DuelChallenge), RequestDuel);
            UiFactory.Place((RectTransform)_duel.transform, new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(HudLayout.Gap, 0f),
                new Vector2(DuelButtonWidth, HudLayout.TargetFrameSize.y));
            _targetOf = UiFactory.Label("TargetOf", zone, ctx.Font, HudLayout.FontSmall, TextAnchor.MiddleCenter, HudPalette.TextDim);
            UiFactory.Place(_targetOf.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -HudLayout.TargetFrameSize.y - HudLayout.Gap),
                new Vector2(HudLayout.TargetFrameSize.x, HudLayout.FontSmall + HudLayout.Gap));
            _statuses = new StatusRowWidget(zone, ctx, tooltip, new Vector2(0.5f, 1f),
                new Vector2(-HudLayout.TargetFrameSize.x * 0.5f, -HudLayout.TargetFrameSize.y - HudLayout.FontSmall - HudLayout.Gap * 3f), TargetStatusColumns, TargetStatusMax);
            _invite = UiFactory.Button("Invite", _panel.transform, ctx.Font, ctx.Text.Get(UiKeys.GroupInviteButton), InviteToGroup);
            UiFactory.Place((RectTransform)_invite.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-HudLayout.Gap, 0f),
                new Vector2(DuelButtonWidth, HudLayout.TargetFrameSize.y));
            _trade = UiFactory.Button("Trade", _panel.transform, ctx.Font, ctx.Text.Get(UiKeys.TradeButton), RequestTrade);
            UiFactory.Place((RectTransform)_trade.transform, new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(DuelButtonWidth + HudLayout.Gap * 2f, 0f),
                new Vector2(DuelButtonWidth, HudLayout.TargetFrameSize.y));
            _panel.gameObject.SetActive(false);
        }

        private const float DuelButtonWidth = 90f;

        /// <summary>Offer a trade to the selected player (the server checks distance, combat and busy states).</summary>
        private void RequestTrade()
        {
            PlayerStateSnapshot player = _ctx.State.Player;
            if (player != null && _ctx.Commands != null)
            {
                _ctx.Commands.RequestTrade(player.TargetId);
            }
        }
        private const int TargetStatusColumns = 3;
        private const int TargetStatusMax = 6;

        /// <summary>Invite the selected player (the server checks leader rights, size and busy states).</summary>
        private void InviteToGroup()
        {
            PlayerStateSnapshot player = _ctx.State.Player;
            if (player != null && _ctx.Commands != null)
            {
                _ctx.Commands.InviteToGroup(player.TargetId);
            }
        }

        /// <summary>The invite button: another player outside my group, and I can invite (no group yet, I lead it, or I assist the raid).</summary>
        private static bool CanInvite(PlayerStateSnapshot player, EntitySnapshot target)
        {
            if (target.Kind != EntityKind.Player || target.Id == player.EntityId)
            {
                return false;
            }
            GroupSnapshot group = player.Group;
            if (group == null || !group.InGroup)
            {
                return true;
            }
            bool canManage = group.LeaderId == player.EntityId || (group.IsRaid && System.Array.IndexOf(group.Assistants, player.EntityId) >= 0);
            return canManage && System.Array.IndexOf(group.Members, target.Id) < 0;
        }

        /// <summary>Challenge the selected player (the server checks distance, combat and busy states).</summary>
        private void RequestDuel()
        {
            PlayerStateSnapshot player = _ctx.State.Player;
            if (player != null && _ctx.Commands != null)
            {
                _ctx.Commands.RequestDuel(player.TargetId);
            }
        }

        public void Refresh(PlayerStateSnapshot player)
        {
            bool has = player != null && player.TargetId.IsValid && _ctx.State.TryGetEntity(player.TargetId, out _);
            _panel.gameObject.SetActive(has);
            _targetOf.gameObject.SetActive(has);
            if (!has)
            {
                _statuses.Refresh(null);
                return;
            }
            _ctx.State.TryGetEntity(player.TargetId, out EntitySnapshot target);
            _duel.gameObject.SetActive(target.Kind == EntityKind.Player && target.Id != player.EntityId && player.Duel.Phase == DuelPhase.None);
            _invite.gameObject.SetActive(CanInvite(player, target));
            _trade.gameObject.SetActive(target.Kind == EntityKind.Player && target.Id != player.EntityId
                && (player.Trade == null || player.Trade.Phase == TradePhase.None));
            _statuses.Refresh(target.Statuses);
            _targetOf.text = target.TargetOfId.IsValid ? _ctx.Text.Format(UiKeys.TargetOfTarget, EntityNames.Of(_ctx, target.TargetOfId)) : string.Empty;
            _name.text = string.IsNullOrEmpty(target.DisplayName) ? _ctx.Text.Get(target.NameKey) : target.DisplayName;
            _health.Set(target.MaxHealth > 0f ? target.Health / target.MaxHealth : 0f,
                _ctx.Text.Format(UiKeys.ValueOfMax, Mathf.CeilToInt(target.Health), Mathf.RoundToInt(target.MaxHealth)));
        }
    }
}
