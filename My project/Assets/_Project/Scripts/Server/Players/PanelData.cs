using System.Collections.Generic;

namespace Game.Server.Players
{
    /// <summary>Quick panels of one character (02, 7.7; module 18): references only (skill ids, item definition ids, instance ids).</summary>
    public sealed class PanelData
    {
        public string[] SkillBar = new string[0];
        public string[] ConsumableBar = new string[0];
        public LoadoutData[] Loadouts = new LoadoutData[0];
        /// <summary>The client gets the panels again after this tick.</summary>
        public bool ToSend = true;
    }

    public sealed class LoadoutData
    {
        public readonly List<LoadoutItemData> Items = new List<LoadoutItemData>();
    }

    public struct LoadoutItemData
    {
        public string SlotId;
        public string InstanceId;
        public string DefinitionId;
    }
}
