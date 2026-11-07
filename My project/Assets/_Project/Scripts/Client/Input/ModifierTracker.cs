using System.Collections.Generic;
using Game.Shared.Content;

namespace Game.Client.Input
{
    /// <summary>
    /// Skill modifier with "last pressed wins" (question 4): holding Shift and then Ctrl gives Ctrl; releasing Ctrl while
    /// Shift is still held gives Shift again.
    /// </summary>
    public sealed class ModifierTracker
    {
        private readonly List<SkillModifier> _held = new List<SkillModifier>();

        public SkillModifier Current => _held.Count > 0 ? _held[_held.Count - 1] : SkillModifier.None;

        public void Press(SkillModifier modifier)
        {
            _held.Remove(modifier);
            _held.Add(modifier);
        }

        public void Release(SkillModifier modifier) => _held.Remove(modifier);

        /// <summary>Forget held keys (input stops being read while there is no game, so releases can be missed).</summary>
        public void Clear() => _held.Clear();
    }
}
