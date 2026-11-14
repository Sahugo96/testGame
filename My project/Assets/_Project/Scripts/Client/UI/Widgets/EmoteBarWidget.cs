using Game.Client.Localization;
using Game.Shared.Content.Dto;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Client.UI.Widgets
{
    /// <summary>
    /// Quick emote buttons to the right of the skill panel (02, 7.7 Г): one button per emote of the content's white
    /// list, at most <see cref="MaxButtons"/>. A click asks the server to show the emote; the server answers everyone
    /// who sees the player. Temporary (DEFAULT, question 89): buttons only, no keys and no radial menu yet.
    /// </summary>
    public sealed class EmoteBarWidget
    {
        private const int MaxButtons = 8;

        public EmoteBarWidget(RectTransform zone, ClientContext ctx, Tooltip tooltip, float leftEdgeX)
        {
            EmoteDto[] emotes = ctx.Content.Config.Emotes ?? new EmoteDto[0];
            float step = HudLayout.SkillSlotSize + HudLayout.Gap;
            for (int i = 0; i < emotes.Length && i < MaxButtons; i++)
            {
                EmoteDto emote = emotes[i];
                var slot = new SlotView($"EmoteButton_{emote.Id}", zone, ctx.Font, HudLayout.SkillSlotSize);
                UiFactory.Place(slot.Rect, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(leftEdgeX + i * step, 0f),
                    new Vector2(HudLayout.SkillSlotSize, HudLayout.SkillSlotSize));
                slot.SetFilled(true);
                slot.SetCaption(ctx.Text.Get(emote.NameKey), HudPalette.Text);
                slot.SetIcon(ctx.Icons.Emote(emote.Icon));
                slot.Bind(
                    () => tooltip.Show(ctx.Text.Get(emote.NameKey) + "\n" + ctx.Text.Get(UiKeys.EmoteHint)),
                    tooltip.Hide,
                    button =>
                    {
                        if (button == PointerEventData.InputButton.Left)
                        {
                            ctx.Commands?.PlayEmote(emote.Id);
                        }
                    });
            }
        }
    }
}
