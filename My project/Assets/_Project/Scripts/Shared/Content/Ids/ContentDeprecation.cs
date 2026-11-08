using System.Collections.Generic;
using Game.Shared.Content.Dto;
using Game.Shared.Core;

namespace Game.Shared.Content.Ids
{
    /// <summary>
    /// Retired records (question 83) before the content is built: a Deprecated item no longer drops, is not crafted and not
    /// given to new characters; a Deprecated mob no longer spawns; a Deprecated recipe (or one that makes or needs a
    /// Deprecated item) is gone from the anvil. The records themselves stay, so old instances load and show.
    /// </summary>
    public static class ContentDeprecation
    {
        public static void Apply(ContentFiles files)
        {
            var items = new HashSet<string>();
            foreach (ItemDto item in files.Items?.Items ?? new ItemDto[0])
            {
                if (item.Deprecated)
                {
                    items.Add(item.Id);
                }
            }
            var mobs = new HashSet<string>();
            foreach (MobDto mob in files.Mobs?.Mobs ?? new MobDto[0])
            {
                if (mob.Deprecated)
                {
                    mobs.Add(mob.Id);
                }
                if (mob.Drops != null)
                {
                    mob.Drops = System.Array.FindAll(mob.Drops, d => !items.Contains(d.ItemId ?? string.Empty));
                }
            }
            if (files.World?.Spawns != null)
            {
                files.World.Spawns = System.Array.FindAll(files.World.Spawns, s => !mobs.Contains(s.MobId ?? string.Empty));
            }
            if (files.Recipes?.Recipes != null)
            {
                files.Recipes.Recipes = System.Array.FindAll(files.Recipes.Recipes, r => !Retired(r, items));
            }
            foreach (ClassDto cls in files.Classes?.Classes ?? new ClassDto[0])
            {
                if (cls.StartingItems != null)
                {
                    cls.StartingItems = System.Array.FindAll(cls.StartingItems, s => !items.Contains(s.ItemId ?? string.Empty));
                }
            }
            if (items.Count + mobs.Count > 0)
            {
                Log.Info(LogTags.Content, $"Deprecated: {items.Count} items, {mobs.Count} mobs (kept for old saves, no longer dropped or spawned)");
            }
        }

        private static bool Retired(RecipeDto recipe, HashSet<string> items)
        {
            if (recipe.Deprecated)
            {
                return true;
            }
            foreach (ItemCountDto part in recipe.Inputs ?? new ItemCountDto[0])
            {
                if (items.Contains(part.ItemId ?? string.Empty))
                {
                    return true;
                }
            }
            foreach (ItemCountDto part in recipe.Outputs ?? new ItemCountDto[0])
            {
                if (items.Contains(part.ItemId ?? string.Empty))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
