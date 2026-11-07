using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Mob spawn point on the XZ plane.</summary>
    [Serializable]
    public class SpawnDto
    {
        public string MobId;
        public float X;
        public float Z;
    }
}
