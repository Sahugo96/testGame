using System.Collections.Generic;
using Game.Shared.Content.Dto;

namespace Game.Shared.Content.Items
{
    /// <summary>Creates a component from its serialized form; reports problems into errors.</summary>
    public delegate ItemComponent ItemComponentFactory(ItemComponentDto dto, string context, List<string> errors);
}
