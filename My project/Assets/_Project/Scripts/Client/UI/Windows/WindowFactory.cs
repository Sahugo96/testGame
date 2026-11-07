using System;
using Game.Shared.Content.Dto;
using UnityEngine;

namespace Game.Client.UI.Windows
{
    /// <summary>Maps window registry ids to window classes. Unknown ids get a placeholder window.</summary>
    public static class WindowFactory
    {
        public const string Character = "character";
        public const string Backpack = "backpack";
        public const string SkillBook = "skillbook";
        public const string Settings = "settings";

        public static HudWindow Create(HudWindowDto definition, RectTransform layer, Canvas canvas, ClientContext ctx, Tooltip tooltip,
            Action<HudWindow> focus, Action<HudWindow> close)
        {
            switch (definition.Id)
            {
                case Character:
                    return new CharacterWindow(definition, layer, canvas, ctx, tooltip, focus, close);
                case Backpack:
                    return new BackpackWindow(definition, layer, canvas, ctx, tooltip, focus, close);
                case SkillBook:
                    return new SkillBookWindow(definition, layer, canvas, ctx, tooltip, focus, close);
                case Settings:
                    return new SettingsWindow(definition, layer, canvas, ctx, tooltip, focus, close);
                default:
                    return new PlaceholderWindow(definition, layer, canvas, ctx, tooltip, focus, close);
            }
        }
    }
}
