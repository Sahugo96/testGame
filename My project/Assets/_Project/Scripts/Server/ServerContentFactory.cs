using System.Collections.Generic;
using Game.Shared.Content;
using Game.Shared.Content.Items;

namespace Game.Server
{
    /// <summary>Builds and validates content for the server. The server refuses to start with broken content (01, 7.10 principle 6).</summary>
    public static class ServerContentFactory
    {
        /// <summary>Effect keys that have handlers in <see cref="GameServer"/>.</summary>
        public static readonly string[] BuiltInEffectKeys = { EffectKeys.Damage, EffectKeys.Heal, EffectKeys.RestoreResource, EffectKeys.ApplyStatus, EffectKeys.Dispel };

        public static ContentDatabase Build(ContentFiles files, List<string> errors) =>
            ContentBuilder.Build(files, ItemComponentRegistry.CreateDefault(), BuiltInEffectKeys, errors);
    }
}
