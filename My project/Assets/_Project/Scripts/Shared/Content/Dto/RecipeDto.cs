using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>
    /// Recipe { Id, Profession, RequiredLevel, Inputs[], Outputs[], Station?, CraftTime, XpReward } (05, 7.4) plus the
    /// success chance (failure destroys all inputs, question 19). Profession, RequiredLevel and XpReward are kept for
    /// module 12: until professions exist anyone may craft (question 73).
    /// </summary>
    [Serializable]
    public class RecipeDto
    {
        public string Id;
        public string NameKey;
        public string Profession;
        public int RequiredLevel;
        /// <summary>Station type (StationTypes) where the recipe is crafted.</summary>
        public string Station;
        public float CraftTime;
        public float SuccessChance;
        public int XpReward;
        public ItemCountDto[] Inputs;
        public ItemCountDto[] Outputs;
    }

    /// <summary>An item id with a count (recipe inputs and outputs).</summary>
    [Serializable]
    public class ItemCountDto
    {
        public string ItemId;
        public int Count;
    }
}
