using Game.Client.Localization;
using Game.Shared.Core;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.UI;
using EntityId = Game.Shared.Core.EntityId;

namespace Game.Client.UI.Widgets
{
    /// <summary>
    /// Group frames under the player frame (02, 7.13 top-left zone): name with the leader mark and health of every other
    /// member. A click selects the member as the target (for healing). In a raid only the own subgroup is shown.
    /// </summary>
    public sealed class GroupFramesWidget
    {
        private static readonly Vector2 FrameSize = new Vector2(240f, 46f);

        private readonly ClientContext _ctx;
        private readonly Image[] _frames;
        private readonly Text[] _names;
        private readonly UiBar[] _health;
        private readonly EntityId[] _ids;

        public GroupFramesWidget(RectTransform zone, ClientContext ctx)
        {
            _ctx = ctx;
            int count = System.Math.Max(ctx.Content.Config.GroupMaxSize, ctx.Content.Config.RaidSubgroupSize) - 1;
            _frames = new Image[count];
            _names = new Text[count];
            _health = new UiBar[count];
            _ids = new EntityId[count];
            float top = -HudLayout.PlayerFrameSize.y - HudLayout.CastBarHeight - HudLayout.Gap * 3f;
            float width = FrameSize.x - HudLayout.Margin * 2f;
            for (int i = 0; i < count; i++)
            {
                int index = i;
                Image frame = UiFactory.Panel($"GroupFrame_{i}", zone, HudPalette.PanelBackground);
                UiFactory.Place(frame.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, top - i * (FrameSize.y + HudLayout.Gap)), FrameSize);
                Button button = frame.gameObject.AddComponent<Button>();
                button.targetGraphic = frame;
                button.onClick.AddListener(() => _ctx.Commands?.SelectTarget(_ids[index]));
                _names[i] = UiFactory.Label("Name", frame.transform, ctx.Font, HudLayout.FontSmall, TextAnchor.MiddleLeft, HudPalette.Text);
                UiFactory.Place(_names[i].rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(HudLayout.Margin, -HudLayout.Gap),
                    new Vector2(width, HudLayout.FontSmall + HudLayout.Gap));
                _health[i] = UiFactory.Bar("Health", frame.transform, ctx.Font, HudPalette.Health);
                UiFactory.Place(_health[i].Rect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(HudLayout.Margin, HudLayout.Gap), new Vector2(width, HudLayout.BarHeight));
                frame.gameObject.SetActive(false);
                _frames[i] = frame;
            }
        }

        public void Refresh(PlayerStateSnapshot state)
        {
            GroupSnapshot group = state?.Group;
            int shown = 0;
            if (group != null && group.InGroup)
            {
                int own = SubgroupOf(group, state.EntityId);
                for (int i = 0; i < group.Members.Length; i++)
                {
                    EntityId member = group.Members[i];
                    if (member == state.EntityId || shown >= _frames.Length || (group.IsRaid && SubgroupAt(group, i) != own))
                    {
                        continue;
                    }
                    Show(shown++, member, member == group.LeaderId);
                }
            }
            for (int i = shown; i < _frames.Length; i++)
            {
                _frames[i].gameObject.SetActive(false);
            }
        }

        private static int SubgroupAt(GroupSnapshot group, int index) => index >= 0 && index < group.Subgroups.Length ? group.Subgroups[index] : 0;

        private static int SubgroupOf(GroupSnapshot group, EntityId member) => SubgroupAt(group, System.Array.IndexOf(group.Members, member));

        private void Show(int slot, EntityId member, bool leader)
        {
            _ids[slot] = member;
            _frames[slot].gameObject.SetActive(true);
            string name = EntityNames.Of(_ctx, member);
            _names[slot].text = leader ? _ctx.Text.Format(UiKeys.GroupLeaderName, name) : name;
            if (_ctx.State.TryGetEntity(member, out EntitySnapshot entity) && entity.MaxHealth > 0f)
            {
                _health[slot].Set(entity.Health / entity.MaxHealth,
                    _ctx.Text.Format(UiKeys.ValueOfMax, Mathf.CeilToInt(entity.Health), Mathf.RoundToInt(entity.MaxHealth)));
            }
            else
            {
                _health[slot].Set(0f, string.Empty);
            }
        }
    }
}
