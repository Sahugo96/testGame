using Game.Server.Systems;
using Game.Shared.Content;

namespace Game.Server.Effects
{
    /// <summary>Removes Amount dispellable debuffs of DispelType (empty = any) from the target (03, 7.12: cleansing).</summary>
    public sealed class DispelEffectHandler : IEffectHandler
    {
        private readonly StatusSystem _statuses;

        public DispelEffectHandler(StatusSystem statuses) => _statuses = statuses;

        public string Key => EffectKeys.Dispel;

        public void Apply(EffectContext context) =>
            _statuses.Dispel(context.TargetId, StatusKind.Debuff, context.Effect.DispelType, (int)context.Effect.Amount);
    }
}
