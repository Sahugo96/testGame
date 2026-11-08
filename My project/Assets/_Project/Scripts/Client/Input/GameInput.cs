using System.Collections.Generic;
using Game.Shared.Content.Dto;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Client.Input
{
    /// <summary>
    /// All gameplay input as Input Actions (frozen rule: input only through Input Actions). Built in code for now;
    /// rebinding UI arrives with settings (module 17). Window hotkeys come from the window registry data.
    /// </summary>
    public sealed class GameInput
    {
        private static readonly string[] SkillSlotKeys =
        {
            "<Keyboard>/1", "<Keyboard>/2", "<Keyboard>/3", "<Keyboard>/4", "<Keyboard>/5", "<Keyboard>/6",
            "<Keyboard>/7", "<Keyboard>/8", "<Keyboard>/9", "<Keyboard>/0", "<Keyboard>/minus", "<Keyboard>/equals",
        };

        private readonly InputActionMap _map = new InputActionMap("Gameplay");

        public readonly InputAction Move;
        public readonly InputAction Look;
        public readonly InputAction LookHold;
        public readonly InputAction Zoom;
        public readonly InputAction Select;
        public readonly InputAction PointerPosition;
        public readonly InputAction CycleTarget;
        public readonly InputAction Cancel;
        public readonly InputAction ChatSubmit;
        public readonly InputAction ModifierShift;
        public readonly InputAction ModifierCtrl;
        public readonly InputAction[] SkillSlots;
        public readonly Dictionary<string, InputAction> WindowHotkeys = new Dictionary<string, InputAction>();

        public GameInput(IEnumerable<HudWindowDto> windows)
        {
            Move = _map.AddAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            Look = _map.AddAction("Look", InputActionType.Value, "<Mouse>/delta");
            LookHold = _map.AddAction("LookHold", InputActionType.Button, "<Mouse>/rightButton");
            Zoom = _map.AddAction("Zoom", InputActionType.Value, "<Mouse>/scroll/y");
            Select = _map.AddAction("Select", InputActionType.Button, "<Mouse>/leftButton");
            PointerPosition = _map.AddAction("PointerPosition", InputActionType.Value, "<Mouse>/position");
            CycleTarget = _map.AddAction("CycleTarget", InputActionType.Button, "<Keyboard>/tab");
            Cancel = _map.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
            ChatSubmit = _map.AddAction("ChatSubmit", InputActionType.Button, "<Keyboard>/enter");
            ChatSubmit.AddBinding("<Keyboard>/numpadEnter");
            ModifierShift = _map.AddAction("ModifierShift", InputActionType.Button, "<Keyboard>/shift");
            ModifierCtrl = _map.AddAction("ModifierCtrl", InputActionType.Button, "<Keyboard>/ctrl");

            SkillSlots = new InputAction[SkillSlotKeys.Length];
            for (int i = 0; i < SkillSlotKeys.Length; i++)
            {
                SkillSlots[i] = _map.AddAction($"SkillSlot{i + 1}", InputActionType.Button, SkillSlotKeys[i]);
            }
            foreach (HudWindowDto window in windows)
            {
                if (!string.IsNullOrEmpty(window.HotkeyBinding))
                {
                    WindowHotkeys[window.Id] = _map.AddAction($"Window_{window.Id}", InputActionType.Button, window.HotkeyBinding);
                }
            }
        }

        public int SkillSlotCount => SkillSlots.Length;

        public static string DisplayKey(int slotIndex) =>
            slotIndex < SkillSlotKeys.Length ? InputControlPath.ToHumanReadableString(SkillSlotKeys[slotIndex], InputControlPath.HumanReadableStringOptions.OmitDevice) : string.Empty;

        /// <summary>The key of a window as shown on menus: the account's key if rebound (module 17), else the registry's.</summary>
        public static string WindowKey(ClientContext ctx, HudWindowDto window) =>
            ctx.Settings != null ? ctx.Settings.Keys.WindowKey(window.Id) : DisplayBinding(window.HotkeyBinding);

        /// <summary>The key of a skill slot of the first row as shown on the panel (rebound or built-in).</summary>
        public static string SkillKey(ClientContext ctx, int slot) => ctx.Settings != null ? ctx.Settings.Keys.SkillKey(slot) : DisplayKey(slot);

        public static string DisplayBinding(string binding) =>
            string.IsNullOrEmpty(binding) ? string.Empty : InputControlPath.ToHumanReadableString(binding, InputControlPath.HumanReadableStringOptions.OmitDevice);

        public Vector2 PointerScreenPosition => PointerPosition.ReadValue<Vector2>();

        public void Enable() => _map.Enable();

        public void Disable() => _map.Disable();

        public void Dispose()
        {
            _map.Disable();
            _map.Dispose();
        }
    }
}
