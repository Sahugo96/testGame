using Game.Shared.Core;

namespace Game.Shared.Protocol
{
    /// <summary>Private state sent only to the owning player.</summary>
    public sealed class PlayerStateSnapshot
    {
        public EntityId EntityId;
        public string ClassId;
        public int Level;
        public int Experience;
        public int ExperienceToNext;
        public int AttributePoints;
        public int ClassPoints;
        public float Health;
        public float MaxHealth;
        public float MainMana;
        public float MaxMainMana;
        public float ReserveMana;
        public float MaxReserveMana;
        public bool MainManaLocked;
        public bool InCombat;
        public bool IsDead;
        public EntityId TargetId;
        public string CastSkillId;
        public float CastProgress;
        public float GlobalCooldownRemaining;
        public float GlobalCooldownDuration;
        public float MainHandSwapLockRemaining;
        public float MainHandSwapRiskRemaining;
        public float OffHandSwapLockRemaining;
        public SkillCooldownSnapshot[] Cooldowns;
        public SkillStateSnapshot[] Skills;
        public ItemSlotSnapshot[] Bag;
        public EquipmentSlotSnapshot[] Equipment;
        public StatSnapshot[] Stats;
        public CooldownGroupSnapshot[] ConsumableCooldowns;
        public bool AutoLoot;
        public string CraftRecipeId;
        public float CraftProgress;
        public DuelSnapshot Duel;
        public GroupSnapshot Group = new GroupSnapshot();
    }
}
