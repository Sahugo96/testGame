namespace Game.Shared.Protocol
{
    /// <summary>Remaining cooldown of a skill.</summary>
    public struct SkillCooldownSnapshot
    {
        public string SkillId;
        public float Remaining;
        public float Duration;
    }
}
