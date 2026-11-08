using System;
using System.Collections.Generic;
using System.Text;
using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Server.Systems
{
    /// <summary>
    /// Chat (module 14): Say — players within InterestRadius; World — everyone; Group — the group or raid; Whisper — one
    /// player by character name (the sender gets a copy). The server trims and limits the text (ChatMaxLength, no control
    /// characters) and the rate (ChatMinIntervalSeconds). System lines (loot votes, 06 7.8) go to a group with a TextKey.
    /// </summary>
    public sealed class ChatSystem
    {
        private readonly ServerContext _ctx;
        private readonly GroupSystem _groups;
        private readonly Dictionary<EntityId, float> _lastSent = new Dictionary<EntityId, float>();

        public ChatSystem(ServerContext ctx, GroupSystem groups)
        {
            _ctx = ctx;
            _groups = groups;
        }

        public void Send(PlayerCharacter player, SendChatCommand command)
        {
            string text = Clean(command.Text, _ctx.Content.Config.ChatMaxLength);
            if (text.Length == 0)
            {
                return;
            }
            if (_lastSent.TryGetValue(player.EntityId, out float last) && _ctx.Time - last < _ctx.Content.Config.ChatMinIntervalSeconds)
            {
                CombatEvents.Reject(player, RejectReasons.ChatTooFast);
                return;
            }
            var message = new ChatMessage
            {
                Channel = command.Channel,
                SenderId = player.EntityId,
                SenderName = NameOf(player.EntityId),
                Text = text,
            };
            switch (command.Channel)
            {
                case ChatChannel.World:
                    foreach (PlayerCharacter other in _ctx.Players.Values)
                    {
                        other.Outbox.Chat.Add(message);
                    }
                    break;
                case ChatChannel.Group:
                    if (!_groups.TryGetGroup(player.EntityId, out GroupData group))
                    {
                        CombatEvents.Reject(player, RejectReasons.GroupNotInGroup);
                        return;
                    }
                    ToGroup(group, message);
                    break;
                case ChatChannel.Whisper:
                    PlayerCharacter target = FindByName(command.Target);
                    if (target == null)
                    {
                        CombatEvents.Reject(player, RejectReasons.ChatNoSuchPlayer);
                        return;
                    }
                    message.TargetName = NameOf(target.EntityId);
                    target.Outbox.Chat.Add(message);
                    if (target != player)
                    {
                        player.Outbox.Chat.Add(message);
                    }
                    break;
                case ChatChannel.Say:
                    foreach (PlayerCharacter other in _ctx.Players.Values)
                    {
                        if (InterestSystem.InRange(_ctx, other, player.EntityId))
                        {
                            other.Outbox.Chat.Add(message);
                        }
                    }
                    break;
                default:
                    CombatEvents.Reject(player, RejectReasons.ChatBadChannel);
                    return;
            }
            _lastSent[player.EntityId] = _ctx.Time;
        }

        /// <summary>A system line to every member of a group (06 7.8: votes are shown in the group chat).</summary>
        public void SystemToGroup(int groupId, string textKey, string name)
        {
            if (!_groups.TryGetGroupById(groupId, out GroupData group))
            {
                return;
            }
            ToGroup(group, new ChatMessage { Channel = ChatChannel.System, TextKey = textKey, SenderName = name });
        }

        /// <summary>A system line to one player: {1} = name (friend requests and the like).</summary>
        public void SystemToPlayer(PlayerCharacter player, string textKey, string name) =>
            player.Outbox.Chat.Add(new ChatMessage { Channel = ChatChannel.System, TextKey = textKey, SenderName = name });

        /// <summary>The account's chat tabs changed (persistence saves the account).</summary>
        public event Action<PlayerCharacter> TabsChanged;

        /// <summary>The player's tabs, or the defaults from config.</summary>
        public ChatTabSnapshot[] Tabs(PlayerCharacter player) => player.ChatTabs ?? _ctx.Content.DefaultChatTabs;

        /// <summary>
        /// Replaces the account's chat tabs: 1..ChatTabsMax tabs, each with its own name (cleaned, up to ChatTabNameMax) or a
        /// default tab's NameKey, and one or more distinct channels. Tabs equal to the defaults are stored as "defaults".
        /// </summary>
        public void SetTabs(PlayerCharacter player, ChatTabSnapshot[] tabs)
        {
            ChatTabSnapshot[] clean = CleanTabs(tabs);
            if (clean == null)
            {
                CombatEvents.Reject(player, RejectReasons.ChatTabsInvalid);
                return;
            }
            player.ChatTabs = SameTabs(clean, _ctx.Content.DefaultChatTabs) ? null : clean;
            TabsChanged?.Invoke(player);
        }

        /// <summary>Checked copy of tabs from a client or a save; null if anything is wrong.</summary>
        public ChatTabSnapshot[] CleanTabs(ChatTabSnapshot[] tabs)
        {
            if (tabs == null || tabs.Length == 0 || tabs.Length > _ctx.Content.Config.ChatTabsMax)
            {
                return null;
            }
            var result = new ChatTabSnapshot[tabs.Length];
            for (int i = 0; i < tabs.Length; i++)
            {
                ChatTabSnapshot tab = tabs[i];
                if (tab == null || tab.Channels == null || tab.Channels.Length == 0)
                {
                    return null;
                }
                string name = Clean(tab.Name, _ctx.Content.Config.ChatTabNameMax);
                bool knownKey = !string.IsNullOrEmpty(tab.NameKey) && IsDefaultKey(tab.NameKey);
                if (name.Length == 0 && !knownKey)
                {
                    return null;
                }
                var channels = new List<ChatChannel>();
                foreach (ChatChannel channel in tab.Channels)
                {
                    if (!Enum.IsDefined(typeof(ChatChannel), channel) || channels.Contains(channel))
                    {
                        return null;
                    }
                    channels.Add(channel);
                }
                result[i] = new ChatTabSnapshot { Name = name.Length > 0 ? name : null, NameKey = name.Length > 0 ? null : tab.NameKey, Channels = channels.ToArray() };
            }
            return result;
        }

        private bool IsDefaultKey(string key)
        {
            foreach (ChatTabSnapshot tab in _ctx.Content.DefaultChatTabs)
            {
                if (tab.NameKey == key)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool SameTabs(ChatTabSnapshot[] a, ChatTabSnapshot[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].Name != null || a[i].NameKey != b[i].NameKey || a[i].Channels.Length != b[i].Channels.Length)
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

        public void OnPlayerRemoved(EntityId id) => _lastSent.Remove(id);

        /// <summary>Finds an online character by name, ignoring case.</summary>
        public PlayerCharacter FindByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }
            string wanted = name.Trim();
            foreach (PlayerCharacter player in _ctx.Players.Values)
            {
                if (string.Equals(NameOf(player.EntityId), wanted, StringComparison.OrdinalIgnoreCase))
                {
                    return player;
                }
            }
            return null;
        }

        private void ToGroup(GroupData group, ChatMessage message)
        {
            foreach (EntityId member in group.Members)
            {
                _ctx.GetPlayer(member)?.Outbox.Chat.Add(message);
            }
        }

        private string NameOf(EntityId id) => _ctx.World.Identities.Has(id) ? _ctx.World.Identities.Get(id).DisplayName ?? string.Empty : string.Empty;

        /// <summary>No control characters, trimmed, at most <paramref name="max"/> characters.</summary>
        private static string Clean(string text, int max)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            var sb = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (!char.IsControl(c))
                {
                    sb.Append(c);
                }
            }
            string clean = sb.ToString().Trim();
            return clean.Length > max ? clean.Substring(0, max) : clean;
        }
    }
}
