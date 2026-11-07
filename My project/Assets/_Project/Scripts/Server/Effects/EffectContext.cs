using Game.Shared.Content;
using Game.Shared.Core;

namespace Game.Server.Effects
{
    /// <summary>Everything an effect handler needs to apply one effect.</summary>
    public struct EffectContext
    {
        public ServerContext Server;
        public EntityId SourceId;
        public EntityId TargetId;
        public string SkillId;
        public EffectDefinition Effect;

        /// <summary>Mastery efficiency of the skill (questions 5, 54).</summary>
        public float Efficiency;
    }
}
