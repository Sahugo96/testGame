using System.Collections.Generic;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using Game.Shared.Core;
using Game.Shared.Protocol;
using Game.Client.UI;
using Game.Shared.Content.Items;
using Game.Client.World.Motion;
using UnityEngine;
using EntityId = Game.Shared.Core.EntityId;

namespace Game.Client.World
{
    /// <summary>Creates, moves and removes entity visuals from world snapshots; maps colliders back to entity ids for clicks.</summary>
    public sealed class EntityViewManager : MonoBehaviour
    {
        private const float TargetMarkerMinDiameter = 1.5f;
        /// <summary>The marker is this much wider than the figure it lies under.</summary>
        private const float TargetMarkerMargin = 1.35f;
        private const float TargetMarkerThickness = 0.01f;
        private const float TargetMarkerHeight = 0.03f;

        private readonly Dictionary<EntityId, EntityView> _views = new Dictionary<EntityId, EntityView>();
        private readonly Dictionary<Collider, EntityId> _byCollider = new Dictionary<Collider, EntityId>();
        private readonly List<EntityId> _stale = new List<EntityId>();
        private readonly HumanClipLibrary _clips = new HumanClipLibrary();
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
            ctx.State.Emote += OnEmote;
            _targetMarker = WorldPrimitives.Part("TargetMarker", transform, PrimitiveType.Cylinder, Vector3.zero,
                new Vector3(TargetMarkerMinDiameter, TargetMarkerThickness, TargetMarkerMinDiameter), Vector3.zero, WorldPrimitives.Glow(WorldPalette.TargetMarker));
            _targetMarker.gameObject.SetActive(false);
        }

        /// <summary>Seconds between snapshots: local host ticks every frame, remote servers less often.</summary>
        public void SetInterval(float interval) => _interval = interval;

        public bool TryGetEntity(Collider collider, out EntityId id) => _byCollider.TryGetValue(collider, out id);

        public bool TryGetView(EntityId id, out EntityView view) => _views.TryGetValue(id, out view);

        public void Clear()
        {
            ReleaseMarker();
            foreach (EntityView view in _views.Values)
            {
                Destroy(view.gameObject);
            }
            _views.Clear();
            _byCollider.Clear();
            LocalPlayerTransform = null;
        }

