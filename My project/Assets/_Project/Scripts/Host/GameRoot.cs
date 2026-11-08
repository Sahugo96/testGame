using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Game.Client;
using Game.Client.CameraRig;
using Game.Client.Input;
using Game.Client.Localization;
using Game.Client.Net;
using Game.Client.State;
using Game.Client.UI;
using Game.Client.World;
using Game.Network;
using Game.Server;
using Game.Server.Items;
using Game.Server.Net;
using Game.Server.Persistence;
using Game.Shared.Content;
using Game.Shared.Core;
using UnityEngine;

namespace Game.Host
{
    /// <summary>
    /// Composition root. Modes: single player (server in this process), host (server in this process + other players
    /// over the network), client (remote server), dedicated server (-server, no graphics). The client always talks to
    /// the server through <see cref="IServerConnection"/>; the authoritative logic is <see cref="GameServer"/>.
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        private const string LanguageCode = "ru";
        private const string BuiltinFontName = "LegacyRuntime.ttf";
        private const float PlaneSizeUnits = 10f;
        private const int DedicatedFrameRateFactor = 2;
        private const string ConnectingKey = "menu.connecting";
        private const string ConnectFailedKey = "menu.error_connect";
        private const string DisconnectedKey = "menu.disconnected";
        private const string HostingKey = "menu.hosting";
        private const string PidFileName = "server.pid";

        private enum Mode
        {
            None,
            Single,
            Host,
            Client,
            Dedicated,
            Bots,
        }

        private ContentDatabase _content;
        private Mode _mode;
        private GameServer _server;
        private ServerNetworkHost _networkHost;
        private IServerConnection _connection;
        private ClientContext _ctx;
        private GameInput _input;
        private ThirdPersonCamera _cameraRig;
        private EntityViewManager _views;
        private HudRoot _hud;
        private StartMenuView _menu;
        private Transform _followed;
        private float _dedicatedAccumulator;
        private float _metricsTimer;
        private long _lastBytesSent;
        private Game.Bots.BotSwarm _bots;
        private const double BytesPerKiB = 1024d;
        private int _lastPlayerCount = -1;
        private string _playerName;
        private string _classId;
        private string _address;
        private int _port;
        private Task<NetworkClientSession> _pendingJoin;
        private FileStorage _storage;
        private CharacterPersistence _persistence;
        private bool _backedUp;
        private string _login;
        private string _password;
        private string _token;
        private const string SavesFolder = "Saves";

