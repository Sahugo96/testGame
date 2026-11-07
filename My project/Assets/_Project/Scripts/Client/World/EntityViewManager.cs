using System.Collections.Generic;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using Game.Shared.Core;
using Game.Shared.Protocol;
using UnityEngine;

namespace Game.Client.World
{
    /// <summary>Creates, moves and removes entity visuals from world snapshots; maps colliders back to entity ids for clicks.</summary>
    public sealed class EntityViewManager : MonoBehaviour
    {
        private const float TargetMarkerDiameter = 1.6f;
        private const float TargetMarkerThickness = 0.02f;
        private const float TargetMarkerHeight = 0.03f;

        private readonly Dictionary<EntityId, EntityView> _views = new Dictionary<EntityId, EntityView>();
        private readonly Dictionary<Collider, EntityId> _byCollider = new Dictionary<Collider, EntityId>();
        private readonly List<EntityId> _stale = new List<EntityId>();
        private ClientContext _ctx;
        private float _interval;
        private Transform _targetMarker;

        public Transform LocalPlayerTransform { get; private set; }

        public void Init(ClientContext ctx, float tickInterval)
        {
            _ctx = ctx;
            _interval = tickInterval;
            ctx.State.WorldChanged += OnWorldChanged;
            ctx.State.PlayerChanged += OnPlayerChanged;
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "TargetMarker";
            Destroy(marker.GetComponent<Collider>());
            marker.GetComponent<Renderer>().material.color = WorldPalette.TargetMarker;
            marker.transform.SetParent(transform, false);
            marker.transform.localScale = new Vector3(TargetMarkerDiameter, TargetMarkerThickness, TargetMarkerDiameter);
            marker.SetActive(false);
            _targetMarker = marker.transform;
        }

        public bool TryGetEntity(Collider collider, out EntityId id) => _byCollider.TryGetValue(collider, out id);

        public bool TryGetView(EntityId id, out EntityView view) => _views.TryGetValue(id, out view);

        public void Clear()
        {
            foreach (EntityView view in _views.Values)
            {
                Destroy(view.gameObject);
            }
            _views.Clear();
            _byCollider.Clear();
            LocalPlayerTransform = null;
        }

        private void OnDestroy()
        {
            if (_ctx == null)
            {
                return;
            }
            _ctx.State.WorldChanged -= OnWorldChanged;
            _ctx.State.PlayerChanged -= OnPlayerChanged;
        }

        private void OnWorldChanged()
        {
            _stale.Clear();
            _stale.AddRange(_views.Keys);
            foreach (EntitySnapshot entity in _ctx.State.World.Entities)
            {
                _stale.Remove(entity.Id);
                if (!_views.TryGetValue(entity.Id, out EntityView view))
                {
                    view = Create(entity);
                    view.Snap(entity.Position, entity.Yaw);
                }
                view.SetTarget(entity.Position, entity.Yaw, entity.IsDead);
            }
            foreach (EntityId id in _stale)
            {
                EntityView view = _views[id];
                _byCollider.Remove(view.Collider);
                _views.Remove(id);
                Destroy(view.gameObject);
            }
        }

        private void OnPlayerChanged()
        {
            EntityId target = _ctx.State.Player.TargetId;
            bool show = target.IsValid && _views.TryGetValue(target, out EntityView view);
            _targetMarker.gameObject.SetActive(show);
            if (show)
            {
                _targetMarker.SetParent(_views[target].transform, false);
                _targetMarker.localPosition = new Vector3(0f, -1f + TargetMarkerHeight, 0f);
            }
        }

        private EntityView Create(EntitySnapshot entity)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"{entity.Kind}_{entity.DefinitionId}_{entity.Id.Value}";
            go.transform.SetParent(transform, false);
            EntityView view = go.AddComponent<EntityView>();
            view.Init(entity.Id, _interval, ColorFor(entity), WorldPalette.Dead);
            _views[entity.Id] = view;
            _byCollider[view.Collider] = entity.Id;
            if (entity.Id == _ctx.State.LocalPlayerId)
            {
                LocalPlayerTransform = go.transform;
            }
            return view;
        }

        private Color ColorFor(EntitySnapshot entity)
        {
            if (entity.Kind == EntityKind.Player)
            {
                return entity.Id == _ctx.State.LocalPlayerId ? WorldPalette.LocalPlayer : WorldPalette.OtherPlayer;
            }
            if (_ctx.Content.Mobs.TryGetValue(entity.DefinitionId, out MobDto mob)
                && ColorUtility.TryParseHtmlString(mob.VisualColor, out Color color))
            {
                return color;
            }
            return WorldPalette.DefaultMob;
        }
    }
}
