using System;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Client.Net
{
    /// <summary>
    /// Client view of the server. Today it is an in-process host (Game.Host); in module 1 a Mirror implementation
    /// replaces it without touching client code.
    /// </summary>
    public interface IServerConnection
    {
        EntityId LocalPlayerId { get; }

        float TickInterval { get; }

        void Send(IGameCommand command);

        event Action<WorldSnapshot> WorldReceived;
        event Action<PlayerStateSnapshot> PlayerStateReceived;
        event Action<CombatEventMessage> CombatEventReceived;
        event Action<CommandRejectedMessage> RejectedReceived;
    }
}
