using System;
using Game.Shared.Content;
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
        public const string Map = "map";
        public const string Anvil = "anvil";
        public const string Whetstone = "whetstone";
        public const string Group = "group";
        public const string CombatLog = "combatlog";
        public const string Friends = "friends";
        public const string Trade = "trade";
        public const string ChatTabs = "chattabs";
        public const string Market = "market";
        public const string Shop = "shop";
        public const string Mail = "mail";
        public const string UpgradeOrders = "orders";

        /// <summary>Window opened by clicking a station of the given type (StationTypes), or null.</summary>
        public static string ForStation(string stationType)
        {
            switch (stationType)
            {
                case StationTypes.Anvil:
                    return Anvil;
                case StationTypes.Whetstone:
                    return Whetstone;
                case StationTypes.Market:
                    return Market;
                case StationTypes.Mailbox:
                    return Mail;
                case StationTypes.ShopSpot:
                case StationTypes.Merchant:
                    return Shop;
                default:
                    return null;
            }
        }

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
                case Anvil:
                    return new AnvilWindow(definition, layer, canvas, ctx, tooltip, focus, close);
                case Whetstone:
                    return new WhetstoneWindow(definition, layer, canvas, ctx, tooltip, focus, close);
                case Map:
                    return new MapWindow(definition, layer, canvas, ctx, tooltip, focus, close);
                case CombatLog:
                    return new CombatLogWindow(definition, layer, canvas, ctx, tooltip, focus, close);
                case Group:
                    return new GroupWindow(definition, layer, canvas, ctx, tooltip, focus, close);
                case Friends:
                    return new FriendsWindow(definition, layer, canvas, ctx, tooltip, focus, close);
                case Market:
                    return new MarketWindow(definition, layer, canvas, ctx, tooltip, focus, close);
                case Shop:
                    return new ShopWindow(definition, layer, canvas, ctx, tooltip, focus, close);
                case Mail:
                    return new MailWindow(definition, layer, canvas, ctx, tooltip, focus, close);
                case UpgradeOrders:
                    return new UpgradeOrdersWindow(definition, layer, canvas, ctx, tooltip, focus, close);
                case ChatTabs:
                    return new ChatTabsWindow(definition, layer, canvas, ctx, tooltip, focus, close);
                case Trade:
                    return new TradeWindow(definition, layer, canvas, ctx, tooltip, focus, close);
                case Settings:
                    return new SettingsWindow(definition, layer, canvas, ctx, tooltip, focus, close);
                default:
                    return new PlaceholderWindow(definition, layer, canvas, ctx, tooltip, focus, close);
            }
        }
    }
}
