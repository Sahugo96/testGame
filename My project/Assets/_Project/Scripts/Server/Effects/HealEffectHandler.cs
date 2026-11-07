using Game.Server.Systems;
using Game.Shared.Content;

namespace Game.Server.Effects
{
    /// <summary>Heals through <see cref="CombatSystem"/>; the dead cannot be healed (question 3).</summary>
    public sealed class HealEffectHandler : IEffectHandler
    {
        private readonly CombatSystem _combat;

        public HealEffectHandler(CombatSystem combat) => _combat = combat;

        public string Key => EffectKeys.Heal;

        public void Apply(EffectContext context) =>
            _combat.Heal(context.SourceId, context.TargetId, context.SkillId, context.Effect.Amount, context.Efficiency);
    }
}
