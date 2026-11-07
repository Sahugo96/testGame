using System;
using System.Collections.Generic;
using Game.Server.Players;
using Game.Server.World;
using Game.Shared.Content.Dto;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Server.Systems
{
    /// <summary>
    /// Duels between two players (question 70, temporary defaults): a request to a nearby player, a countdown, then the two
    /// become hostile only to each other inside an arena circle. The duel is not to the death: the lethal hit from the
    /// opponent leaves the loser with <c>DuelLoserHealth</c>. Leaving the circle, dying from anything else, surrendering or
    /// disconnecting loses; the time limit is a draw. Outsiders cannot help or harm duelists (hostility and assist rules).
    /// </summary>
    public sealed class DuelSystem
    {
        private readonly ServerContext _ctx;
        private readonly ResourceSystem _resources;
        private readonly Dictionary<int, DuelData> _duels = new Dictionary<int, DuelData>();
        private readonly Dictionary<EntityId, int> _byPlayer = new Dictionary<EntityId, int>();
        private readonly List<int> _ids = new List<int>();
        private int _nextId = 1;

        /// <summary>Raised when an accepted duel (countdown or active) ends: (a, b). Casts at each other are cancelled.</summary>
        public event Action<EntityId, EntityId> Ended;

        public DuelSystem(ServerContext ctx, ResourceSystem resources)
        {
            _ctx = ctx;
            _resources = resources;
        }

        private GameConfig Config => _ctx.Content.Config;

        /// <summary>True only while the duel between a and b is active (not during request or countdown).</summary>
        public bool AreOpponents(EntityId a, EntityId b) =>
            a != b && TryGet(a, out DuelData duel) && duel.Stage == DuelStage.Active && duel.Opponent(a) == b;

        /// <summary>Accepted duelists (countdown or active) are closed to everyone except themselves.</summary>
        public bool IsLockedIn(EntityId id) => TryGet(id, out DuelData duel) && duel.Stage != DuelStage.Request;

        public bool TryGet(EntityId id, out DuelData duel)
        {
            if (_byPlayer.TryGetValue(id, out int duelId))
            {
                duel = _duels[duelId];
                return true;
            }
            duel = default;
            return false;
        }

        public void Request(PlayerCharacter player, EntityId targetId)
        {
            EntityId self = player.EntityId;
            ServerWorld world = _ctx.World;
            if (!world.IsAlive(self))
            {
                CombatEvents.Reject(player, RejectReasons.Dead);
                return;
            }
            if (targetId == self || _ctx.GetPlayer(targetId) == null)
            {
                CombatEvents.Reject(player, RejectReasons.DuelNotPlayer);
                return;
            }
            if (!world.IsAlive(targetId))
            {
                CombatEvents.Reject(player, RejectReasons.InvalidTarget);
                return;
            }
            if (_byPlayer.ContainsKey(self))
            {
                CombatEvents.Reject(player, RejectReasons.DuelBusy);
                return;
            }
            if (_byPlayer.ContainsKey(targetId))
            {
                CombatEvents.Reject(player, RejectReasons.DuelTargetBusy);
                return;
            }
            if (_resources.IsInCombat(self) || _resources.IsInCombat(targetId))
            {
                CombatEvents.Reject(player, RejectReasons.DuelInCombat);
                return;
            }
            if (Distance(self, targetId) > Config.DuelRequestRange)
            {
                CombatEvents.Reject(player, RejectReasons.OutOfRange);
                return;
            }
            var duel = new DuelData
            {
                Id = _nextId++,
                Challenger = self,
                Target = targetId,
                Stage = DuelStage.Request,
                StageEndsAt = _ctx.Time + Config.DuelRequestSeconds,
            };
            _duels.Add(duel.Id, duel);
            _byPlayer[self] = duel.Id;
            _byPlayer[targetId] = duel.Id;
            Notify(targetId, NotificationKeys.DuelRequested, true);
        }

        public void Respond(PlayerCharacter player, bool accept)
        {
            EntityId self = player.EntityId;
            if (!TryGet(self, out DuelData duel) || duel.Stage != DuelStage.Request || duel.Target != self)
            {
                CombatEvents.Reject(player, RejectReasons.DuelNoRequest);
                return;
            }
            if (!accept)
            {
                Remove(duel, false);
                Notify(duel.Challenger, NotificationKeys.DuelDeclined, false);
                return;
            }
            if (!_ctx.World.IsAlive(self) || !_ctx.World.IsAlive(duel.Challenger))
            {
                CombatEvents.Reject(player, RejectReasons.Dead);
                return;
            }
            if (_resources.IsInCombat(self) || _resources.IsInCombat(duel.Challenger))
            {
                CombatEvents.Reject(player, RejectReasons.DuelInCombat);
                return;
            }
            if (Distance(self, duel.Challenger) > Config.DuelRequestRange)
            {
                CombatEvents.Reject(player, RejectReasons.OutOfRange);
                return;
            }
            Vec3 a = _ctx.World.Transforms.Get(duel.Challenger).Position;
            Vec3 b = _ctx.World.Transforms.Get(self).Position;
            duel.Center = new Vec3((a.X + b.X) * 0.5f, 0f, (a.Z + b.Z) * 0.5f);
            duel.Stage = DuelStage.Countdown;
            duel.StageEndsAt = _ctx.Time + Config.DuelCountdownSeconds;
            _duels[duel.Id] = duel;
        }

        /// <summary>Request: withdraw (challenger) or decline (target). Countdown: cancel for both. Active: lose.</summary>
        public void Surrender(PlayerCharacter player)
        {
            EntityId self = player.EntityId;
            if (!TryGet(self, out DuelData duel))
            {
                CombatEvents.Reject(player, RejectReasons.DuelNoRequest);
                return;
            }
            switch (duel.Stage)
            {
                case DuelStage.Request:
                    Remove(duel, false);
                    Notify(duel.Opponent(self), self == duel.Target ? NotificationKeys.DuelDeclined : NotificationKeys.DuelCancelled, false);
                    break;
                case DuelStage.Countdown:
                    Cancel(duel);
                    break;
                default:
                    Finish(duel, duel.Opponent(self), self, NotificationKeys.DuelLost);
                    break;
            }
        }

        /// <summary>The opponent's hit would kill: the loser keeps DuelLoserHealth and the duel ends. Called by CombatSystem.</summary>
        public void DefeatByOpponent(EntityId winner, EntityId loser)
        {
            if (TryGet(loser, out DuelData duel) && duel.Stage == DuelStage.Active && duel.Opponent(loser) == winner)
            {
                Finish(duel, winner, loser, NotificationKeys.DuelLost);
            }
        }

        /// <summary>Death from anything else (a mob, own health cost): an active duel is lost, an earlier stage is cancelled.</summary>
        public void OnDied(EntityId victim)
        {
            if (!TryGet(victim, out DuelData duel))
            {
                return;
            }
            if (duel.Stage == DuelStage.Active)
            {
                Finish(duel, duel.Opponent(victim), victim, NotificationKeys.DuelLost);
            }
            else
            {
                Cancel(duel);
            }
        }

        /// <summary>Disconnect: an active duel is lost; otherwise the request or countdown is cancelled.</summary>
        public void OnPlayerRemoved(EntityId id)
        {
            if (!TryGet(id, out DuelData duel))
            {
                return;
            }
            if (duel.Stage == DuelStage.Active)
            {
                Finish(duel, duel.Opponent(id), id, null);
            }
            else
            {
                Cancel(duel);
            }
        }

        public void Update()
        {
            _ids.Clear();
            _ids.AddRange(_duels.Keys);
            foreach (int id in _ids)
            {
                if (!_duels.TryGetValue(id, out DuelData duel))
                {
                    continue;
                }
                bool expired = _ctx.Time >= duel.StageEndsAt;
                switch (duel.Stage)
                {
                    case DuelStage.Request:
                        if (expired)
                        {
                            Remove(duel, false);
                            Notify(duel.Challenger, NotificationKeys.DuelExpired, false);
                            Notify(duel.Target, NotificationKeys.DuelExpired, false);
                        }
                        break;
                    case DuelStage.Countdown:
                        if (expired)
                        {
                            duel.Stage = DuelStage.Active;
                            duel.StageEndsAt = _ctx.Time + Config.DuelMaxSeconds;
                            _duels[id] = duel;
                            Notify(duel.Challenger, NotificationKeys.DuelStarted, true);
                            Notify(duel.Target, NotificationKeys.DuelStarted, true);
                        }
                        break;
                    default:
                        UpdateActive(duel, expired);
                        break;
                }
            }
        }

        public DuelSnapshot Snapshot(EntityId id)
        {
            if (!TryGet(id, out DuelData duel))
            {
                return default;
            }
            DuelPhase phase;
            switch (duel.Stage)
            {
                case DuelStage.Request:
                    phase = duel.Target == id ? DuelPhase.IncomingRequest : DuelPhase.OutgoingRequest;
                    break;
                case DuelStage.Countdown:
                    phase = DuelPhase.Countdown;
                    break;
                default:
                    phase = DuelPhase.Active;
                    break;
            }
            return new DuelSnapshot
            {
                Phase = phase,
                OpponentId = duel.Opponent(id),
                Remaining = Math.Max(0f, duel.StageEndsAt - _ctx.Time),
                CenterX = duel.Center.X,
                CenterZ = duel.Center.Z,
                Radius = duel.Stage == DuelStage.Request ? 0f : Config.DuelRadius,
            };
        }

        private void UpdateActive(DuelData duel, bool expired)
        {
            bool challengerOut = Vec3.DistanceXZ(_ctx.World.Transforms.Get(duel.Challenger).Position, duel.Center) > Config.DuelRadius;
            bool targetOut = Vec3.DistanceXZ(_ctx.World.Transforms.Get(duel.Target).Position, duel.Center) > Config.DuelRadius;
            if (challengerOut && targetOut)
            {
                Draw(duel);
            }
            else if (challengerOut)
            {
                Finish(duel, duel.Target, duel.Challenger, NotificationKeys.DuelLostLeftArea);
            }
            else if (targetOut)
            {
                Finish(duel, duel.Challenger, duel.Target, NotificationKeys.DuelLostLeftArea);
            }
            else if (expired)
            {
                Draw(duel);
            }
        }

        private void Finish(DuelData duel, EntityId winner, EntityId loser, string loserKey)
        {
            Remove(duel, true);
            Notify(winner, NotificationKeys.DuelWon, true);
            if (loserKey != null)
            {
                Notify(loser, loserKey, false);
            }
            Log.Info(LogTags.Server, $"Duel {duel.Id}: {winner} won against {loser}");
        }

        private void Draw(DuelData duel)
        {
            Remove(duel, true);
            Notify(duel.Challenger, NotificationKeys.DuelDraw, false);
            Notify(duel.Target, NotificationKeys.DuelDraw, false);
        }

        private void Cancel(DuelData duel)
        {
            Remove(duel, duel.Stage != DuelStage.Request);
            Notify(duel.Challenger, NotificationKeys.DuelCancelled, false);
            Notify(duel.Target, NotificationKeys.DuelCancelled, false);
        }

        private void Remove(DuelData duel, bool accepted)
        {
            _duels.Remove(duel.Id);
            _byPlayer.Remove(duel.Challenger);
            _byPlayer.Remove(duel.Target);
            if (accepted)
            {
                Ended?.Invoke(duel.Challenger, duel.Target);
            }
        }

        private void Notify(EntityId id, string key, bool positive)
        {
            PlayerCharacter player = _ctx.GetPlayer(id);
            player?.Outbox.Notifications.Add(new NotificationMessage { TextKey = key, IsPositive = positive });
        }

        private float Distance(EntityId a, EntityId b) =>
            Vec3.DistanceXZ(_ctx.World.Transforms.Get(a).Position, _ctx.World.Transforms.Get(b).Position);
    }
}
