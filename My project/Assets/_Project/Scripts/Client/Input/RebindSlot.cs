using UnityEngine.InputSystem;

namespace Game.Client.Input
{
    /// <summary>One rebindable binding: an action and the index of its binding, with how the settings window names it.</summary>
    public sealed class RebindSlot
    {
        public RebindSlot(InputAction action, int bindingIndex, string labelKey, string labelArgument)
        {
            Action = action;
            BindingIndex = bindingIndex;
            LabelKey = labelKey;
            LabelArgument = labelArgument;
        }

        public InputAction Action { get; }
        public int BindingIndex { get; }
        /// <summary>Localization key of the name; for skill slots {0} = number, for windows {0} = the window id (named by its NameKey).</summary>
        public string LabelKey { get; }
        public string LabelArgument { get; }

        /// <summary>Stable id stored on the account: "ActionName/bindingIndex".</summary>
        public string Id => Action.name + "/" + BindingIndex;

        public string Path => Action.bindings[BindingIndex].effectivePath;
    }
}
