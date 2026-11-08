using System;
using System.Collections.Generic;
using Game.Client.Localization;
using Game.Shared.Content.Dto;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Windows
{
    /// <summary>
    /// Chat tabs of the account (decision of the developer): pick a tab, rename it, choose the channels it shows, add or
    /// delete tabs, return to the defaults ("General", "Private"). Edits are a local draft until "Save"; the server checks
    /// and stores them in the account settings. Opened from the chat panel and from the settings window.
    /// </summary>
    public sealed class ChatTabsWindow : HudWindow
    {
        private static readonly Vector2 Size = new Vector2(520f, 470f);
        private const float ListWidth = 150f;
        private const float ButtonWidth = 150f;
        private static readonly ChatChannel[] Channels = { ChatChannel.Say, ChatChannel.World, ChatChannel.Group, ChatChannel.Whisper, ChatChannel.System };

        private readonly Button[] _tabButtons;
        private readonly Text[] _tabLabels;
        private readonly InputField _name;
        private readonly Text _namePlaceholder;
        private readonly Text[] _channelLabels = new Text[Channels.Length];
        private readonly Button _add;
        private readonly Button _delete;
        private readonly List<ChatTabSnapshot> _draft = new List<ChatTabSnapshot>();
        private int _selected;

        public ChatTabsWindow(HudWindowDto definition, RectTransform parent, Canvas canvas, ClientContext ctx, Tooltip tooltip,
            Action<HudWindow> focus, Action<HudWindow> close) : base(definition, parent, canvas, ctx, tooltip, focus, close, Size)
        {
            int max = ctx.Content.Config.ChatTabsMax;
            _tabButtons = new Button[max];
            _tabLabels = new Text[max];
            for (int i = 0; i < max; i++)
            {
                int index = i;
                _tabButtons[i] = UiFactory.Button($"Tab_{i}", Content, ctx.Font, string.Empty, () => Select(index));
                UiFactory.Place((RectTransform)_tabButtons[i].transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, -i * (HudLayout.MenuButtonHeight + HudLayout.Gap)), new Vector2(ListWidth, HudLayout.MenuButtonHeight));
                _tabLabels[i] = _tabButtons[i].GetComponentInChildren<Text>();
            }

            float x = ListWidth + HudLayout.Margin;
            float width = Size.x - HudLayout.Margin * 3f - ListWidth;
            float y = 0f;
            Text nameTitle = Label("NameTitle", x, ref y, width);
            nameTitle.text = ctx.Text.Get(UiKeys.ChatTabName);
            _name = UiFactory.Input("Name", Content, ctx.Font, HudLayout.FontNormal, ctx.Content.Config.ChatTabNameMax, string.Empty);
            UiFactory.Place((RectTransform)_name.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, y), new Vector2(width, HudLayout.MenuButtonHeight));
            _namePlaceholder = (Text)_name.placeholder;
            _name.onEndEdit.AddListener(text => StoreName());
            y -= HudLayout.MenuButtonHeight + HudLayout.Gap * 3f;

            Text channelsTitle = Label("ChannelsTitle", x, ref y, width);
            channelsTitle.text = ctx.Text.Get(UiKeys.ChatTabChannels);
            for (int i = 0; i < Channels.Length; i++)
            {
                ChatChannel channel = Channels[i];
                Button toggle = UiFactory.Button($"Channel_{channel}", Content, ctx.Font, string.Empty, () => Toggle(channel));
                UiFactory.Place((RectTransform)toggle.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, y), new Vector2(width, HudLayout.MenuButtonHeight));
                _channelLabels[i] = toggle.GetComponentInChildren<Text>();
                _channelLabels[i].alignment = TextAnchor.MiddleLeft;
                y -= HudLayout.MenuButtonHeight + HudLayout.Gap;
            }

            float bottom = HudLayout.MenuButtonHeight + HudLayout.Gap;
            _add = AddButton("Add", UiKeys.ChatTabAdd, 0, bottom, Add);
            _delete = AddButton("Delete", UiKeys.ChatTabDelete, 1, bottom, Delete);
            AddButton("Defaults", UiKeys.ChatTabDefaults, 2, bottom, Defaults);
            AddButton("Save", UiKeys.ChatTabSave, 0, 0f, Save);
        }

        public override void Refresh(PlayerStateSnapshot state)
        {
        }

        /// <summary>Opening starts a fresh draft from the account's tabs.</summary>
        protected override void OnVisibilityChanged(bool open)
        {
            if (!open)
            {
                return;
            }
            Load(Ctx.State.Player?.ChatTabs ?? Ctx.Content.DefaultChatTabs);
        }

        private void Load(ChatTabSnapshot[] tabs)
        {
            _draft.Clear();
            foreach (ChatTabSnapshot tab in tabs)
            {
                _draft.Add(new ChatTabSnapshot { Name = tab.Name, NameKey = tab.NameKey, Channels = (ChatChannel[])tab.Channels.Clone() });
            }
            _selected = 0;
            Show();
        }

        private void Select(int index)
        {
            StoreName();
            _selected = Mathf.Clamp(index, 0, _draft.Count - 1);
            Show();
        }

        /// <summary>A typed name replaces the default's name; an empty field keeps the default name (if the tab has one).</summary>
        private void StoreName()
        {
            if (_selected >= _draft.Count)
            {
                return;
            }
            string typed = _name.text?.Trim();
            _draft[_selected].Name = string.IsNullOrEmpty(typed) ? null : typed;
            UpdateList();
        }

        private void Toggle(ChatChannel channel)
        {
            ChatTabSnapshot tab = _draft[_selected];
            var channels = new List<ChatChannel>(tab.Channels);
            if (!channels.Remove(channel))
            {
                channels.Add(channel);
            }
            tab.Channels = channels.ToArray();
            Show();
        }

        private void Add()
        {
            StoreName();
            if (_draft.Count >= Ctx.Content.Config.ChatTabsMax)
            {
                return;
            }
            _draft.Add(new ChatTabSnapshot { Name = Ctx.Text.Format(UiKeys.ChatTabNewName, _draft.Count + 1), Channels = new[] { ChatChannel.World } });
            _selected = _draft.Count - 1;
            Show();
        }

        private void Delete()
        {
            if (_draft.Count <= 1)
            {
                return;
            }
            _draft.RemoveAt(_selected);
            _selected = Mathf.Clamp(_selected, 0, _draft.Count - 1);
            Show();
        }

        private void Defaults() => Load(Ctx.Content.DefaultChatTabs);

        private void Save()
        {
            StoreName();
            Ctx.Commands?.SetChatTabs(_draft.ToArray());
        }

        private void Show()
        {
            UpdateList();
            if (_selected >= _draft.Count)
            {
                return;
            }
            ChatTabSnapshot tab = _draft[_selected];
            _name.text = tab.Name ?? string.Empty;
            _namePlaceholder.text = string.IsNullOrEmpty(tab.NameKey) ? string.Empty : Ctx.Text.Get(tab.NameKey);
            for (int i = 0; i < Channels.Length; i++)
            {
                bool on = Array.IndexOf(tab.Channels, Channels[i]) >= 0;
                string name = Ctx.Text.Get(UiKeys.ChatChannelPrefix + Channels[i].ToString().ToLowerInvariant());
                _channelLabels[i].text = Ctx.Text.Format(on ? UiKeys.CheckOn : UiKeys.CheckOff, name);
                _channelLabels[i].color = on ? HudPalette.Positive : HudPalette.TextDim;
            }
            _add.interactable = _draft.Count < Ctx.Content.Config.ChatTabsMax;
            _delete.interactable = _draft.Count > 1;
        }

        private void UpdateList()
        {
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                bool shown = i < _draft.Count;
                _tabButtons[i].gameObject.SetActive(shown);
                if (shown)
                {
                    _tabLabels[i].text = string.IsNullOrEmpty(_draft[i].Name) ? Ctx.Text.Get(_draft[i].NameKey) : _draft[i].Name;
                    _tabLabels[i].color = i == _selected ? HudPalette.Cast : HudPalette.Text;
                }
            }
        }

        private Text Label(string name, float x, ref float y, float width)
        {
            Text text = UiFactory.Label(name, Content, Ctx.Font, HudLayout.FontNormal, TextAnchor.MiddleLeft, HudPalette.Text);
            UiFactory.Place(text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, y), new Vector2(width, HudLayout.RowHeight));
            y -= HudLayout.RowHeight + HudLayout.Gap;
            return text;
        }

        /// <summary>Buttons along the bottom: column 0..2, y from the bottom edge.</summary>
        private Button AddButton(string name, string key, int column, float y, UnityEngine.Events.UnityAction onClick)
        {
            Button button = UiFactory.Button(name, Content, Ctx.Font, Ctx.Text.Get(key), onClick);
            UiFactory.Place((RectTransform)button.transform, Vector2.zero, Vector2.zero,
                new Vector2(column * (ButtonWidth + HudLayout.Gap), y), new Vector2(ButtonWidth, HudLayout.MenuButtonHeight));
            return button;
        }
    }
}
