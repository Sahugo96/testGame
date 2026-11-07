using Game.Server.Systems;
using Game.Shared.Content;

namespace Game.Server.Effects
{
    /// <summary>Restores a resource (health or mana) of the target.</summary>
    public sealed class RestoreResourceEffectHandler : IEffectHandler
    {
        private readonly ResourceSystem _resources;

        public RestoreResourceEffectHandler(ResourceSystem resources) => _resources = resources;

        public string Key => EffectKeys.RestoreResource;

        public void Apply(EffectContext context) =>
            _resources.Restore(context.TargetId, context.Effect.Resource, context.Effect.Amount * context.Efficiency, context.SourceId, context.SkillId);
    }
}
