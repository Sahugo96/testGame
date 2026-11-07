using System;
using System.Collections.Generic;
using Game.Client;
using Game.Client.CameraRig;
using Game.Client.Input;
using Game.Client.Localization;
using Game.Client.State;
using Game.Client.UI;
using Game.Client.World;
using Game.Server;
using Game.Server.Items;
using Game.Shared.Content;
using Game.Shared.Core;
using UnityEngine;

namespace Game.Host
{
    /// <summary>
    /// Composition root of the local vertical slice: builds content, the authoritative server, the client (camera,
    /// input, entity views, HUD) and connects them through <see cref="LocalServerConnection"/>.
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        private const string LanguageCode = "ru";
        private const string BuiltinFontName = "LegacyRuntime.ttf";
        private const float PlaneSizeUnits = 10f;

        private GameServer _server;
        private LocalServerConnection _connection;
        private ClientContext _ctx;
        private GameInput _input;
        private ThirdPersonCamera _cameraRig;
        private EntityViewManager _views;
        private Transform _followed;

        private void Start()
        {
            Log.SetSink(new UnityLogSink());
            var errors = new List<string>();
            ContentDatabase content = ServerContentFactory.Build(UnityContentLoader.LoadAll(), errors);
            if (errors.Count > 0)
            {
                foreach (string error in errors)
                {
                    Log.Error(LogTags.Content, error);
                }
                Log.Error(LogTags.Content, "Content is broken: the game does not start (01, 7.10).");
                enabled = false;
                return;
            }

            _server = new GameServer(content, new SystemRandom(Environment.TickCount), new InMemoryAuditLog());
            CreateGround(content.Config.WorldHalfSize);

            Camera camera = Camera.main;
            if (camera == null)
            {
                camera = new GameObject("Main Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
            }
            _cameraRig = camera.gameObject.AddComponent<ThirdPersonCamera>();

            _ctx = new ClientContext
            {
                Content = content,
                State = new ClientState(),
                Text = new JsonTableLocalizationService(LanguageCode),
                Font = Resources.GetBuiltinResource<Font>(BuiltinFontName),
                Camera = camera,
                Modifiers = new ModifierTracker(),
            };
            _input = new GameInput(content.HudWindows);
            _input.Enable();

            _views = new GameObject("Entities").AddComponent<EntityViewManager>();
            _views.Init(_ctx, _server.TickInterval);
            var hudObject = new GameObject("HUD", typeof(RectTransform));
            HudRoot hud = hudObject.AddComponent<HudRoot>();
            hud.Init(_ctx, _views, () => _input.PointerScreenPosition);
            gameObject.AddComponent<PlayerInputController>().Init(_ctx, _input, _cameraRig, _views, hud);

            _ctx.ClassSwitchRequested += Connect;
            Connect(content.Config.DefaultClassId);
        }

        private void Update()
        {
            if (_connection == null)
            {
                return;
            }
            _connection.Update(Time.deltaTime);
            if (_views.LocalPlayerTransform != _followed)
            {
                _followed = _views.LocalPlayerTransform;
                _cameraRig.SetTarget(_followed);
            }
        }

        private void OnDestroy()
        {
            if (_ctx != null)
            {
                _ctx.ClassSwitchRequested -= Connect;
            }
            _input?.Dispose();
        }

        /// <summary>Joins the local server with the class; on a repeated call replaces the character (settings → switch class).</summary>
        private void Connect(string classId)
        {
            if (_connection != null)
            {
                _ctx.State.Unbind(_connection);
                _server.RemovePlayer(_connection.LocalPlayerId);
                _views.Clear();
                _followed = null;
            }
            EntityId player = _server.AddPlayer(classId);
            _connection = new LocalServerConnection(_server, player);
            _ctx.Commands = new ClientCommands(_connection);
            _ctx.State.Bind(_connection);
        }

        private static void CreateGround(float halfSize)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            float scale = halfSize * 2f / PlaneSizeUnits;
            ground.transform.localScale = new Vector3(scale, 1f, scale);
            ground.GetComponent<Renderer>().material.color = WorldPalette.Ground;
        }
    }
}
