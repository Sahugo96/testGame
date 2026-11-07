using Game.Client.Localization;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Widgets
{
    /// <summary>
    /// Duel panel under the target frame (question 70): incoming request (accept / decline), own request (cancel),
    /// countdown and active duel (surrender). Shown only while the player has a duel; the server decides everything.
    /// </summary>
    public sealed class DuelWidget
    {
        private const float OffsetFromTop = -190f;
        private static readonly Vector2 Size = new Vector2(440f, 86f);
        private const float ButtonWidth = 130f;

        private readonly ClientContext _ctx;
        private readonly Image _panel;
        private readonly Text _text;
        private readonly Button _accept;
        private readonly Button _decline;
        private readonly Button _surrender;
        private readonly Text _surrenderLabel;

        public DuelWidget(RectTransform root, ClientContext ctx)
        {
            _ctx = ctx;
            _panel = UiFactory.Panel("Duel", root, HudPalette.PanelBackground, false);
            UiFactory.Place(_panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, OffsetFromTop), Size);
            _text = UiFactory.Label("Text", _panel.transform, ctx.Font, HudLayout.FontNormal, TextAnchor.MiddleCenter, HudPalette.Text);
            UiFactory.Place(_text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -HudLayout.Gap),
                new Vector2(Size.x - HudLayout.Margin * 2f, Size.y - HudLayout.MenuButtonHeight - HudLayout.Gap * 3f));

            _accept = AddButton("Accept", UiKeys.DuelAccept, -1f, () => _ctx.Commands?.RespondDuel(true));
            _decline = AddButton("Decline", UiKeys.DuelDecline, 1f, () => _ctx.Commands?.RespondDuel(false));
            _surrender = AddButton("Surrender", UiKeys.DuelSurrender, 0f, () => _ctx.Commands?.SurrenderDuel());
            _surrenderLabel = _surrender.GetComponentInChildren<Text>();
            _panel.gameObject.SetActive(false);
        }

        public void Refresh(PlayerStateSnapshot state)
        {
            DuelSnapshot duel = state != null ? state.Duel : default;
            bool visible = duel.Phase != DuelPhase.None;
            _panel.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }
            string opponent = OpponentName(duel);
            int seconds = Mathf.CeilToInt(duel.Remaining);
            bool incoming = duel.Phase == DuelPhase.IncomingRequest;
            _accept.gameObject.SetActive(incoming);
            _decline.gameObject.SetActive(incoming);
            _surrender.gameObject.SetActive(!incoming);
            switch (duel.Phase)
            {
                case DuelPhase.IncomingRequest:
                    _text.text = _ctx.Text.Format(UiKeys.DuelIncoming, opponent, seconds);
                    break;
                case DuelPhase.OutgoingRequest:
                    _text.text = _ctx.Text.Format(UiKeys.DuelOutgoing, opponent, seconds);
                    _surrenderLabel.text = _ctx.Text.Get(UiKeys.DuelCancel);
                    break;
                case DuelPhase.Countdown:
                    _text.text = _ctx.Text.Format(UiKeys.DuelCountdown, opponent, seconds);
                    _surrenderLabel.text = _ctx.Text.Get(UiKeys.DuelCancel);
                    break;
                default:
                    _text.text = _ctx.Text.Format(UiKeys.DuelActive, opponent, seconds);
                    _surrenderLabel.text = _ctx.Text.Get(UiKeys.DuelSurrender);
                    break;
            }
        }

        private string OpponentName(DuelSnapshot duel)
        {
            if (!_ctx.State.TryGetEntity(duel.OpponentId, out EntitySnapshot opponent))
            {
                return string.Empty;
            }
            return string.IsNullOrEmpty(opponent.DisplayName) ? _ctx.Text.Get(opponent.NameKey) : opponent.DisplayName;
        }

        /// <summary>side: -1 left, 0 center, 1 right.</summary>
        private Button AddButton(string name, string key, float side, UnityEngine.Events.UnityAction onClick)
        {
            Button button = UiFactory.Button(name, _panel.transform, _ctx.Font, _ctx.Text.Get(key), onClick);
            UiFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(side * (ButtonWidth * 0.5f + HudLayout.Gap), HudLayout.Gap * 2f), new Vector2(ButtonWidth, HudLayout.MenuButtonHeight));
            return button;
        }
    }
}
