namespace Game.Shared.Protocol
{
    /// <summary>CmdUseSkill { skillId, targetId, modifier, clientSeq } (03, 7.2). The server picks the variant itself.</summary>
    public struct UseSkillCommand : IGameCommand
    {
        public string SkillId;
        public Game.Shared.Core.EntityId TargetId;
        public Game.Shared.Content.SkillModifier Modifier;
        public int ClientSeq;
    }
}
