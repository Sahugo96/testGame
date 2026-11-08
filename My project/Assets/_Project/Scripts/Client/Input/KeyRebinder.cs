using System;
using System.Collections.Generic;
using Game.Shared.Protocol;
using UnityEngine.InputSystem;

namespace Game.Client.Input
{
    /// <summary>
    /// Key rebinding (02, 7.6) on the Input Actions of <see cref="GameInput"/> through the Input System rebinding API.
    /// Rebindable: movement keys, target, chat, the Shift/Ctrl skill modifiers, skill slots and window hotkeys; mouse
    /// controls and Esc are reserved (Esc cancels a rebinding). A key already used by another action is a conflict the
    /// player resolves: replace (the other action loses the key), swap, or cancel. Only changed keys are stored on the
    /// account (KeyBindingSnapshot: "Action/index" → path; empty path = unbound).
    /// </summary>
    public sealed class KeyRebinder
    {
        private const string ReservedKey = "<Keyboard>/escape";
        private const string MouseDevice = "<Mouse>";
        private static readonly string[] MoveParts = { "move_up", "move_down", "move_left", "move_right" };

        private readonly GameInput _input;
        private readonly List<RebindSlot> _slots = new List<RebindSlot>();
        private readonly Dictionary<string, RebindSlot> _windowSlots = new Dictionary<string, RebindSlot>();
        private InputActionRebindingExtensions.RebindingOperation _operation;

        public KeyRebinder(GameInput input)
        {
            _input = input;
            for (int part = 0; part < MoveParts.Length; part++)
            {
                // Binding 0 is the first WASD composite, 1..4 its parts (the arrow keys stay as they are).
                Add(input.Move, part + 1, KeyLabels.Prefix + MoveParts[part], null);
            }
            Add(input.CycleTarget, 0, KeyLabels.Prefix + "cycle_target", null);
            Add(input.ChatSubmit, 0, KeyLabels.Prefix + "chat", null);
            Add(input.ModifierShift, 0, KeyLabels.Prefix + "modifier_empowered", null);
            Add(input.ModifierCtrl, 0, KeyLabels.Prefix + "modifier_weakened", null);
            for (int i = 0; i < input.SkillSlots.Length; i++)
            {
                Add(input.SkillSlots[i], 0, KeyLabels.SkillSlot, (i + 1).ToString());
            }
            foreach (KeyValuePair<string, InputAction> window in input.WindowHotkeys)
            {
                _windowSlots[window.Key] = Add(window.Value, 0, KeyLabels.Window, window.Key);
            }
        }

        public IReadOnlyList<RebindSlot> Slots => _slots;

        /// <summary>Any key changed (panels refresh the key shown on slots and buttons).</summary>
        public event Action KeysChanged;

        public bool IsRebinding => _operation != null;

        /// <summary>Human-readable current key of a slot; empty when unbound.</summary>
        public static string Display(RebindSlot slot)
        {
            string path = slot.Path;
            return string.IsNullOrEmpty(path) ? string.Empty : InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        public string SkillKey(int slot) => slot < _input.SkillSlots.Length ? Display(_slots.Find(s => s.Action == _input.SkillSlots[slot])) : string.Empty;

        public string WindowKey(string windowId) => _windowSlots.TryGetValue(windowId, out RebindSlot slot) ? Display(slot) : string.Empty;

        /// <summary>Waits for a key (mouse excluded, Esc cancels) and reports the result; a clash is returned for the player to resolve.</summary>
        public void Rebind(RebindSlot slot, Action<RebindResult> done)
        {
            if (_operation != null)
            {
                return;
            }
            string old = slot.Path;
            _input.Disable();
            _operation = slot.Action.PerformInteractiveRebinding(slot.BindingIndex)
                .WithControlsExcluding(MouseDevice)
                .WithCancelingThrough(ReservedKey)
                .OnComplete(operation => Finish(slot, old, false, done))
                .OnCancel(operation => Finish(slot, old, true, done))
                .Start();
        }

        public void CancelRebind()
        {
            _operation?.Cancel();
        }

        /// <summary>Conflict: the other action loses the key.</summary>
        public void Replace(RebindResult conflict)
        {
            Set(conflict.Other, string.Empty);
            KeysChanged?.Invoke();
        }

        /// <summary>Conflict: the other action takes this slot's previous key.</summary>
        public void Swap(RebindResult conflict)
        {
            Set(conflict.Other, conflict.PreviousPath);
            KeysChanged?.Invoke();
        }

        /// <summary>Conflict: this slot gets its previous key back.</summary>
        public void Revert(RebindResult conflict)
        {
            Set(conflict.Slot, conflict.PreviousPath);
            KeysChanged?.Invoke();
        }

        /// <summary>The account's changed keys on top of the built-in ones (everything else back to built-in).</summary>
        public void Apply(KeyBindingSnapshot[] bindings)
        {
            CancelRebind();
            var byId = new Dictionary<string, string>();
            foreach (KeyBindingSnapshot binding in bindings ?? new KeyBindingSnapshot[0])
            {
                if (!string.IsNullOrEmpty(binding.BindingId))
                {
                    byId[binding.BindingId] = binding.Path ?? string.Empty;
                }
            }
            foreach (RebindSlot slot in _slots)
            {
                slot.Action.RemoveBindingOverride(slot.BindingIndex);
                if (byId.TryGetValue(slot.Id, out string path))
                {
                    slot.Action.ApplyBindingOverride(slot.BindingIndex, path);
                }
            }
            KeysChanged?.Invoke();
        }

        /// <summary>Keys that differ from the built-in ones, for the account settings.</summary>
        public KeyBindingSnapshot[] Export()
        {
            var result = new List<KeyBindingSnapshot>();
            foreach (RebindSlot slot in _slots)
            {
                InputBinding binding = slot.Action.bindings[slot.BindingIndex];
                if (binding.overridePath != null && binding.overridePath != binding.path)
                {
                    result.Add(new KeyBindingSnapshot { BindingId = slot.Id, Path = binding.overridePath });
                }
            }
            return result.ToArray();
        }

        private void Finish(RebindSlot slot, string old, bool cancelled, Action<RebindResult> done)
        {
            _operation?.Dispose();
            _operation = null;
            _input.Enable();
            var result = new RebindResult { Slot = slot, PreviousPath = old, Cancelled = cancelled };
            if (!cancelled)
            {
                string path = slot.Path;
                result.Other = _slots.Find(other => other != slot && !string.IsNullOrEmpty(path) && string.Equals(other.Path, path, StringComparison.OrdinalIgnoreCase));
            }
            KeysChanged?.Invoke();
            done?.Invoke(result);
        }

        private static void Set(RebindSlot slot, string path) => slot.Action.ApplyBindingOverride(slot.BindingIndex, path ?? string.Empty);

        private RebindSlot Add(InputAction action, int bindingIndex, string labelKey, string labelArgument)
        {
            var slot = new RebindSlot(action, bindingIndex, labelKey, labelArgument);
            _slots.Add(slot);
            return slot;
        }
    }
}
