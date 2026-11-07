namespace Game.Server.Effects
{
    /// <summary>Applies one kind of effect; registered by key (01, 7.10).</summary>
    public interface IEffectHandler
    {
        string Key { get; }

        void Apply(EffectContext context);
    }
}
