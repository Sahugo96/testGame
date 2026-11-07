namespace Game.Shared.Protocol
{
    /// <summary>A class skill as seen by its owner: mastery and unlocked variants.</summary>
    public struct SkillStateSnapshot
    {
        public string SkillId;
        public bool Unlocked;
        public float Mastery;
        public float Efficiency;
        public bool EmpoweredAvailable;
        public bool WeakenedAvailable;
    }
}
