using Game.Shared.Core;

namespace Game.Server.World
{
    public enum DuelStage : byte
    {
        Request,
        Countdown,
        Active,
    }

    /// <summary>One duel between two players: request → countdown → active (question 70).</summary>
    public struct DuelData
    {
        public int Id;
        public EntityId Challenger;
        public EntityId Target;
        public DuelStage Stage;
        /// <summary>Server time when the current stage ends (request expires, countdown ends, duel is a draw).</summary>
        public float StageEndsAt;
        public Vec3 Center;

        public bool Involves(EntityId id) => Challenger == id || Target == id;

        public EntityId Opponent(EntityId id) => Challenger == id ? Target : Challenger;
    }
}
