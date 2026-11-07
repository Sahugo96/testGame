using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Game.Client.Localization;
using Game.Client.UI.Widgets;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using Game.Shared.Content.Items;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.UI.Windows
{
    /// <summary>
    /// Anvil — crafting by recipes (05, 7.4; developer decision v15). Left: recipes of this station; right: result, inputs
    /// (have / need), success chance, time, "Create" and a progress bar with "Cancel". The server checks and rolls; the
    /// window closes when the player walks away from the station or enters combat (02, 7.13).
    /// </summary>
    public sealed class AnvilWindow : HudWindow
    {
        private static readonly Vector2 Size = new Vector2(760f, 470f);
        private const float ListWidth = 330f;
        private const float PercentFactor = 100f;

        private readonly List<RecipeDto> _recipes = new List<RecipeDto>();
        private readonly List<Text> _rows = new List<Text>();
        private readonly Text _details;
        private readonly Button _craft;
        private readonly Button _cancel;
        private readonly UiBar _progress;
        private readonly StringBuilder _sb = new StringBuilder();
        private string _selected;

        public AnvilWindow(HudWindowDto definition, RectTransform parent, Canvas canvas, ClientContext ctx, Tooltip tooltip,
            Action<HudWindow> focus, Action<HudWindow> close) : base(definition, parent, canvas, ctx, tooltip, focus, close, Size)
        {
            RectTransform left = UiFactory.Rect("Recipes", Content);
            left.anchorMin = Vector2.zero;
            left.anchorMax = new Vector2(0f, 1f);
            left.pivot = new Vector2(0f, 0.5f);
            left.offsetMin = Vector2.zero;
            left.offsetMax = new Vector2(ListWidth, 0f);
            RectTransform list = VerticalList("List", left);
            foreach (RecipeDto recipe in ctx.Content.RecipeList)
            {
                if (recipe.Station != StationTypes.Anvil)
                {
                    continue;
                }
                int index = _recipes.Count;
                _recipes.Add(recipe);
                Text row = Row("Recipe", list);
                row.raycastTarget = true;
                row.gameObject.AddComponent<HoverTrigger>().Clicked = button =>
                {
                    if (button == PointerEventData.InputButton.Left)
                    {
                        _selected = _recipes[index].Id;
                        Refresh(Ctx.State.Player);
                    }
                };
                _rows.Add(row);
            }
            _selected = _recipes.Count > 0 ? _recipes[0].Id : null;

            RectTransform right = UiFactory.Rect("Details", Content);
            right.anchorMin = Vector2.zero;
            right.anchorMax = Vector2.one;
            right.offsetMin = new Vector2(ListWidth + HudLayout.Margin, 0f);
            right.offsetMax = Vector2.zero;
            _details = UiFactory.Label("Text", right, ctx.Font, HudLayout.FontNormal, TextAnchor.UpperLeft, HudPalette.Text);
            _details.supportRichText = true;
            UiFactory.Stretch(_details.rectTransform);
            _details.rectTransform.offsetMin = new Vector2(0f, (HudLayout.MenuButtonHeight + HudLayout.Gap) * 2f);

            _progress = UiFactory.Bar("Progress", right, ctx.Font, HudPalette.Cast);
            UiFactory.Place(_progress.Rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, HudLayout.MenuButtonHeight + HudLayout.Gap * 2f),
                new Vector2(HudLayout.MenuButtonWidth * 3f, HudLayout.BarHeight));
            _craft = UiFactory.Button("Craft", right, ctx.Font, ctx.Text.Get(UiKeys.CraftButton), Craft);
            UiFactory.Place((RectTransform)_craft.transform, new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-HudLayout.Gap, 0f),
                new Vector2(HudLayout.MenuButtonWidth * 1.5f, HudLayout.MenuButtonHeight));
            _cancel = UiFactory.Button("Cancel", right, ctx.Font, ctx.Text.Get(UiKeys.DuelCancel), () => Ctx.Commands?.CancelCraft());
            UiFactory.Place((RectTransform)_cancel.transform, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(HudLayout.Gap, 0f),
                new Vector2(HudLayout.MenuButtonWidth * 1.5f, HudLayout.MenuButtonHeight));
        }

        public override void Refresh(PlayerStateSnapshot state)
        {
            if (!IsNearActiveStation(state) || state.InCombat)
            {
                RequestClose();
                return;
            }
            for (int i = 0; i < _rows.Count; i++)
            {
                RecipeDto recipe = _recipes[i];
                ItemDefinition output = Ctx.Content.Items[recipe.Outputs[0].ItemId];
                bool selected = recipe.Id == _selected;
                _rows[i].text = (selected ? "▶ " : string.Empty) + Ctx.Text.Get(recipe.NameKey);
                _rows[i].color = HudPalette.ForRarity(output.Rarity);
            }
            bool crafting = !string.IsNullOrEmpty(state.CraftRecipeId);
            _progress.SetVisible(crafting);
            _cancel.gameObject.SetActive(crafting);
            if (crafting && Ctx.Content.Recipes.TryGetValue(state.CraftRecipeId, out RecipeDto current))
            {
                _progress.Set(state.CraftProgress, Ctx.Text.Get(current.NameKey));
            }
            ShowDetails(state, crafting);
        }

        private void ShowDetails(PlayerStateSnapshot state, bool crafting)
        {
            if (_selected == null || !Ctx.Content.Recipes.TryGetValue(_selected, out RecipeDto recipe))
            {
                _details.text = Ctx.Text.Get(UiKeys.CraftNoRecipes);
                _craft.interactable = false;
                return;
            }
            ILocalizationService text = Ctx.Text;
            _sb.Clear();
            ItemCountDto main = recipe.Outputs[0];
            _sb.Append("<b>").Append(TooltipText.ItemName(Ctx, Ctx.Content.Items[main.ItemId], 0)).Append("</b>");
            if (main.Count > 1)
            {
                _sb.Append(" ×").Append(main.Count);
            }
            _sb.Append("\n\n").Append(text.Get(UiKeys.CraftInputs)).Append('\n');
            bool enough = true;
            foreach (ItemCountDto input in recipe.Inputs)
            {
                int have = CountInBag(state, input.ItemId);
                enough &= have >= input.Count;
                _sb.Append(text.Format(UiKeys.CraftInputLine, text.Get(Ctx.Content.Items[input.ItemId].NameKey), have, input.Count)).Append('\n');
            }
            _sb.Append('\n')
                .Append(text.Format(UiKeys.CraftChance, (recipe.SuccessChance * PercentFactor).ToString("0", CultureInfo.InvariantCulture))).Append('\n')
                .Append(text.Format(UiKeys.CraftTime, recipe.CraftTime.ToString("0.#", CultureInfo.InvariantCulture))).Append('\n')
                .Append(text.Get(UiKeys.CraftFailLoss));
            _details.text = _sb.ToString();
            _craft.interactable = enough && !crafting;
        }

        private void Craft()
        {
            if (_selected != null)
            {
                Ctx.Commands?.Craft(_selected, Ctx.ActiveStation);
            }
        }

        private static int CountInBag(PlayerStateSnapshot state, string definitionId)
        {
            int total = 0;
            foreach (ItemSlotSnapshot slot in state.Bag)
            {
                if (slot.DefinitionId == definitionId)
                {
                    total += slot.Count;
                }
            }
            return total;
        }
    }
}
