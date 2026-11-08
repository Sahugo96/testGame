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
        /// <summary>Languages to choose from (Settings → Game); module 4, question 81.</summary>
        public ILanguageService Languages;
        public Font Font;
        public Camera Camera;
        public ModifierTracker Modifiers;
        /// <summary>User settings (module 17): device graphics, account keys / camera / interface.</summary>
        public Settings.SettingsService Settings;

        /// <summary>Station (whetstone, anvil, ...) the player is interacting with; set when its window is opened by clicking the station.</summary>
        public Game.Shared.Core.EntityId ActiveStation;

        /// <summary>Raised by the settings window; the host reconnects the player with another class (dev tool).</summary>
        public event Action<string> ClassSwitchRequested;

        public void RequestClassSwitch(string classId) => ClassSwitchRequested?.Invoke(classId);

        /// <summary>Raised by widgets and windows that open another window (chat tab settings); HudRoot opens it.</summary>
        public event Action<string> WindowRequested;

        public void RequestWindow(string windowId) => WindowRequested?.Invoke(windowId);

        /// <summary>Raised by the friends window: open the chat on the whisper channel to this character.</summary>
        public event Action<string> WhisperRequested;

        public void RequestWhisper(string name) => WhisperRequested?.Invoke(name);

        /// <summary>Raised by the settings window: leave the game and return to the start menu.</summary>
        public event Action LeaveRequested;

        public void RequestLeave() => LeaveRequested?.Invoke();
    }
}
