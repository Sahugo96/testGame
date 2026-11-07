using Game.Client.CameraRig;
using Game.Client.UI;
using Game.Client.World;
using Game.Shared.Content;
using Game.Shared.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Client.Input
{
    /// <summary>
    /// Turns input into server commands (the client sends only intents): WASD relative to the camera, skill keys with
    /// the current Shift/Ctrl modifier, Tab/click targeting, window hotkeys, Esc.
    /// Note: in the Unity Editor Ctrl+digit opens editor windows — test Ctrl variants in a build or rebind (03, 7.2).
    /// </summary>
    public sealed class PlayerInputController : MonoBehaviour
    {
        private const float DirectionPrecision = 100f;
        private const float MaxRayDistance = 500f;

        private ClientContext _ctx;
        private GameInput _input;
        private ThirdPersonCamera _camera;
        private EntityViewManager _views;
        private HudRoot _hud;

        public void Init(ClientContext ctx, GameInput input, ThirdPersonCamera cameraRig, EntityViewManager views, HudRoot hud)
        {
            _ctx = ctx;
            _input = input;
            _camera = cameraRig;
            _views = views;
            _hud = hud;
        }

        private void Update()
        {
            if (_ctx == null || _ctx.Commands == null)
            {
                return;
            }
            UpdateModifiers();
            bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (_input.LookHold.IsPressed())
            {
                _camera.Rotate(_input.Look.ReadValue<Vector2>());
            }
            if (!pointerOverUi)
            {
                _camera.ZoomBy(_input.Zoom.ReadValue<float>());
            }
            UpdateMovement();

            for (int i = 0; i < _input.SkillSlotCount && i < _hud.SkillBar.VisibleRowSlots; i++)
            {
                if (_input.SkillSlots[i].WasPressedThisFrame())
                {
                    string skillId = _hud.SkillBar.SkillAt(i);
                    if (skillId != null)
                    {
                        _ctx.Commands.UseSkill(skillId, default, _ctx.Modifiers.Current);
                    }
                }
            }
            if (_input.CycleTarget.WasPressedThisFrame())
            {
                _ctx.Commands.CycleTarget();
            }
            if (_input.Select.WasPressedThisFrame() && !pointerOverUi)
            {
                SelectUnderPointer();
            }
            foreach (var pair in _input.WindowHotkeys)
            {
                if (pair.Value.WasPressedThisFrame())
                {
                    _hud.Windows.Toggle(pair.Key);
                }
            }
            if (_input.Cancel.WasPressedThisFrame())
            {
                if (_hud.Windows.AnyOpen)
                {
                    _hud.Windows.CloseTop();
                }
                else
                {
                    _ctx.Commands.SelectTarget(EntityId.None);
                }
            }
        }

        private void UpdateModifiers()
        {
            Track(_input.ModifierShift, SkillModifier.Shift);
            Track(_input.ModifierCtrl, SkillModifier.Ctrl);
        }

        private void Track(UnityEngine.InputSystem.InputAction action, SkillModifier modifier)
        {
            if (action.WasPressedThisFrame())
            {
                _ctx.Modifiers.Press(modifier);
            }
            if (action.WasReleasedThisFrame())
            {
                _ctx.Modifiers.Release(modifier);
            }
        }

        private void UpdateMovement()
        {
            Vector2 move = _input.Move.ReadValue<Vector2>();
            float yaw = _camera.Yaw * Mathf.Deg2Rad;
            var forward = new Vector2(Mathf.Sin(yaw), Mathf.Cos(yaw));
            var right = new Vector2(Mathf.Cos(yaw), -Mathf.Sin(yaw));
            Vector2 world = right * move.x + forward * move.y;
            if (world.sqrMagnitude > 1f)
            {
                world.Normalize();
            }
            _ctx.Commands.Move(Quantize(world.x), Quantize(world.y));
        }

        private void SelectUnderPointer()
        {
            Ray ray = _ctx.Camera.ScreenPointToRay(_input.PointerScreenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, MaxRayDistance) && _views.TryGetEntity(hit.collider, out EntityId id))
            {
                if (_views.IsStation(id))
                {
                    _hud.OpenStation(id);
                    return;
                }
                if (_views.IsLoot(id))
                {
                    _ctx.Commands.PickUpLoot(id);
                    return;
                }
                if (id != _ctx.State.LocalPlayerId)
                {
                    _ctx.Commands.SelectTarget(id);
                }
                return;
            }
            _ctx.Commands.SelectTarget(EntityId.None);
        }

        private static float Quantize(float value) => Mathf.Round(value * DirectionPrecision) / DirectionPrecision;
    }
}
