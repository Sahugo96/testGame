using System.Collections.Generic;

namespace Game.Shared.Content.Items
{
    /// <summary>Item definition (04, 7.9). Behaviour comes from components; instances are created only by the server.</summary>
    public sealed class ItemDefinition
    {
        public string Id;
        public string NameKey;
        public string DescriptionKey;
        public string Icon;
        public ItemCategory Category;
        public Rarity Rarity;
        public int ItemLevel;
        public int RequiredLevel;
        public readonly HashSet<string> ClassRestriction = new HashSet<string>();
        public BindingType Binding;
        public readonly List<ItemComponent> Components = new List<ItemComponent>();

        public T Get<T>() where T : ItemComponent
        {
            foreach (ItemComponent component in Components)
            {
                if (component is T typed)
                {
                    return typed;
                }
            }
            return null;
        }

        public bool Has<T>() where T : ItemComponent => Get<T>() != null;

        public bool HasType(string type)
        {
            foreach (ItemComponent component in Components)
            {
                if (component.Type == type)
                {
                    return true;
                }
            }
            return false;
        }

        public int MaxStack => Get<StackableComponent>()?.MaxStack ?? 1;

        public bool IsUsableBy(string classId) => ClassRestriction.Count == 0 || ClassRestriction.Contains(classId);
    }
}
