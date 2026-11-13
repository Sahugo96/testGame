namespace Game.Shared.Protocol
{
    /// <summary>
    /// Puts a skill into a slot of the skill panels (02, 7.7 А; module 18): index over all rows (row × slots per row + column);
    /// an empty id clears the slot. Only the character's class skills; the server keeps the layout per character.
    /// </summary>
    public struct SetSkillSlotCommand : IGameCommand
    {
        public int Index;
        public string SkillId;
    }

    /// <summary>Puts a consumable (item definition id) into a slot of the consumable panel (02, 7.7 Б); empty clears it.</summary>
    public struct SetConsumableSlotCommand : IGameCommand
    {
        public int Index;
        public string ItemId;
    }

    /// <summary>Remembers the current equipment in loadout Index (02, 7.7 В).</summary>
    public struct SaveLoadoutCommand : IGameCommand
    {
        public int Index;
    }

    /// <summary>CmdSwapLoadout (02, 7.7 В): puts on the loadout's items (in combat — weapons only).</summary>
    public struct ApplyLoadoutCommand : IGameCommand
    {
        public int Index;
    }

    /// <summary>One remembered item of a loadout: the equipment slot, the instance and what it is (to show it when it is gone).</summary>
    public struct LoadoutItemSnapshot
    {
        public string SlotId;
        public string InstanceId;
        public string DefinitionId;
    }

    public sealed class LoadoutSnapshot
    {
        public LoadoutItemSnapshot[] Items = new LoadoutItemSnapshot[0];
    }

    /// <summary>
    /// Server → client after login and after every change (module 18): skill panel slots (skill ids, null = empty), consumable
    /// panel slots (item definition ids) and the loadouts. Sent only when changed, not with every state snapshot.
    /// </summary>
    public sealed class PanelsMessage
    {
        public string[] SkillBar = new string[0];
        public string[] ConsumableBar = new string[0];
        public LoadoutSnapshot[] Loadouts = new LoadoutSnapshot[0];
    }
}
