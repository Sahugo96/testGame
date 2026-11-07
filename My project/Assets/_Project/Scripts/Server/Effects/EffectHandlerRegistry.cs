using System.Collections.Generic;
using Game.Shared.Core;

namespace Game.Server.Effects
{
    /// <summary>Effect handlers by key (01, 7.10). New effects are added by registration, not by editing the core.</summary>
    public sealed class EffectHandlerRegistry
    {
        private readonly Dictionary<string, IEffectHandler> _handlers = new Dictionary<string, IEffectHandler>();

        public ICollection<string> Keys => _handlers.Keys;

        public void Register(IEffectHandler handler) => _handlers[handler.Key] = handler;

        public void Apply(EffectContext context)
        {
            if (_handlers.TryGetValue(context.Effect.Key, out IEffectHandler handler))
            {
                handler.Apply(context);
                return;
            }
            Log.Error(LogTags.Skills, $"No effect handler for '{context.Effect.Key}'");
        }
    }
}