        private void Start()
        {
            Log.SetSink(new UnityLogSink());
            Application.runInBackground = true;
            var errors = new List<string>();
            _content = ServerContentFactory.Build(UnityContentLoader.LoadAll(), errors);
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

            LaunchOptions options = LaunchOptions.Parse(Environment.GetCommandLineArgs());
            _port = options.Port > 0 ? options.Port : _content.Config.DefaultPort;
            if (options.Bots > 0 && !string.IsNullOrEmpty(options.ConnectAddress))
            {
                StartBots(options.ConnectAddress, _port, options.Bots);
                return;
            }
            if (options.DedicatedServer || Application.isBatchMode)
            {
                StartDedicated();
                return;
            }

            BuildClient();
            if (options.AutoStart)
            {
                _menu.Prefill(options.PlayerName, options.ClassId ?? _content.Config.DefaultClassId, options.ConnectAddress, _port, options.Login, options.Password);
                if (options.Host)
                {
                    StartHost(_menu.PlayerName, _menu.ClassId, _port);
                }
                else
                {
                    StartJoin(_menu.PlayerName, _menu.ClassId, options.ConnectAddress, _port);
                }
            }
            else
            {
                ShowMenu(null);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_mode == Mode.Dedicated)
            {
                UpdateDedicated(dt);
                return;
            }
            if (_mode == Mode.Bots)
            {
                _bots.Update(dt);
                return;
            }
            if (_pendingJoin != null && _pendingJoin.IsCompleted)
            {
                CompleteJoin();
            }
            _persistence?.Update(dt);
            if (_connection == null)
            {
                return;
            }
            _connection.Update(dt);
            if (_connection is RemoteServerConnection remote && remote.Token != null)
            {
                _token = remote.Token;
            }
            if (_connection == null)
            {
                return;
            }
            _views.SetInterval(_connection.TickInterval);
            if (_connection.IsClosed)
            {
                string reason = _connection.CloseReason;
                Leave();
                ShowMenu(_ctx.Text.Format(DisconnectedKey, LocalizeReason(reason)));
                return;
            }
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
                _ctx.ClassSwitchRequested -= SwitchClass;
                _ctx.LeaveRequested -= LeaveToMenu;
            }
            Leave();
            _bots?.Dispose();
            _input?.Dispose();
        }

        private void BuildClient()
        {
            CreateGround(_content.Config.WorldHalfSize);
            Camera camera = Camera.main;
            if (camera == null)
            {
                camera = new GameObject("Main Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
            }
            camera.cullingMask &= RenderLayers.AllExceptPreview;
            _cameraRig = camera.gameObject.AddComponent<ThirdPersonCamera>();

            _ctx = new ClientContext
            {
                Content = _content,
                State = new ClientState(),
                Text = new JsonTableLocalizationService(LanguageCode),
                Font = Resources.GetBuiltinResource<Font>(BuiltinFontName),
                Camera = camera,
                Modifiers = new ModifierTracker(),
            };
            _input = new GameInput(_content.HudWindows);
            _input.Enable();

            _views = new GameObject("Entities").AddComponent<EntityViewManager>();
            _views.Init(_ctx, 1f / _content.Config.TickRate);
            var hudObject = new GameObject("HUD", typeof(RectTransform));
            _hud = hudObject.AddComponent<HudRoot>();
            _hud.Init(_ctx, _views, () => _input.PointerScreenPosition);
            gameObject.AddComponent<PlayerInputController>().Init(_ctx, _input, _cameraRig, _views, _hud);

            var menuObject = new GameObject("StartMenu", typeof(RectTransform));
            _menu = menuObject.AddComponent<StartMenuView>();
            _menu.Init(_ctx);
            _menu.SingleRequested = StartSingle;
            _menu.HostRequested = StartHost;
            _menu.JoinRequested = StartJoin;

            _ctx.ClassSwitchRequested += SwitchClass;
            _ctx.LeaveRequested += LeaveToMenu;
        }

        private void StartSingle(string playerName, string classId)
        {
            Leave();
            Remember(playerName, classId, null, _port);
            _server = CreateServer();
            _mode = Mode.Single;
            JoinLocal();
        }

        private void StartHost(string playerName, string classId, int port)
        {
            Leave();
            Remember(playerName, classId, null, port);
            _server = CreateServer();
            try
            {
                _networkHost = new ServerNetworkHost(_server, port, _persistence);
            }
            catch (Exception exception)
            {
                _server = null;
                ShowMenu(_ctx.Text.Format(ConnectFailedKey, exception.Message));
                return;
            }
            _mode = Mode.Host;
            JoinLocal();
            _hud.ShowInfo(_ctx.Text.Format(HostingKey, _networkHost.Port, LocalAddresses()));
        }

        private void StartJoin(string playerName, string classId, string address, int port)
        {
            Leave();
            Remember(playerName, classId, address, port);
            if (_login != _menu.Login)
            {
                _token = null;
            }
            _login = _menu.Login;
            _password = _menu.Password;
            string login = _login;
            string password = _password;
            string token = _token;
            ShowMenu(_ctx.Text.Get(ConnectingKey));
            // Connecting may take up to ConnectTimeoutMs: do it off the main thread so the window keeps drawing.
            _pendingJoin = Task.Run(() => NetworkClientSession.Connect(address, port, playerName, classId, login, password, token));
        }

        private void CompleteJoin()
        {
            Task<NetworkClientSession> join = _pendingJoin;
            _pendingJoin = null;
            if (join.IsFaulted || join.IsCanceled)
            {
                Exception error = join.Exception?.GetBaseException();
                ShowMenu(_ctx.Text.Format(ConnectFailedKey, error != null ? error.Message : string.Empty));
                return;
            }
            _mode = Mode.Client;
            Bind(new RemoteServerConnection(join.Result));
            _hud.ShowInfo(string.Empty);
        }

        /// <summary>Start menu over an empty scene: the HUD is hidden (there is no game to control).</summary>
        private void ShowMenu(string status)
        {
            _hud.gameObject.SetActive(false);
            _menu.Show(status);
        }

        private void JoinLocal()
        {
            EntityId player = _persistence.Enter(CharacterPersistence.LocalLogin, null, null, _playerName, _classId, true, out string error, out _);
            if (!player.IsValid)
            {
                Leave();
                ShowMenu(_ctx.Text.Get(error));
                return;
            }
            var local = new LocalServerConnection(_server, player);
            if (_networkHost != null)
            {
                local.BeforeTick = _networkHost.Poll;
            }
            Bind(local);
            _hud.ShowInfo(string.Empty);
        }

        private void Bind(IServerConnection connection)
        {
            _connection = connection;
            _ctx.Commands = new ClientCommands(connection);
            _ctx.State.Bind(connection);
            _menu.Hide();
            _hud.gameObject.SetActive(true);
        }

        /// <summary>Settings → switch class: replace the character (local) or reconnect with the new class (remote).</summary>
        private void SwitchClass(string classId)
        {
            switch (_mode)
            {
                case Mode.Single:
                case Mode.Host:
                    UnbindConnection();
                    _classId = classId;
                    JoinLocal();
                    break;
                case Mode.Client:
                    StartJoin(_playerName, classId, _address, _port);
                    break;
            }
        }

        private void LeaveToMenu()
        {
            Leave();
            ShowMenu(null);
        }

        /// <summary>Drops the current game (connection, local server, network host) and clears the view.</summary>
        private void Leave()
        {
            DropPendingJoin();
            UnbindConnection();
            _networkHost?.Dispose();
            _networkHost = null;
            _persistence?.SaveAll();
            _persistence = null;
            _server = null;
            _mode = Mode.None;
        }

        /// <summary>A connection attempt that is no longer wanted: its session is closed when (if) it arrives.</summary>
        private void DropPendingJoin()
        {
            if (_pendingJoin == null)
            {
                return;
            }
            _pendingJoin.ContinueWith(task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    task.Result.Dispose();
                }
            });
            _pendingJoin = null;
        }

        private void UnbindConnection()
        {
            if (_connection == null)
            {
                return;
            }
            _ctx.State.Unbind(_connection);
            if (_connection is LocalServerConnection local && _persistence != null)
            {
                _persistence.Leave(local.LocalPlayerId);
            }
            (_connection as IDisposable)?.Dispose();
            _connection = null;
            _ctx.Commands = null;
            _views.Clear();
            _followed = null;
            _ctx.Modifiers.Clear();
        }

        private void StartDedicated()
        {
            _server = CreateServer();
            _networkHost = new ServerNetworkHost(_server, _port, _persistence);
            _mode = Mode.Dedicated;
            Log.Info(LogTags.Server, $"Saves: {_storage.Root}");
            Application.targetFrameRate = _content.Config.TickRate * DedicatedFrameRateFactor;
            Log.Info(LogTags.Server, $"Dedicated server started on port {_networkHost.Port}. Addresses: {LocalAddresses()}");
            WritePidFile();
        }

        /// <summary>server.pid next to the executable: StopServer.bat stops exactly this process.</summary>
        private static void WritePidFile()
        {
            if (Application.isEditor)
            {
                return;
            }
            try
            {
                string folder = Path.GetDirectoryName(Application.dataPath);
                File.WriteAllText(Path.Combine(folder, PidFileName), Process.GetCurrentProcess().Id.ToString());
            }
            catch (Exception exception)
            {
                Log.Warning(LogTags.Server, $"Cannot write {PidFileName}: {exception.Message}");
            }
        }

        private void UpdateDedicated(float dt)
        {
            _persistence?.Update(dt);
            _dedicatedAccumulator += dt;
            while (_dedicatedAccumulator >= _server.TickInterval)
            {
                _dedicatedAccumulator -= _server.TickInterval;
                _networkHost.Poll();
                _server.Tick();
            }
            if (_networkHost.ConnectedPlayers != _lastPlayerCount)
            {
                _lastPlayerCount = _networkHost.ConnectedPlayers;
                Log.Info(LogTags.Server, $"Players online: {_lastPlayerCount}");
            }
            _metricsTimer += dt;
            if (_metricsTimer >= _content.Config.MetricsLogSeconds)
            {
                long bytes = _networkHost.BytesSent;
                double perPlayer = (bytes - _lastBytesSent) / _metricsTimer / Math.Max(1, _networkHost.ConnectedPlayers) / BytesPerKiB;
                Log.Info(LogTags.Server, $"Metrics: {_server.Metrics.Summary()}, out {perPlayer:0.0} KiB/s per player");
                _lastBytesSent = bytes;
                _metricsTimer = 0f;
            }
        }

        /// <summary>-bots N -connect ADDRESS: headless bots for load tests (module 3); no graphics, no player.</summary>
        private void StartBots(string address, int port, int count)
        {
            var classes = new List<string>(_content.Classes.Keys);
            _bots = Game.Bots.BotSwarm.Connect(address, port, count, classes.ToArray(), _content.Config.WorldHalfSize);
            _mode = Mode.Bots;
            Application.targetFrameRate = _content.Config.TickRate;
            Log.Info(LogTags.Client, $"{count} bots connected to {address}:{port}");
        }

        /// <summary>Server error keys ("net.*") are localized; transport reasons are shown as is.</summary>
        private string LocalizeReason(string reason)
        {
            if (string.IsNullOrEmpty(reason))
            {
                return string.Empty;
            }
            return reason.StartsWith(NetworkConstants.ErrorKeyPrefix, StringComparison.Ordinal) ? _ctx.Text.Get(reason) : reason;
        }

        /// <summary>
        /// A server with saves (module 13): accounts and characters in persistentDataPath/Saves, the item audit log on disk,
        /// the unique item registry; the folder is backed up once per launch (the newest BackupsToKeep copies stay).
        /// </summary>
        private GameServer CreateServer()
        {
            _storage = new FileStorage(Path.Combine(Application.persistentDataPath, SavesFolder), new UnityJsonCodec());
            if (!_backedUp)
            {
                _storage.Backup(_content.Config.BackupsToKeep);
                _backedUp = true;
            }
            var server = new GameServer(_content, new SystemRandom(Environment.TickCount), new FileAuditLog(_storage.AuditPath));
            server.Context.Items.UniqueRegistry = _storage;
            _persistence = new CharacterPersistence(server, _storage, _storage, _storage);
            return server;
        }

        private void Remember(string playerName, string classId, string address, int port)
        {
            _playerName = playerName;
            _classId = classId ?? _content.Config.DefaultClassId;
            _address = address;
            _port = port;
        }

        /// <summary>IPv4 addresses of this computer, to tell a friend where to connect.</summary>
        private static string LocalAddresses()
        {
            var addresses = new List<string>();
            try
            {
                foreach (IPAddress address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        addresses.Add(address.ToString());
                    }
                }
            }
            catch (Exception)
            {
                // No DNS: leave the list empty.
            }
            return string.Join(", ", addresses);
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
