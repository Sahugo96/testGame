using System;
using Game.Client.Net;
using Game.Server;
using Game.Server.Players;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Host
{
    /// <summary>
    /// In-process "network": runs the authoritative server at its fixed tick and delivers its messages to the client.
    /// Temporary until Mirror (module 1); the client sees only <see cref="IServerConnection"/>.
    /// </summary>
    public sealed class LocalServerConnection : IServerConnection
    {
        private const int MaxTicksPerFrame = 5;

        private readonly GameServer _server;
        private float _accumulator;

        public LocalServerConnection(GameServer server, EntityId playerId)
        {
            _server = server;
            LocalPlayerId = playerId;
        }

        public EntityId LocalPlayerId { get; }

        public float TickInterval => _server.TickInterval;

        public event Action<WorldSnapshot> WorldReceived;
        public event Action<PlayerStateSnapshot> PlayerStateReceived;
        public event Action<CombatEventMessage> CombatEventReceived;
        public event Action<CommandRejectedMessage> RejectedReceived;

        public void Send(IGameCommand command) => _server.Submit(LocalPlayerId, command);

        public void Update(float deltaTime)
        {
            _accumulator += deltaTime;
            int ticks = 0;
            while (_accumulator >= _server.TickInterval && ticks < MaxTicksPerFrame)
            {
                _accumulator -= _server.TickInterval;
                _server.Tick();
                Deliver();
                ticks++;
            }
            if (ticks == MaxTicksPerFrame)
            {
                _accumulator = 0f;
            }
        }

        private void Deliver()
        {
            PlayerCharacter player = _server.GetPlayer(LocalPlayerId);
            if (player == null)
            {
                return;
            }
            PlayerOutbox outbox = player.Outbox;
            foreach (CombatEventMessage message in outbox.CombatEvents)
            {
                CombatEventReceived?.Invoke(message);
            }
            outbox.CombatEvents.Clear();
            foreach (CommandRejectedMessage message in outbox.Rejections)
            {
                RejectedReceived?.Invoke(message);
            }
            outbox.Rejections.Clear();
            if (outbox.World != null)
            {
                WorldReceived?.Invoke(outbox.World);
            }
            if (outbox.State != null)
            {
                PlayerStateReceived?.Invoke(outbox.State);
            }
        }
    }
}
