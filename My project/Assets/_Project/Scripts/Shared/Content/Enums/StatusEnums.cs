using System;

namespace Game.Shared.Content
{
    /// <summary>Buff or debuff (03, 7.12): dispel and UI colour depend on it.</summary>
    public enum StatusKind
    {
        Buff,
        Debuff,
    }

    /// <summary>Status category (03, 7.12): Stat, DoT, HoT, Control, Shield, Special.</summary>
    public enum StatusCategory
    {
        Stat,
        DoT,
        HoT,
        Control,
        Shield,
        Special,
    }

    /// <summary>What happens when the same status is applied again (03, 7.12).</summary>
    public enum StatusStacking
    {
        /// <summary>Restart the duration; strength is replaced by the new one.</summary>
        Refresh,
        /// <summary>One more stack (up to MaxStacks) and the duration restarts.</summary>
        AddStacks,
        /// <summary>Replace only if the new one is at least as strong.</summary>
        ReplaceStronger,
    }

    /// <summary>Control effects (status category Control): stun stops moving and skills, root stops moving, silence stops skills.</summary>
    [Flags]
    public enum ControlType
    {
        None = 0,
        Stun = 1,
        Root = 2,
        Silence = 4,
    }
}
