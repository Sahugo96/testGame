using System;
using System.Collections.Generic;
using Game.Shared.Content;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Start menu: player name, class, server address and port; play alone, host a server for friends, or join one.
    /// Built in code on its own overlay canvas above the HUD.
    /// </summary>
    public sealed class StartMenuView : MonoBehaviour
    {
        private const int SortingOrder = 100;
        private static readonly Vector2 PanelSize = new Vector2(520f, 470f);
        private const float RowHeight = 40f;
        private const float LabelWidth = 150f;
        private const int NameLimit = 16;
        private const int AddressLimit = 64;
        private const int PortLimit = 5;
        private const string TitleKey = "menu.title";
        private const string NameKey = "menu.name";
        private const string ClassKey = "menu.class";
        private const string AddressKey = "menu.address";
        private const string PortKey = "menu.port";
        private const string SingleKey = "menu.single";
        private const string HostKey = "menu.host";
        private const string JoinKey = "menu.join";
        private const string DefaultAddress = "127.0.0.1";

        private readonly List<Button> _classButtons = new List<Button>();
        private readonly List<string> _classIds = new List<string>();
        private ClientContext _ctx;
        private GameObject _root;
        private InputField _name;
        private InputField _address;
        private InputField _port;
        private Text _status;
        private string _classId;

        /// <summary>(name, classId) — play alone.</summary>
        public Action<string, string> SingleRequested;

        /// <summary>(name, classId, port) — host a server and play on it.</summary>
        public Action<string, string, int> HostRequested;

        /// <summary>(name, classId, address, port) — join a server.</summary>
        public Action<string, string, string, int> JoinRequested;

        public void Init(ClientContext ctx)
        {
            _ctx = ctx;
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = HudLayout.ReferenceResolution;
            scaler.matchWidthOrHeight = HudLayout.ScalerMatch;
            gameObject.AddComponent<GraphicRaycaster>();

            Image backdrop = UiFactory.Panel("Backdrop", transform, HudPalette.Backdrop);
            UiFactory.Stretch(backdrop.rectTransform);
            _root = backdrop.gameObject;
            Image panel = UiFactory.Panel("Panel", backdrop.transform, HudPalette.PanelBackground);
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, PanelSize);

            float y = -HudLayout.Margin;
            Text title = UiFactory.Label("Title", panel.transform, ctx.Font, HudLayout.FontHuge, TextAnchor.MiddleCenter, HudPalette.Cast);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(PanelSize.x, RowHeight));
            title.text = ctx.Text.Get(TitleKey);
            y -= RowHeight + HudLayout.Gap * 2f;

            _name = Field(panel.transform, NameKey, ref y, NameLimit, string.Empty);
            ClassRow(panel.transform, ref y);
            _address = Field(panel.transform, AddressKey, ref y, AddressLimit, DefaultAddress);
            _port = Field(panel.transform, PortKey, ref y, PortLimit, ctx.Content.Config.DefaultPort.ToString());
            _port.contentType = InputField.ContentType.IntegerNumber;
            y -= HudLayout.Gap * 2f;

            float buttonWidth = (PanelSize.x - HudLayout.Margin * 2f - HudLayout.Gap * 2f) / 3f;
            AddButton(panel.transform, SingleKey, 0, buttonWidth, y, () => SingleRequested?.Invoke(PlayerName, _classId));
            AddButton(panel.transform, HostKey, 1, buttonWidth, y, () => HostRequested?.Invoke(PlayerName, _classId, Port));
            AddButton(panel.transform, JoinKey, 2, buttonWidth, y, () => JoinRequested?.Invoke(PlayerName, _classId, Address, Port));
            y -= HudLayout.MenuButtonHeight + HudLayout.Gap * 2f;

            _status = UiFactory.Label("Status", panel.transform, ctx.Font, HudLayout.FontNormal, TextAnchor.UpperCenter, HudPalette.Warning);
            UiFactory.Place(_status.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y),
                new Vector2(PanelSize.x - HudLayout.Margin * 2f, RowHeight * 2f));
            SelectClass(ctx.Content.Config.DefaultClassId);
        }

        public string PlayerName => _name.text;

        public string Address => string.IsNullOrWhiteSpace(_address.text) ? DefaultAddress : _address.text.Trim();

        public int Port => int.TryParse(_port.text, out int port) && port > 0 ? port : _ctx.Content.Config.DefaultPort;

        public string ClassId => _classId;

        public void Show(string status)
        {
            _root.SetActive(true);
            _status.text = status ?? string.Empty;
        }

        public void Hide() => _root.SetActive(false);

        public bool IsVisible => _root.activeSelf;

        public void SetStatus(string status) => _status.text = status ?? string.Empty;

        public void Prefill(string playerName, string classId, string address, int port)
        {
            if (!string.IsNullOrEmpty(playerName))
            {
                _name.text = playerName;
            }
            if (!string.IsNullOrEmpty(address))
            {
                _address.text = address;
            }
            if (port > 0)
            {
                _port.text = port.ToString();
            }
            SelectClass(classId);
        }

        private void ClassRow(Transform parent, ref float y)
        {
            Text label = UiFactory.Label("ClassLabel", parent, _ctx.Font, HudLayout.FontNormal, TextAnchor.MiddleLeft, HudPalette.Text);
            UiFactory.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(HudLayout.Margin, y), new Vector2(LabelWidth, RowHeight));
            label.text = _ctx.Text.Get(ClassKey);
            float x = HudLayout.Margin + LabelWidth;
            float width = (PanelSize.x - x - HudLayout.Margin) / Math.Max(1, _ctx.Content.Classes.Count) - HudLayout.Gap;
            foreach (ClassDefinition cls in _ctx.Content.Classes.Values)
            {
                string classId = cls.Id;
                Button button = UiFactory.Button($"Class_{classId}", parent, _ctx.Font, _ctx.Text.Get(cls.NameKey), () => SelectClass(classId));
                UiFactory.Place((RectTransform)button.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, y), new Vector2(width, RowHeight));
                x += width + HudLayout.Gap;
                _classButtons.Add(button);
                _classIds.Add(classId);
            }
            y -= RowHeight + HudLayout.Gap;
        }

        private void SelectClass(string classId)
        {
            if (classId == null || !_ctx.Content.Classes.ContainsKey(classId))
            {
                return;
            }
            _classId = classId;
            for (int i = 0; i < _classButtons.Count; i++)
            {
                _classButtons[i].targetGraphic.color = _classIds[i] == classId ? HudPalette.Cast : HudPalette.Button;
            }
        }

        private InputField Field(Transform parent, string labelKey, ref float y, int limit, string value)
        {
            Text label = UiFactory.Label(labelKey, parent, _ctx.Font, HudLayout.FontNormal, TextAnchor.MiddleLeft, HudPalette.Text);
            UiFactory.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(HudLayout.Margin, y), new Vector2(LabelWidth, RowHeight));
            label.text = _ctx.Text.Get(labelKey);

            Image background = UiFactory.Panel(labelKey + "_Field", parent, HudPalette.SlotFilled);
            UiFactory.Place(background.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(HudLayout.Margin + LabelWidth, y),
                new Vector2(PanelSize.x - LabelWidth - HudLayout.Margin * 2f, RowHeight));
            Text text = UiFactory.Label("Text", background.transform, _ctx.Font, HudLayout.FontLarge, TextAnchor.MiddleLeft, HudPalette.Text);
            UiFactory.Stretch(text.rectTransform, HudLayout.Gap * 2f);
            text.supportRichText = false;
            InputField field = background.gameObject.AddComponent<InputField>();
            field.targetGraphic = background;
            field.textComponent = text;
            field.characterLimit = limit;
            field.text = value;
            y -= RowHeight + HudLayout.Gap;
            return field;
        }

        private void AddButton(Transform parent, string key, int index, float width, float y, UnityEngine.Events.UnityAction onClick)
        {
            Button button = UiFactory.Button(key, parent, _ctx.Font, _ctx.Text.Get(key), onClick);
            UiFactory.Place((RectTransform)button.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(HudLayout.Margin + index * (width + HudLayout.Gap), y), new Vector2(width, HudLayout.MenuButtonHeight));
        }
    }
}
