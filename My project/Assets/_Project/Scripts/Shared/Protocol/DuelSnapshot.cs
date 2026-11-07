using Game.Shared.Core;

namespace Game.Shared.Protocol
{
    /// <summary>What the player sees of their duel.</summary>
    public enum DuelPhase : byte
    {
        None = 0,
        IncomingRequest = 1,
        OutgoingRequest = 2,
        Countdown = 3,
        Active = 4,
    }

    /// <summary>Duel state of the owning player (part of <see cref="PlayerStateSnapshot"/>). Remaining = seconds of the current phase.</summary>
    public struct DuelSnapshot
    {
        public DuelPhase Phase;
        public EntityId OpponentId;
        public float Remaining;
        public float CenterX;
        public float CenterZ;
        public float Radius;
    }
}
