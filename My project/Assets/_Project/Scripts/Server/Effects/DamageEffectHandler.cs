using Game.Server.Systems;
using Game.Shared.Content;

namespace Game.Server.Effects
{
    /// <summary>Deals damage through <see cref="CombatSystem"/> (03, 7.12 order).</summary>
    public sealed class DamageEffectHandler : IEffectHandler
    {
        private readonly CombatSystem _combat;

        public DamageEffectHandler(CombatSystem combat) => _combat = combat;

        public string Key => EffectKeys.Damage;

        public void Apply(EffectContext context) =>
            _combat.DealDamage(context.SourceId, context.TargetId, context.SkillId, context.Effect.Amount, context.Efficiency);
    }
}
