using System;
using System.Collections.Generic;
using Game.Client.Net;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Client.State
{
    /// <summary>
    /// Client-side model (ViewModel source): the last snapshots received from the server. Views read it and subscribe
    /// to its events; they never talk to the network directly (02, 7.13).
    /// </summary>
    public sealed class ClientState
    {
        private readonly Dictionary<EntityId, EntitySnapshot> _entities = new Dictionary<EntityId, EntitySnapshot>();

        public WorldSnapshot World { get; private set; }
        public PlayerStateSnapshot Player { get; private set; }
        public EntityId LocalPlayerId { get; private set; }

        public event Action WorldChanged;
        public event Action PlayerChanged;
        public event Action<CombatEventMessage> CombatEvent;
        public event Action<string> Rejected;

        public void Bind(IServerConnection connection)
        {
            LocalPlayerId = connection.LocalPlayerId;
            connection.WorldReceived += OnWorld;
            connection.PlayerStateReceived += OnPlayer;
            connection.CombatEventReceived += OnCombatEvent;
            connection.RejectedReceived += OnRejected;
        }

        public void Unbind(IServerConnection connection)
        {
            connection.WorldReceived -= OnWorld;
            connection.PlayerStateReceived -= OnPlayer;
            connection.CombatEventReceived -= OnCombatEvent;
            connection.RejectedReceived -= OnRejected;
            _entities.Clear();
            World = null;
            Player = null;
        }

        public bool TryGetEntity(EntityId id, out EntitySnapshot entity) => _entities.TryGetValue(id, out entity);

        public IEnumerable<EntitySnapshot> Entities => _entities.Values;

        private void OnWorld(WorldSnapshot snapshot)
        {
            World = snapshot;
            _entities.Clear();
            foreach (EntitySnapshot entity in snapshot.Entities)
            {
                _entities[entity.Id] = entity;
            }
            WorldChanged?.Invoke();
        }

        private void OnCombatEvent(CombatEventMessage message) => CombatEvent?.Invoke(message);

        private void OnRejected(CommandRejectedMessage message) => Rejected?.Invoke(message.ReasonKey);

        private void OnPlayer(PlayerStateSnapshot snapshot)
        {
            Player = snapshot;
            PlayerChanged?.Invoke();
        }
    }
}