        /// <summary>Takes the marker back from the figure it lies under (before that figure is destroyed) and hides it.</summary>
        private void ReleaseMarker()
        {
            _targetMarker.SetParent(transform, false);
            _targetMarker.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_ctx == null)
            {
                return;
            }
            _ctx.State.WorldChanged -= OnWorldChanged;
            _ctx.State.PlayerChanged -= OnPlayerChanged;
            _ctx.State.Emote -= OnEmote;
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
                view.SetInterval(_interval);
                view.SetTarget(entity.Position, entity.Yaw, entity.IsDead);
            }
            foreach (EntityId id in _stale)
            {
                EntityView view = _views[id];
                if (_targetMarker.parent == view.transform)
                {
                    ReleaseMarker();
                }
                _byCollider.Remove(view.Collider);
                _views.Remove(id);
                Destroy(view.gameObject);
            }
        }

        /// <summary>An entity in view started an emote: its figure plays the emote's motion clip.</summary>
        private void OnEmote(EmoteMessage message)
        {
            if (!_views.TryGetValue(message.EntityId, out EntityView view))
            {
                return;
            }
            foreach (EmoteDto emote in _ctx.Content.Config.Emotes ?? new EmoteDto[0])
            {
                if (emote.Id == message.EmoteId)
                {
                    view.PlayClip(_clips.Get(emote.Animation));
                    return;
                }
            }
        }

        private void OnPlayerChanged()
        {
            EntityId target = _ctx.State.Player.TargetId;
            if (!target.IsValid || !_views.TryGetValue(target, out EntityView view))
            {
                if (_targetMarker.gameObject.activeSelf)
                {
                    ReleaseMarker();
                }
                return;
            }
            if (_targetMarker.parent != view.transform || !_targetMarker.gameObject.activeSelf)
            {
                // The marker lies on the ground under the target and moves with it.
                float diameter = Mathf.Max(TargetMarkerMinDiameter, view.Footprint * TargetMarkerMargin);
                _targetMarker.SetParent(view.transform, false);
                _targetMarker.localPosition = new Vector3(0f, view.GroundOffset + TargetMarkerHeight, 0f);
                _targetMarker.localScale = new Vector3(diameter, TargetMarkerThickness, diameter);
                _targetMarker.gameObject.SetActive(true);
            }
        }

        private EntityView Create(EntitySnapshot entity)
        {
            var go = new GameObject($"{entity.Kind}_{entity.DefinitionId}_{entity.Id.Value}");
            go.transform.SetParent(transform, false);
            EntityView view = go.AddComponent<EntityView>();
            view.Init(entity.Id, _interval, WorldFigures.Build(ShapeFor(entity), ColorFor(entity)), WorldPalette.Dead, FacingFor(entity));
            _views[entity.Id] = view;
            _byCollider[view.Collider] = entity.Id;
            if (entity.Id == _ctx.State.LocalPlayerId)
            {
                LocalPlayerTransform = go.transform;
            }
            return view;
        }

        /// <summary>Content of a station entity (type, look) by its definition id; a player's merchant uses World.MerchantVisual.</summary>
        public StationDto FindStation(string definitionId)
        {
            StationDto merchant = _ctx.Content.World.MerchantVisual;
            if (merchant != null && merchant.Id == definitionId)
            {
                return merchant;
            }
            return System.Array.Find(_ctx.Content.World.Stations ?? new StationDto[0], s => s.Id == definitionId);
        }

        /// <summary>A drop on the ground (06, 7.8): clicking it picks it up.</summary>
        public bool IsLoot(EntityId id) => _ctx.State.TryGetEntity(id, out EntitySnapshot entity) && entity.Kind == EntityKind.Loot;

        public bool IsStation(EntityId id) => _ctx.State.TryGetEntity(id, out EntitySnapshot entity) && entity.Kind == EntityKind.Npc;

        /// <summary>
        /// Temporary (DEFAULT): stations have no direction in the content, so they face the spawn square — otherwise
        /// half of them would stand with their backs to the player. Everything else is turned by the server's yaw.
        /// </summary>
        private float FacingFor(EntitySnapshot entity)
        {
            if (entity.Kind != EntityKind.Npc)
            {
                return 0f;
            }
            float dx = _ctx.Content.World.PlayerSpawnX - entity.Position.X;
            float dz = _ctx.Content.World.PlayerSpawnZ - entity.Position.Z;
            return dx * dx + dz * dz > Mathf.Epsilon ? Mathf.Atan2(dx, dz) * Mathf.Rad2Deg - entity.Yaw : 0f;
        }

        /// <summary>The figure key (see <see cref="WorldFigures"/>): from the content for mobs and stations.</summary>
        private string ShapeFor(EntitySnapshot entity)
        {
            switch (entity.Kind)
            {
                case EntityKind.Loot:
                    return WorldFigures.Loot;
                case EntityKind.Npc:
                    return FindStation(entity.DefinitionId)?.VisualShape;
                case EntityKind.Mob:
                    return _ctx.Content.Mobs.TryGetValue(entity.DefinitionId, out MobDto mob) ? mob.VisualShape : null;
                case EntityKind.Player:
                    return PlayerShape(entity.DefinitionId);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Temporary (DEFAULT): a class has no look of its own in the content yet, so the figure follows its primary
        /// attribute — intelligence gives a robe and a staff, strength a helmet, a sword and a shield.
        /// </summary>
        private string PlayerShape(string classId)
        {
            if (classId == null || !_ctx.Content.Classes.TryGetValue(classId, out ClassDefinition cls))
            {
                return WorldFigures.Humanoid;
            }
            switch (cls.PrimaryAttribute)
            {
                case StatId.Intelligence:
                    return WorldFigures.Caster;
                case StatId.Strength:
                    return WorldFigures.Fighter;
                default:
                    return WorldFigures.Humanoid;
            }
        }

        private Color ColorFor(EntitySnapshot entity)
        {
            if (entity.Kind == EntityKind.Npc)
            {
                StationDto station = FindStation(entity.DefinitionId);
                return station != null && ColorUtility.TryParseHtmlString(station.VisualColor, out Color stationColor) ? stationColor : WorldPalette.Dead;
            }
            if (entity.Kind == EntityKind.Loot)
            {
                return _ctx.Content.Items.TryGetValue(entity.DefinitionId ?? string.Empty, out ItemDefinition item)
                    ? HudPalette.ForRarity(item.Rarity) : WorldPalette.Dead;
            }
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
