using System;
using System.Collections.Generic;
using Game.Server.Players;
using Game.Shared.Content.Dto;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Server.Systems
{
    /// <summary>
    /// Emotes (02, 7.7 Г): the client names an emote from the content's white list, the server checks it and the rate
    /// limit and tells everyone who sees the player. The emote changes nothing in the simulation: how it looks (and when
    /// it stops — on a step, on death) is the client's business.
    /// </summary>
    public sealed class EmoteSystem
    {
        private readonly ServerContext _ctx;
        private readonly HashSet<string> _known = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<EntityId, float> _lastPlayed = new Dictionary<EntityId, float>();

        public EmoteSystem(ServerContext ctx)
        {
            _ctx = ctx;
            foreach (EmoteDto emote in ctx.Content.Config.Emotes ?? new EmoteDto[0])
            {
                _known.Add(emote.Id);
            }
        }

        public void Play(PlayerCharacter player, string emoteId)
        {
            if (emoteId == null || !_known.Contains(emoteId))
            {
                return;   // not from the white list: a broken or dishonest client, nothing to answer
            }
            if (!_ctx.World.IsAlive(player.EntityId))
            {
                CombatEvents.Reject(player, RejectReasons.Dead);
                return;
            }
            if (_lastPlayed.TryGetValue(player.EntityId, out float last) && _ctx.Time - last < _ctx.Content.Config.EmoteCooldownSeconds)
            {
                CombatEvents.Reject(player, RejectReasons.EmoteTooFast);
                return;
            }
            _lastPlayed[player.EntityId] = _ctx.Time;
            var message = new EmoteMessage { EntityId = player.EntityId, EmoteId = emoteId };
            foreach (PlayerCharacter observer in _ctx.Players.Values)
            {
                if (observer == player || InterestSystem.InRange(_ctx, observer, player.EntityId))
                {
                    observer.Outbox.Replies.Add(message);
                }
            }
        }

        public void OnPlayerRemoved(EntityId id) => _lastPlayed.Remove(id);
    }
}
