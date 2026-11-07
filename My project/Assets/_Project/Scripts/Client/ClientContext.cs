using System;
using Game.Client.Input;
using Game.Client.Localization;
using Game.Client.State;
using Game.Shared.Content;
using UnityEngine;

namespace Game.Client
{
    /// <summary>Services shared by client views (passed explicitly; no FindObjectOfType, no global singletons).</summary>
    public sealed class ClientContext
    {
        public ContentDatabase Content;
        public ClientState State;
        public ClientCommands Commands;
        public ILocalizationService Text;
        public Font Font;
        public Camera Camera;
        public ModifierTracker Modifiers;

        /// <summary>Raised by the settings window; the host reconnects the player with another class (dev tool).</summary>
        public event Action<string> ClassSwitchRequested;

        public void RequestClassSwitch(string classId) => ClassSwitchRequested?.Invoke(classId);
    }
}
