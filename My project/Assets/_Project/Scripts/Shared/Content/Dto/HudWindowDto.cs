using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>Window registry entry (02, 7.13: HudWindowDefinition). Windows are built in code for now, so there is no PrefabRef yet.</summary>
    [Serializable]
    public class HudWindowDto
    {
        public string Id;
        public string NameKey;
        public string Icon;
        public string MenuZone;
        public int MenuOrder;
        public string HotkeyBinding;
        public string OpenedBy;
        public bool Singleton;
    }
}
