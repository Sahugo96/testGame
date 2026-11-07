using Game.Shared.Core;

namespace Game.Server.Systems
{
    /// <summary>
    /// Context of one hit (01 §13 hook, module 8): PvP or PvE, so balance can use separate coefficients
    /// (now only <c>config.PvpDamageMultiplier</c>).
    /// </summary>
    public readonly struct CombatContext
    {
        public readonly EntityId Source;
        public readonly EntityId Target;
        public readonly bool IsPvp;

        public CombatContext(EntityId source, EntityId target, bool isPvp)
        {
            Source = source;
            Target = target;
            IsPvp = isPvp;
        }
    }
}
