namespace Game.Server.Players
{
    /// <summary>One shared cooldown timer per skill (03, 7.2); its length comes from the variant that was used.</summary>
    public struct SkillCooldown
    {
        public float Remaining;
        public float Duration;
    }
}
