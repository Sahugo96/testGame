using Game.Shared.Core;

namespace Game.Shared.Protocol
{
    /// <summary>Combat log entry (damage, heal, restore, death) sent to players who see the target.</summary>
    public struct CombatEventMessage
    {
        public EntityId SourceId;
        public EntityId TargetId;
        public string SkillId;
        public CombatEventKind Kind;
        public int Amount;
        public int CritTier;
        /// <summary>Damage soaked by shields (not included in Amount).</summary>
        public int Absorbed;
    }
}
