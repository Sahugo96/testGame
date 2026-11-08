using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>File wrapper: Resources/Content/<pack>/recipes.json (merged across packs).</summary>
    [Serializable]
    public class RecipesFileDto
    {
        public RecipeDto[] Recipes;
    }
}
