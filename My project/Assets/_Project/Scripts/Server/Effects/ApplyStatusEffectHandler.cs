using Game.Server.Systems;
using Game.Shared.Content;

namespace Game.Server.Effects
{
    /// <summary>Puts a status on the target (03, 7.12); its strength is the skill's efficiency (mastery).</summary>
    public sealed class ApplyStatusEffectHandler : IEffectHandler
    {
        private readonly StatusSystem _statuses;

        public ApplyStatusEffectHandler(StatusSystem statuses) => _statuses = statuses;

        public string Key => EffectKeys.ApplyStatus;

        public void Apply(EffectContext context) =>
            _statuses.Apply(context.SourceId, context.TargetId, context.Effect.StatusId, context.Efficiency);
    }
}
