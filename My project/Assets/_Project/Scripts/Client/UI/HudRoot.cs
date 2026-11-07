using System;
using Game.Client.UI.Widgets;
using Game.Client.UI.Windows;
using Game.Client.World;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using Game.Shared.Core;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Player overlay (02, 7.13): one screen-space canvas with zones — player frame top-left, target top-center,
    /// minimap top-right, skill and consumable panels bottom-center, mechanics menu bottom-left, system menu bottom-right,
    /// windows in the center. Built in code from data (window registry, config).
    /// </summary>
    public sealed class HudRoot : MonoBehaviour
    {
        private ClientContext _ctx;
        private Func<Vector2> _pointer;
        private Tooltip _tooltip;
        private PlayerFrameWidget _playerFrame;
        private TargetFrameWidget _targetFrame;
        private ConsumableBarWidget _consumables;
        private MinimapWidget _minimap;
        private MessageWidget _message;
        private CombatTextWidget _combatText;
        private NameplateWidget _nameplates;
        private ExperienceBarWidget _experience;
        private EntityViewManager _views;
        private Text _info;
        private DuelWidget _duel;
        private GroupFramesWidget _groupFrames;
        private StatusRowWidget _playerStatuses;
        private GroupPromptWidget _groupPrompt;
        private DuelArenaView _arena;
        private string _classId;

        private const float InfoWidth = 520f;
        private const int PlayerStatusColumns = 4;
        private const int PlayerStatusMax = 8;
        private const int InfoLines = 2;

        public SkillBarWidget SkillBar { get; private set; }
        public WindowManager Windows { get; private set; }

        public void Init(ClientContext ctx, EntityViewManager views, Func<Vector2> pointer)
        {
            _ctx = ctx;
            _views = views;
            _pointer = pointer;

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = HudLayout.ReferenceResolution;
            scaler.matchWidthOrHeight = HudLayout.ScalerMatch;
            gameObject.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            RectTransform root = (RectTransform)transform;
            RectTransform worldLayer = Layer("WorldLayer", root);
            RectTransform hudLayer = Layer("HudLayer", root);
            RectTransform windowLayer = Layer("WindowLayer", root);
            _tooltip = new Tooltip(root, ctx.Font);

            float margin = HudLayout.Margin;
            RectTransform topLeft = Zone("TopLeft", hudLayer, new Vector2(0f, 1f), new Vector2(margin, -margin));
            RectTransform topCenter = Zone("TopCenter", hudLayer, new Vector2(0.5f, 1f), new Vector2(0f, -margin));
            RectTransform topRight = Zone("TopRight", hudLayer, Vector2.one, new Vector2(-margin, -margin));
            RectTransform bottomCenter = Zone("BottomCenter", hudLayer, new Vector2(0.5f, 0f), new Vector2(0f, margin));
            RectTransform bottomLeft = Zone("BottomLeft", hudLayer, Vector2.zero, new Vector2(margin, margin));
            RectTransform bottomRight = Zone("BottomRight", hudLayer, new Vector2(1f, 0f), new Vector2(-margin, margin));

            _playerFrame = new PlayerFrameWidget(topLeft, ctx);
            _targetFrame = new TargetFrameWidget(topCenter, ctx, _tooltip);
            _playerStatuses = new StatusRowWidget(topLeft, ctx, _tooltip, new Vector2(0f, 1f), new Vector2(HudLayout.PlayerFrameSize.x + HudLayout.Gap, 0f), PlayerStatusColumns, PlayerStatusMax);
            _minimap = new MinimapWidget(topRight, ctx);
            SkillBar = new SkillBarWidget(bottomCenter, ctx, _tooltip);
            float skillRowsHeight = ctx.Content.Config.SkillBarRows * (HudLayout.SkillSlotSize + HudLayout.Gap);
            _consumables = new ConsumableBarWidget(bottomCenter, ctx, _tooltip, skillRowsHeight + HudLayout.Gap);
            Windows = new WindowManager(windowLayer, canvas, ctx, _tooltip);
            new IconMenuWidget(bottomCenter, ctx, _tooltip, MenuZones.SkillBarLeft, -SkillBar.RowWidth * 0.5f - HudLayout.Gap, Windows.Toggle);
            new MenuBarWidget(bottomLeft, ctx, MenuZones.BottomLeft, false, Windows.Toggle);
            new MenuBarWidget(bottomRight, ctx, MenuZones.BottomRight, true, Windows.Toggle);
            _experience = new ExperienceBarWidget(bottomRight, ctx, HudLayout.MenuButtonHeight + HudLayout.Gap);
            _message = new MessageWidget(hudLayer, ctx);
            _duel = new DuelWidget(hudLayer, ctx);
            _groupFrames = new GroupFramesWidget(topLeft, ctx);
            _groupPrompt = new GroupPromptWidget(hudLayer, ctx);
            _arena = new GameObject("DuelArena").AddComponent<DuelArenaView>();
            _arena.Init(ctx);
            _info = UiFactory.Label("Info", bottomLeft, ctx.Font, HudLayout.FontSmall, TextAnchor.LowerLeft, HudPalette.TextDim);
            UiFactory.Place(_info.rectTransform, Vector2.zero, Vector2.zero, new Vector2(0f, HudLayout.MenuButtonHeight + HudLayout.Gap),
                new Vector2(InfoWidth, HudLayout.FontHuge * InfoLines));
            _info.text = string.Empty;
            _combatText = new CombatTextWidget(worldLayer, ctx, views);
            _nameplates = new NameplateWidget(worldLayer, ctx, views);

            ctx.State.PlayerChanged += OnPlayerChanged;
            ctx.State.WorldChanged += OnWorldChanged;
            ctx.State.CombatEvent += _combatText.Show;
            ctx.State.Rejected += _message.Show;
            ctx.State.Notification += _message.Show;
        }

        /// <summary>Persistent info line bottom-left (e.g. "hosting on port ..."); empty string hides it.</summary>
        public void ShowInfo(string text)
        {
            if (_info != null)
            {
                _info.text = text ?? string.Empty;
            }
        }

        /// <summary>Opens the window of a clicked station (whetstone, anvil) if the player stands close enough (02, 7.13).</summary>
        public void OpenStation(EntityId station)
        {
            PlayerStateSnapshot player = _ctx.State.Player;
            if (player == null || !_ctx.State.TryGetEntity(station, out EntitySnapshot stationEntity)
                || !_ctx.State.TryGetEntity(player.EntityId, out EntitySnapshot self))
            {
                return;
            }
            if (Vec3.DistanceXZ(stationEntity.Position, self.Position) > _ctx.Content.Config.InteractRange)
            {
                _message.Show(RejectReasons.NotNearStation);
                return;
            }
            StationDto content = _views.FindStation(stationEntity.DefinitionId);
            string windowId = content == null ? null : WindowFactory.ForStation(content.Type);
            if (windowId == null)
            {
                return;
            }
            _ctx.ActiveStation = station;
            Windows.Open(windowId);
        }

        private void OnDisable()
        {
            if (_arena != null)
            {
                _arena.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_ctx == null)
            {
                return;
            }
            _ctx.State.PlayerChanged -= OnPlayerChanged;
            _ctx.State.WorldChanged -= OnWorldChanged;
            _ctx.State.CombatEvent -= _combatText.Show;
            _ctx.State.Rejected -= _message.Show;
            _ctx.State.Notification -= _message.Show;
            _minimap.Destroy();
        }

        private void Update()
        {
            if (_ctx == null)
            {
                return;
            }
            float dt = Time.deltaTime;
            _tooltip.Follow(_pointer());
            _minimap.Follow(_views.LocalPlayerTransform);
            _minimap.Tick();
            _message.Tick(dt);
            _combatText.Tick(dt);
            _nameplates.Tick();
            _arena.Tick();
        }

        private void OnPlayerChanged()
        {
            PlayerStateSnapshot state = _ctx.State.Player;
            if (state.ClassId != _classId && _ctx.Content.Classes.TryGetValue(state.ClassId, out ClassDefinition cls))
            {
                _classId = state.ClassId;
                SkillBar.AssignClassSkills(cls);
            }
            _playerFrame.Refresh(state);
            _experience.Refresh(state);
            _targetFrame.Refresh(state);
            _duel.Refresh(state);
            _groupFrames.Refresh(state);
            _groupPrompt.Refresh(state);
            SkillBar.Refresh(state);
            _consumables.Refresh(state);
            Windows.Refresh(state);
        }

        private void OnWorldChanged()
        {
            PlayerStateSnapshot player = _ctx.State.Player;
            _playerStatuses.Refresh(player != null && _ctx.State.TryGetEntity(player.EntityId, out EntitySnapshot self) ? self.Statuses : null);
            _targetFrame.Refresh(_ctx.State.Player);
            _groupFrames.Refresh(_ctx.State.Player);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static RectTransform Layer(string name, RectTransform parent) => UiFactory.Stretch(UiFactory.Rect(name, parent));

        private static RectTransform Zone(string name, RectTransform parent, Vector2 anchor, Vector2 offset) =>
            UiFactory.Place(UiFactory.Rect(name, parent), anchor, anchor, offset, Vector2.zero);
    }
}
