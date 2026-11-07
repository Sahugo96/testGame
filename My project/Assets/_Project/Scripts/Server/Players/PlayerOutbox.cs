using System.Collections.Generic;
using Game.Shared.Protocol;

namespace Game.Server.Players
{
    /// <summary>Messages prepared for one player: latest snapshots plus queued events. The transport drains it.</summary>
    public sealed class PlayerOutbox
    {
        public WorldSnapshot World;
        public PlayerStateSnapshot State;
        public readonly List<CombatEventMessage> CombatEvents = new List<CombatEventMessage>();
        public readonly List<CommandRejectedMessage> Rejections = new List<CommandRejectedMessage>();
        public readonly List<NotificationMessage> Notifications = new List<NotificationMessage>();
    }
}
