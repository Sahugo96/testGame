using Game.Shared.Content;
using Game.Shared.Core;

namespace Game.Server.World
{
    /// <summary>Current cast and global cooldown of a caster.</summary>
    public struct CastData
    {
        public bool IsCasting;
        public string SkillId;
        public SkillVariantKind Variant;
        public EntityId TargetId;
        public float Elapsed;
        public float Duration;
        public float GlobalCooldownRemaining;
        public float GlobalCooldownDuration;
    }
}
