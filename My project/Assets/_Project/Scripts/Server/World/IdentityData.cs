using Game.Shared.Protocol;

namespace Game.Server.World
{
    /// <summary>What the entity is: kind, content definition, display name key and faction.</summary>
    public struct IdentityData
    {
        public EntityKind Kind;
        public string DefinitionId;
        public string NameKey;
        public string DisplayName;
        public string Faction;
    }
}
