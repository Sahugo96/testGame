namespace Game.Shared.Content
{
    /// <summary>Skill cost resource (03, 7.1; CostEntry is FROZEN). CombatMana = pool A, the main mana (question 60); ReserveMana = pool B; Mana = pool A first, then B.</summary>
    public enum ResourceKind
    {
        Mana,
        CombatMana,
        ReserveMana,
        Health,
    }
}
