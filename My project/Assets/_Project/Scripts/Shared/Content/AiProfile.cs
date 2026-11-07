using System.Collections.Generic;

namespace Game.Shared.Content
{
    /// <summary>How a mob reacts to players (module 10).</summary>
    public enum AiBehavior
    {
        /// <summary>Never attacks (training targets).</summary>
        Passive,
        /// <summary>Attacks players who come within AggroRadius.</summary>
        Aggressive,
        /// <summary>Attacks only those who hurt it (or healed its attackers).</summary>
        Defensive,
    }

    /// <summary>Built AI profile: behaviour, speed, aggro and leash radii, abilities by priority.</summary>
    public sealed class AiProfile
    {
        public string Id;
        public AiBehavior Behavior;
        public float MoveSpeed;
        public float AggroRadius;
        public float LeashRadius;
        public readonly List<string> Abilities = new List<string>();
    }
}
