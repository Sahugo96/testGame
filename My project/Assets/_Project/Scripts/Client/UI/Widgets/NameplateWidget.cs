using System.Collections.Generic;
using Game.Client.World;
using Game.Shared.Core;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Widgets
{
    /// <summary>Names and health bars above entities in the world (02, 7.13 «Мир»).</summary>
    public sealed class NameplateWidget
    {
        private sealed class Plate
        {
            public RectTransform Root;
            public Text Name;
            public UiBar Health;
        }

        private readonly ClientContext _ctx;
        private readonly EntityViewManager _views;
        private readonly RectTransform _root;
        private readonly Dictionary<EntityId, Plate> _plates = new Dictionary<EntityId, Plate>();
        private readonly List<EntityId> _stale = new List<EntityId>();

        public NameplateWidget(RectTransform root, ClientContext ctx, EntityViewManager views)
        {
            _root = root;
            _ctx = ctx;
            _views = views;
        }

        public void Tick()
        {
            _stale.Clear();
            _stale.AddRange(_plates.Keys);
            foreach (EntitySnapshot entity in _ctx.State.Entities)
            {
                if (entity.Id == _ctx.State.LocalPlayerId || entity.IsDead || !_views.TryGetView(entity.Id, out EntityView view))
                {
                    continue;
                }
                _stale.Remove(entity.Id);
                if (!_plates.TryGetValue(entity.Id, out Plate plate))
                {
                    plate = Create();
                    _plates[entity.Id] = plate;
                }
                Vector3 screen = _ctx.Camera.WorldToScreenPoint(view.transform.position + Vector3.up * HudLayout.NameplateHeight);
                bool visible = screen.z > 0f;
                plate.Root.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out Vector2 local);
                plate.Root.anchoredPosition = local;
                plate.Name.text = string.IsNullOrEmpty(entity.DisplayName) ? _ctx.Text.Get(entity.NameKey) : entity.DisplayName;
                plate.Health.SetVisible(entity.MaxHealth > 0f);
                plate.Health.Set(entity.MaxHealth > 0f ? entity.Health / entity.MaxHealth : 0f, string.Empty);
            }
            foreach (EntityId id in _stale)
            {
                Object.Destroy(_plates[id].Root.gameObject);
                _plates.Remove(id);
            }
        }

        private Plate Create()
        {
            RectTransform root = UiFactory.Rect("Nameplate", _root);
            root.sizeDelta = HudLayout.NameplateSize;
            Text name = UiFactory.Label("Name", root, _ctx.Font, HudLayout.FontSmall, TextAnchor.LowerCenter, HudPalette.Text);
            UiFactory.Place(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(HudLayout.NameplateSize.x * 2f, HudLayout.RowHeight));
            UiBar health = UiFactory.Bar("Health", root, _ctx.Font, HudPalette.Health);
            UiFactory.Stretch(health.Rect);
            return new Plate { Root = root, Name = name, Health = health };
        }
    }
}
