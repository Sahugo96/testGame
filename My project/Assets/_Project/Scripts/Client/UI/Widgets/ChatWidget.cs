using System;
using System.Collections.Generic;
using System.Text;
using Game.Client.Localization;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Widgets
{
    /// <summary>
    /// Chat panel bottom-left (module 14): tabs from the account settings (decision of the developer: "General" and
    /// "Private" by default, own tabs show the channels chosen for them; a tab with new lines is highlighted), the last
    /// lines of the selected tab, a channel button (Say → World → Group → Whisper), a recipient field for whispers and the
    /// text field. Enter (Input Action) focuses the field and sends; the server filters, limits and delivers. Player text
    /// is shown without rich text.
    /// </summary>
    public sealed class ChatWidget
    {
        private static readonly Vector2 Size = new Vector2(500f, 230f);
        private const float OffsetY = 108f;
        private const float ChannelWidth = 96f;
        private const float TargetWidth = 120f;
        private const int VisibleLines = 9;
        private const int TargetNameLimit = 32;
        private const float TabWidth = 92f;
        private const float TabHeight = 26f;
        private const float SettingsWidth = 72f;
        private static readonly ChatChannel[] Channels = { ChatChannel.Say, ChatChannel.World, ChatChannel.Group, ChatChannel.Whisper };

        private readonly ClientContext _ctx;
        private readonly Text _log;
        private readonly Text _channelLabel;
        private readonly InputField _target;
        private readonly InputField _input;
        private readonly RectTransform _inputRect;
        private readonly StringBuilder _sb = new StringBuilder();
        private Func<bool> _submitPressed;
        private int _channel;
        private int _submittedFrame = -1;
        private readonly Button[] _tabButtons;
        private readonly Text[] _tabLabels;
        private readonly bool[] _unread;
        private ChatTabSnapshot[] _tabs = new ChatTabSnapshot[0];
        private int _tab;

        public ChatWidget(RectTransform zone, ClientContext ctx)
        {
            _ctx = ctx;
            Image panel = UiFactory.Panel("Chat", zone, HudPalette.PanelBackground, false);
            UiFactory.Place(panel.rectTransform, Vector2.zero, Vector2.zero, new Vector2(0f, OffsetY), Size);
            float inner = Size.x - HudLayout.Margin * 2f;
            float rowY = HudLayout.Gap;

            _log = UiFactory.Label("Log", panel.transform, ctx.Font, HudLayout.FontSmall, TextAnchor.LowerLeft, HudPalette.Text);
            _log.supportRichText = false;
            UiFactory.Place(_log.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(HudLayout.Margin, -HudLayout.Gap),
                new Vector2(inner, Size.y - HudLayout.MenuButtonHeight - HudLayout.Gap * 3f));
            _log.verticalOverflow = VerticalWrapMode.Truncate;

            Button channel = UiFactory.Button("Channel", panel.transform, ctx.Font, string.Empty, NextChannel);
            UiFactory.Place((RectTransform)channel.transform, Vector2.zero, Vector2.zero, new Vector2(HudLayout.Margin, rowY),
                new Vector2(ChannelWidth, HudLayout.MenuButtonHeight));
            _channelLabel = channel.GetComponentInChildren<Text>();

            _target = UiFactory.Input("Target", panel.transform, ctx.Font, HudLayout.FontSmall, TargetNameLimit, ctx.Text.Get(UiKeys.ChatTargetPlaceholder));
            UiFactory.Place((RectTransform)_target.transform, Vector2.zero, Vector2.zero, new Vector2(HudLayout.Margin + ChannelWidth + HudLayout.Gap, rowY),
                new Vector2(TargetWidth, HudLayout.MenuButtonHeight));

            _input = UiFactory.Input("Input", panel.transform, ctx.Font, HudLayout.FontSmall, ctx.Content.Config.ChatMaxLength, ctx.Text.Get(UiKeys.ChatPlaceholder));
            _inputRect = (RectTransform)_input.transform;
            _input.onEndEdit.AddListener(OnEndEdit);

            int maxTabs = ctx.Content.Config.ChatTabsMax;
            _tabButtons = new Button[maxTabs];
            _tabLabels = new Text[maxTabs];
            _unread = new bool[maxTabs];
            for (int i = 0; i < maxTabs; i++)
            {
                int index = i;
                _tabButtons[i] = UiFactory.Button($"Tab_{i}", zone, ctx.Font, string.Empty, () => SelectTab(index));
                UiFactory.Place((RectTransform)_tabButtons[i].transform, Vector2.zero, Vector2.zero,
                    new Vector2(i * (TabWidth + HudLayout.Gap), OffsetY + Size.y + HudLayout.Gap), new Vector2(TabWidth, TabHeight));
                _tabLabels[i] = _tabButtons[i].GetComponentInChildren<Text>();
                _tabLabels[i].fontSize = HudLayout.FontSmall;
                _tabButtons[i].gameObject.SetActive(false);
            }
            Button settings = UiFactory.Button("TabSettings", zone, ctx.Font, ctx.Text.Get(UiKeys.ChatTabsSettings), () => ctx.RequestWindow(Windows.WindowFactory.ChatTabs));
            UiFactory.Place((RectTransform)settings.transform, Vector2.zero, Vector2.zero,
                new Vector2(Size.x - SettingsWidth, OffsetY + Size.y + HudLayout.Gap), new Vector2(SettingsWidth, TabHeight));
            settings.GetComponentInChildren<Text>().fontSize = HudLayout.FontSmall;

            ctx.State.Chat += OnChat;
            SetChannel(0);
            ApplyTabs(ctx.Content.DefaultChatTabs);
        }

        /// <summary>The account's tabs come with the player state (changed in the chat tabs window).</summary>
        public void Refresh(PlayerStateSnapshot state)
        {
            if (state?.ChatTabs != null && !Same(state.ChatTabs, _tabs))
            {
                ApplyTabs(state.ChatTabs);
            }
        }

        /// <summary>The Enter action (Input Actions only); used to tell "sent with Enter" from "clicked elsewhere".</summary>
        public void BindSubmit(Func<bool> submitPressed) => _submitPressed = submitPressed;

        /// <summary>One of the chat fields has focus.</summary>
        public bool HasFocus => _input.isFocused || _target.isFocused;

        /// <summary>A chat field has focus, or Enter/Esc was just used by the chat this frame (so it is not a gameplay key).</summary>
        public bool IsTyping => HasFocus || _submittedFrame == Time.frameCount;

        /// <summary>Enter while not typing: focus the text field.</summary>
        public void Focus()
        {
            if (_submittedFrame == Time.frameCount)
            {
                return;
            }
            _input.ActivateInputField();
            _input.Select();
        }

        /// <summary>Enter while typing: send the line (if any) and leave the field.</summary>
        public void Submit()
        {
            if (_submittedFrame == Time.frameCount)
            {
                return;
            }
            string text = _input.text;
            _submittedFrame = Time.frameCount;
            if (!string.IsNullOrWhiteSpace(text) && _ctx.Commands != null)
            {
                ChatChannel channel = Channels[_channel];
                _ctx.Commands.SendChat(channel, text, channel == ChatChannel.Whisper ? _target.text : null);
            }
            _input.text = string.Empty;
            _input.DeactivateInputField();
        }

        /// <summary>Esc while typing: leave the field and keep the text.</summary>
        public void Blur()
        {
            _submittedFrame = Time.frameCount;
            _input.DeactivateInputField();
            _target.DeactivateInputField();
        }

        /// <summary>Whisper to a character (friends window, a click on a whisper line later): switch the channel and focus.</summary>
        public void StartWhisper(string name)
        {
            SetChannel(Array.IndexOf(Channels, ChatChannel.Whisper));
            _target.text = name ?? string.Empty;
            _input.ActivateInputField();
            _input.Select();
        }

        public void Destroy() => _ctx.State.Chat -= OnChat;

        private void OnEndEdit(string text)
        {
            // uGUI leaves the field itself on Enter; send only when Enter (not a click outside) ended the edit.
            if (_submitPressed != null && _submitPressed())
            {
                Submit();
            }
        }

        private void NextChannel() => SetChannel((_channel + 1) % Channels.Length);

        private void SetChannel(int index)
        {
            _channel = index < 0 ? 0 : index;
            ChatChannel channel = Channels[_channel];
            _channelLabel.text = ChannelName(channel);
            bool whisper = channel == ChatChannel.Whisper;
            _target.gameObject.SetActive(whisper);
            float left = HudLayout.Margin + ChannelWidth + HudLayout.Gap + (whisper ? TargetWidth + HudLayout.Gap : 0f);
            UiFactory.Place(_inputRect, Vector2.zero, Vector2.zero, new Vector2(left, HudLayout.Gap),
                new Vector2(Size.x - left - HudLayout.Margin, HudLayout.MenuButtonHeight));
        }

        private string ChannelName(ChatChannel channel) => _ctx.Text.Get(UiKeys.ChatChannelPrefix + channel.ToString().ToLowerInvariant());

        private void OnChat(ChatMessage message)
        {
            for (int i = 0; i < _tabs.Length; i++)
            {
                if (i != _tab && Shows(_tabs[i], message.Channel))
                {
                    _unread[i] = true;
                }
            }
            UpdateTabLabels();
            Redraw();
        }

        private void ApplyTabs(ChatTabSnapshot[] tabs)
        {
            _tabs = tabs ?? new ChatTabSnapshot[0];
            _tab = Mathf.Clamp(_tab, 0, Math.Max(0, _tabs.Length - 1));
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                _tabButtons[i].gameObject.SetActive(i < _tabs.Length);
                _unread[i] = false;
            }
            UpdateTabLabels();
            Redraw();
        }

        /// <summary>Shows the tab and, if it cannot show the channel being written to, switches to the first channel it shows.</summary>
        private void SelectTab(int index)
        {
            if (index >= _tabs.Length)
            {
                return;
            }
            _tab = index;
            _unread[index] = false;
            if (!Shows(_tabs[index], Channels[_channel]))
            {
                foreach (ChatChannel channel in _tabs[index].Channels)
                {
                    int sendable = Array.IndexOf(Channels, channel);
                    if (sendable >= 0)
                    {
                        SetChannel(sendable);
                        break;
                    }
                }
            }
            UpdateTabLabels();
            Redraw();
        }

        private void UpdateTabLabels()
        {
            for (int i = 0; i < _tabs.Length && i < _tabLabels.Length; i++)
            {
                _tabLabels[i].text = TabName(_tabs[i]);
                _tabLabels[i].color = i == _tab ? HudPalette.Cast : _unread[i] ? HudPalette.Warning : HudPalette.Text;
            }
        }

        private string TabName(ChatTabSnapshot tab) => string.IsNullOrEmpty(tab.Name) ? _ctx.Text.Get(tab.NameKey) : tab.Name;

        private static bool Shows(ChatTabSnapshot tab, ChatChannel channel) => tab != null && Array.IndexOf(tab.Channels, channel) >= 0;

        private static bool Same(ChatTabSnapshot[] a, ChatTabSnapshot[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].Name != b[i].Name || a[i].NameKey != b[i].NameKey || a[i].Channels.Length != b[i].Channels.Length)
                {
                    return false;
                }
                for (int k = 0; k < a[i].Channels.Length; k++)
                {
                    if (a[i].Channels[k] != b[i].Channels[k])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>The last VisibleLines lines of the selected tab's channels, oldest first.</summary>
        private void Redraw()
        {
            List<ChatMessage> log = _ctx.State.ChatLog;
            ChatTabSnapshot tab = _tab < _tabs.Length ? _tabs[_tab] : null;
            int first = log.Count;
            for (int shown = 0; first > 0 && shown < VisibleLines;)
            {
                first--;
                if (tab == null || Shows(tab, log[first].Channel))
                {
                    shown++;
                }
            }
            _sb.Clear();
            for (int i = first; i < log.Count; i++)
            {
                if (tab != null && !Shows(tab, log[i].Channel))
                {
                    continue;
                }
                if (_sb.Length > 0)
                {
                    _sb.Append('\n');
                }
                _sb.Append(Format(log[i]));
            }
            _log.text = _sb.ToString();
        }

        private string Format(ChatMessage message)
        {
            ILocalizationService text = _ctx.Text;
            switch (message.Channel)
            {
                case ChatChannel.System:
                    // Same arguments as HUD notifications: {0} = number (none here), {1} = name.
                    return text.Format(UiKeys.ChatSystemLine, text.Format(message.TextKey, 0, message.SenderName ?? string.Empty));
                case ChatChannel.Whisper:
                    PlayerStateSnapshot self = _ctx.State.Player;
                    bool outgoing = self != null && message.SenderId == self.EntityId;
                    return outgoing
                        ? text.Format(UiKeys.ChatWhisperOut, message.TargetName, message.Text)
                        : text.Format(UiKeys.ChatWhisperIn, message.SenderName, message.Text);
                default:
                    return text.Format(UiKeys.ChatLine, ChannelName(message.Channel), message.SenderName, message.Text);
            }
        }
    }
}
