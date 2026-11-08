using System;

namespace Game.Shared.Content.Dto
{
    /// <summary>File wrapper: Resources/Content/<pack>/items.json (merged across packs).</summary>
    [Serializable]
    public class ItemsFileDto
    {
        public ItemDto[] Items;
    }
}
