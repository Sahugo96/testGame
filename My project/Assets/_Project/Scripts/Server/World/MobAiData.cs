using Game.Shared.Core;

namespace Game.Server.World
{
    /// <summary>What a mob's AI is doing (module 10).</summary>
    public enum MobAiState : byte
    {
        Idle,
        Chase,
        /// <summary>Leashed: walking home, not fighting; health is restored on arrival.</summary>
        Return,
    }

    /// <summary>Per-mob AI state: current target, ability cooldowns (by the profile's ability index) and the shared pause.</summary>
    public struct MobAiData
    {
        public MobAiState State;
        public EntityId TargetId;
        public float GlobalCooldown;
        public float[] AbilityCooldowns;
    }
}
