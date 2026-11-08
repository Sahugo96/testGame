using System;
using System.Collections.Generic;
using Game.Client.Input;
using Game.Client.Localization;
using Game.Client.Settings;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Windows
{
    /// <summary>
    /// Settings (02, 7.6; module 17) in tabs: Graphics (device: preset, resolution, window mode, VSync, FPS limit, FPS
    /// counter), Controls (account: key rebinding with conflicts, camera sensitivity, invert, zoom), Interface (account:
    /// UI scale, HUD opacity, damage numbers, names, health bars) and Game (auto-pickup, chat tabs, class switch for
    /// testing, leave). Changes are a draft until "Apply"; "Cancel" drops them; "Defaults" resets the tab, "All defaults"
    /// everything. A new resolution or window mode must be confirmed within ResolutionConfirmSeconds or it is reverted.
    /// </summary>
    public sealed class SettingsWindow : HudWindow
    {
        private static readonly Vector2 Size = new Vector2(760f, 660f);
        private const float TabHeight = 34f;
        private const float RowHeight = 34f;
        private const float LabelWidth = 300f;
        private const float ValueWidth = 260f;
        private const float StepWidth = 40f;
        private const float BottomButtonWidth = 170f;
        private const int KeyRows = 7;
        private const float SensitivityStep = 0.1f;
        private const float ScaleStep = 0.05f;

        private enum Tab
        {
            Graphics,
            Controls,
            Interface,
            Game,
        }

        private readonly RectTransform[] _panels = new RectTransform[4];
        private readonly Text[] _tabLabels = new Text[4];
        private readonly List<Action> _refreshers = new List<Action>();
        private readonly Text _notice;
        private readonly Button _keep;
        private readonly Button _revert;
        private readonly Button _replace;
        private readonly Button _swap;
        private readonly Button _keepOld;
        private readonly RectTransform[] _keyRows = new RectTransform[KeyRows];
        private readonly Text[] _keyNames = new Text[KeyRows];
        private readonly Text[] _keyValues = new Text[KeyRows];
        private readonly Widgets.PagerWidget _keyPager;
        private readonly Button _autoLoot;
        private readonly Text _autoLootLabel;
        private Tab _tab;
        private LocalSettings _local;
        private AccountSettingsSnapshot _account;
        private LocalSettings _beforeResolution;
        private float _confirmUntil = -1f;
        private RebindResult _conflict;
        private RebindSlot _waiting;
        private int _keyFirst;
        private bool _autoLootOn;

        public SettingsWindow(HudWindowDto definition, RectTransform parent, Canvas canvas, ClientContext ctx, Tooltip tooltip,
            Action<HudWindow> focus, Action<HudWindow> close) : base(definition, parent, canvas, ctx, tooltip, focus, close, Size)
        {
            float width = Size.x - HudLayout.Margin * 2f;
            float tabWidth = (width - HudLayout.Gap * 3f) / 4f;
            string[] tabKeys = { UiKeys.SettingsTabGraphics, UiKeys.SettingsTabControls, UiKeys.SettingsTabInterface, UiKeys.SettingsTabGame };
            for (int i = 0; i < 4; i++)
            {
                Tab tab = (Tab)i;
                Button button = UiFactory.Button($"Tab_{tab}", Content, ctx.Font, ctx.Text.Get(tabKeys[i]), () => SelectTab(tab));
                UiFactory.Place((RectTransform)button.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(i * (tabWidth + HudLayout.Gap), 0f), new Vector2(tabWidth, TabHeight));
                _tabLabels[i] = button.GetComponentInChildren<Text>();
                RectTransform panel = UiFactory.Rect($"Panel_{tab}", Content);
                panel.anchorMin = Vector2.zero;
                panel.anchorMax = Vector2.one;
                panel.offsetMin = new Vector2(0f, (HudLayout.MenuButtonHeight + HudLayout.Gap) * 2f + HudLayout.Gap);
                panel.offsetMax = new Vector2(0f, -TabHeight - HudLayout.Gap * 2f);
                _panels[i] = panel;
            }

            BuildGraphics(_panels[(int)Tab.Graphics], width);
            BuildControls(_panels[(int)Tab.Controls], width, out _keyPager);
            BuildInterface(_panels[(int)Tab.Interface], width);
            BuildGame(_panels[(int)Tab.Game], width, out _autoLoot, out _autoLootLabel);

            // Notices (resolution confirm, key conflict) above the bottom buttons.
            float noticeY = HudLayout.MenuButtonHeight + HudLayout.Gap;
            _notice = UiFactory.Label("Notice", Content, ctx.Font, HudLayout.FontSmall, TextAnchor.MiddleLeft, HudPalette.Warning);
            UiFactory.Place(_notice.rectTransform, Vector2.zero, Vector2.zero, new Vector2(0f, noticeY), new Vector2(width - 3f * (BottomButtonWidth * 0.7f + HudLayout.Gap), HudLayout.MenuButtonHeight));
            _keep = NoticeButton(UiKeys.SettingsKeep, 1, noticeY, KeepResolution);
            _revert = NoticeButton(UiKeys.SettingsRevert, 0, noticeY, RevertResolution);
            _replace = NoticeButton(UiKeys.SettingsReplace, 2, noticeY, () => Resolve(c => Ctx.Settings.Keys.Replace(c)));
            _swap = NoticeButton(UiKeys.SettingsSwap, 1, noticeY, () => Resolve(c => Ctx.Settings.Keys.Swap(c)));
            _keepOld = NoticeButton(UiKeys.SettingsCancel, 0, noticeY, () => Resolve(c => Ctx.Settings.Keys.Revert(c)));

            BottomButton(UiKeys.SettingsApply, 0, Apply);
            BottomButton(UiKeys.SettingsCancel, 1, CancelChanges);
            BottomButton(UiKeys.SettingsDefaults, 2, () => Defaults(_tab));
            BottomButton(UiKeys.SettingsAllDefaults, 3, () =>
            {
                foreach (Tab tab in Enum.GetValues(typeof(Tab)))
                {
                    Defaults(tab);
                }
            });
            if (ctx.Settings != null)
            {
                ctx.Settings.Keys.KeysChanged += RefreshAll;
            }
            SelectTab(Tab.Graphics);
            ShowNotice();
        }

        public override void Refresh(PlayerStateSnapshot state)
        {
            _autoLootOn = state.AutoLoot;
            _autoLootLabel.text = Ctx.Text.Get(_autoLootOn ? UiKeys.AutoLootOn : UiKeys.AutoLootOff);
            if (_confirmUntil > 0f)
            {
                if (Time.unscaledTime >= _confirmUntil)
                {
                    RevertResolution();
                }
                else
                {
                    ShowNotice();
                }
            }
        }

        protected override void OnVisibilityChanged(bool open)
        {
            if (open && Ctx.Settings != null)
            {
                LoadDrafts();
            }
            else if (!open)
            {
                Ctx.Settings?.Keys.CancelRebind();
                _waiting = null;
            }
        }

        private void LoadDrafts()
        {
            _local = (Ctx.Settings.Local ?? Ctx.Settings.DefaultLocal()).Copy();
            _account = SettingsService.Copy(Ctx.Settings.Account);
            RefreshAll();
        }

        // ---- Graphics (device) ----

        private void BuildGraphics(RectTransform panel, float width)
        {
            float y = 0f;
            CycleRow(panel, ref y, UiKeys.SettingsPreset, () =>
            {
                GraphicsPresetDto[] presets = Ctx.Content.Config.GraphicsPresets;
                int index = Array.FindIndex(presets, p => p.Id == _local.PresetId);
                _local.PresetId = presets[(index + 1) % presets.Length].Id;
            }, () => Ctx.Text.Get(GraphicsSettingsApplier.Preset(Ctx.Content.Config, _local.PresetId)?.NameKey));
            CycleRow(panel, ref y, UiKeys.SettingsResolution, () =>
            {
                List<Vector2Int> sizes = GraphicsSettingsApplier.Resolutions();
                int index = sizes.FindIndex(s => s.x == _local.Width && s.y == _local.Height);
                Vector2Int next = sizes[(index + 1) % sizes.Count];
                _local.Width = next.x;
                _local.Height = next.y;
            }, () => Ctx.Text.Format(UiKeys.SettingsResolutionValue, _local.Width, _local.Height));
            CycleRow(panel, ref y, UiKeys.SettingsWindowMode, () =>
            {
                FullScreenMode[] modes = GraphicsSettingsApplier.WindowModes;
                int index = Array.IndexOf(modes, GraphicsSettingsApplier.ModeOf(_local));
                _local.WindowMode = modes[(index + 1) % modes.Length].ToString();
            }, () => Ctx.Text.Get(UiKeys.SettingsWindowModePrefix + GraphicsSettingsApplier.ModeOf(_local)));
            CycleRow(panel, ref y, UiKeys.SettingsVSync, () => _local.VSync = !_local.VSync, () => OnOff(_local.VSync));
            CycleRow(panel, ref y, UiKeys.SettingsFpsLimit, () =>
            {
                int[] limits = Ctx.Content.Config.FpsLimits;
                int index = Array.IndexOf(limits, _local.FpsLimit);
                _local.FpsLimit = limits[(index + 1) % limits.Length];
            }, () => _local.FpsLimit > 0 ? _local.FpsLimit.ToString() : Ctx.Text.Get(UiKeys.SettingsNoLimit));
            CycleRow(panel, ref y, UiKeys.SettingsShowFps, () => _local.ShowFps = !_local.ShowFps, () => OnOff(_local.ShowFps));
            Hint(panel, ref y, width, UiKeys.SettingsGraphicsHint);
        }

        // ---- Controls (account) ----

        private void BuildControls(RectTransform panel, float width, out Widgets.PagerWidget pager)
        {
            float y = 0f;
            StepRow(panel, ref y, UiKeys.SettingsSensitivity, -SensitivityStep, SensitivityStep, delta =>
            {
                var d = Ctx.Content.Config.AccountSettings;
                _account.CameraSensitivity = Mathf.Clamp(_account.CameraSensitivity + delta, d.CameraSensitivityMin, d.CameraSensitivityMax);
            }, () => _account.CameraSensitivity.ToString("0.0"));
            CycleRow(panel, ref y, UiKeys.SettingsInvertY, () => _account.InvertY = !_account.InvertY, () => OnOff(_account.InvertY));
            StepRow(panel, ref y, UiKeys.SettingsZoomSpeed, -SensitivityStep, SensitivityStep, delta =>
            {
                var d = Ctx.Content.Config.AccountSettings;
                _account.ZoomSpeed = Mathf.Clamp(_account.ZoomSpeed + delta, d.ZoomSpeedMin, d.ZoomSpeedMax);
            }, () => _account.ZoomSpeed.ToString("0.0"));
            Hint(panel, ref y, width, UiKeys.SettingsKeysHint);
            for (int i = 0; i < KeyRows; i++)
            {
                int index = i;
                RectTransform row = MarketUi.Row($"Key_{i}", panel, y - i * (RowHeight + HudLayout.Gap), width, RowHeight);
                _keyNames[i] = MarketUi.Cell(Ctx, row, 0f, LabelWidth);
                _keyNames[i].fontSize = HudLayout.FontNormal;
                Button button = UiFactory.Button("Key", row, Ctx.Font, string.Empty, () => StartRebind(index));
                UiFactory.Place((RectTransform)button.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(LabelWidth + HudLayout.Gap, 0f), new Vector2(ValueWidth, RowHeight));
                _keyValues[i] = button.GetComponentInChildren<Text>();
                _keyRows[i] = row;
            }
            pager = new Widgets.PagerWidget(panel, Ctx, new Vector2(1f, 0f), new Vector2(0f, HudLayout.MenuButtonHeight * 0.5f), RefreshAll);
            _refreshers.Add(RefreshKeys);
        }

        private void RefreshKeys()
        {
            if (Ctx.Settings == null)
            {
                return;
            }
            IReadOnlyList<RebindSlot> slots = Ctx.Settings.Keys.Slots;
            _keyFirst = _keyPager.Apply(slots.Count, KeyRows);
            for (int i = 0; i < KeyRows; i++)
            {
                int index = _keyFirst + i;
                bool shown = index < slots.Count;
                _keyRows[i].gameObject.SetActive(shown);
                if (!shown)
                {
                    continue;
                }
                RebindSlot slot = slots[index];
                _keyNames[i].text = SlotName(slot);
                string key = KeyRebinder.Display(slot);
                _keyValues[i].text = slot == _waiting ? Ctx.Text.Get(UiKeys.SettingsPressKey) : string.IsNullOrEmpty(key) ? Ctx.Text.Get(UiKeys.SettingsUnbound) : key;
                _keyValues[i].color = slot == _waiting ? HudPalette.Cast : string.IsNullOrEmpty(key) ? HudPalette.Warning : HudPalette.Text;
            }
        }

        private string SlotName(RebindSlot slot)
        {
            if (slot.LabelKey == KeyLabels.Window)
            {
                HudWindowDto window = Ctx.Content.HudWindows.Find(w => w.Id == slot.LabelArgument);
                return Ctx.Text.Format(KeyLabels.Window, window != null ? Ctx.Text.Get(window.NameKey) : slot.LabelArgument);
            }
            return slot.LabelArgument != null ? Ctx.Text.Format(slot.LabelKey, slot.LabelArgument) : Ctx.Text.Get(slot.LabelKey);
        }

        private void StartRebind(int row)
        {
            IReadOnlyList<RebindSlot> slots = Ctx.Settings?.Keys.Slots;
            int index = _keyFirst + row;
            if (slots == null || index >= slots.Count || _conflict != null || Ctx.Settings.Keys.IsRebinding)
            {
                return;
            }
            _waiting = slots[index];
            RefreshKeys();
            Ctx.Settings.Keys.Rebind(_waiting, result =>
            {
                _waiting = null;
                _conflict = result.IsConflict ? result : null;
                RefreshKeys();
                ShowNotice();
            });
        }

        private void Resolve(Action<RebindResult> how)
        {
            if (_conflict != null)
            {
                how(_conflict);
                _conflict = null;
                ShowNotice();
            }
        }

        // ---- Interface (account) ----

        private void BuildInterface(RectTransform panel, float width)
        {
            float y = 0f;
            StepRow(panel, ref y, UiKeys.SettingsUiScale, -ScaleStep, ScaleStep, delta =>
            {
                var d = Ctx.Content.Config.AccountSettings;
                _account.UiScale = Mathf.Clamp(_account.UiScale + delta, d.UiScaleMin, d.UiScaleMax);
            }, () => Ctx.Text.Format(UiKeys.SettingsPercent, Mathf.RoundToInt(_account.UiScale * 100f)));
            StepRow(panel, ref y, UiKeys.SettingsOpacity, -ScaleStep, ScaleStep, delta =>
            {
                var d = Ctx.Content.Config.AccountSettings;
                _account.PanelOpacity = Mathf.Clamp(_account.PanelOpacity + delta, d.PanelOpacityMin, 1f);
            }, () => Ctx.Text.Format(UiKeys.SettingsPercent, Mathf.RoundToInt(_account.PanelOpacity * 100f)));
            CycleRow(panel, ref y, UiKeys.SettingsDamageNumbers, () => _account.DamageNumbers = !_account.DamageNumbers, () => OnOff(_account.DamageNumbers));
            CycleRow(panel, ref y, UiKeys.SettingsNameplates, () => _account.Nameplates = !_account.Nameplates, () => OnOff(_account.Nameplates));
            CycleRow(panel, ref y, UiKeys.SettingsNameplateHealth, () => _account.NameplateHealth = !_account.NameplateHealth, () => OnOff(_account.NameplateHealth));
            Hint(panel, ref y, width, UiKeys.SettingsAccountHint);
        }

        // ---- Game (account + tools) ----

        private void BuildGame(RectTransform panel, float width, out Button autoLoot, out Text autoLootLabel)
        {
            float y = 0f;
            Text info = MarketUi.Cell(Ctx, MarketUi.Row("Info", panel, y, width, RowHeight), 0f, width);
            info.text = Ctx.Text.Get(UiKeys.SettingsInfo);
            info.color = HudPalette.TextDim;
            y -= RowHeight + HudLayout.Gap;
            if (Ctx.Languages != null && Ctx.Languages.Codes.Count > 1)
            {
                CycleRow(panel, ref y, UiKeys.SettingsLanguage, NextLanguage, () => Ctx.Languages.NameOf(LanguageShown()));
                Hint(panel, ref y, width, UiKeys.SettingsLanguageHint);
            }
            autoLoot = PanelButton(panel, ref y, string.Empty, () => Ctx.Commands?.SetAutoLoot(!_autoLootOn));
            autoLootLabel = autoLoot.GetComponentInChildren<Text>();
            PanelButton(panel, ref y, Ctx.Text.Get(UiKeys.ChatTabsSettingsLong), () => Ctx.RequestWindow(WindowFactory.ChatTabs));
            y -= HudLayout.Gap * 2f;
            Text header = MarketUi.Cell(Ctx, MarketUi.Row("SwitchHeader", panel, y, width, RowHeight), 0f, width);
            header.text = Ctx.Text.Get(UiKeys.SwitchClass);
            header.color = HudPalette.Cast;
            y -= RowHeight + HudLayout.Gap;
            foreach (ClassDefinition cls in Ctx.Content.Classes.Values)
            {
                string classId = cls.Id;
                PanelButton(panel, ref y, Ctx.Text.Get(cls.NameKey), () => Ctx.RequestClassSwitch(classId));
            }
            y -= HudLayout.Gap * 2f;
            PanelButton(panel, ref y, Ctx.Text.Get(UiKeys.LeaveGame), () => Ctx.RequestLeave());
        }

        private string LanguageShown() => string.IsNullOrEmpty(_local.Language) ? Ctx.Languages.Current : _local.Language;

        private void NextLanguage()
        {
            var codes = new System.Collections.Generic.List<string>(Ctx.Languages.Codes);
            int index = codes.IndexOf(LanguageShown());
            _local.Language = codes[(index + 1) % codes.Count];
        }

        // ---- Apply / cancel / defaults ----

        private void Apply()
        {
            if (Ctx.Settings == null)
            {
                return;
            }
            LocalSettings applied = Ctx.Settings.Local;
            bool screenChanged = applied == null || applied.Width != _local.Width || applied.Height != _local.Height || applied.WindowMode != _local.WindowMode;
            if (screenChanged)
            {
                _beforeResolution = applied?.Copy();
                _confirmUntil = Time.unscaledTime + Ctx.Content.Config.ResolutionConfirmSeconds;
                Ctx.Settings.ApplyLocal(_local, false);
            }
            else
            {
                Ctx.Settings.ApplyLocal(_local, true);
            }
            Ctx.Settings.SaveAccount(SettingsService.Copy(_account));
            ShowNotice();
        }

        private void KeepResolution()
        {
            if (_confirmUntil > 0f)
            {
                _confirmUntil = -1f;
                Ctx.Settings.ApplyLocal(Ctx.Settings.Local, true);
                ShowNotice();
            }
        }

        private void RevertResolution()
        {
            if (_confirmUntil <= 0f)
            {
                return;
            }
            _confirmUntil = -1f;
            if (_beforeResolution != null)
            {
                Ctx.Settings.ApplyLocal(_beforeResolution, true);
                _local = _beforeResolution.Copy();
            }
            RefreshAll();
            ShowNotice();
        }

        private void CancelChanges()
        {
            if (Ctx.Settings == null)
            {
                return;
            }
            Ctx.Settings.Keys.CancelRebind();
            Ctx.Settings.Keys.Apply(Ctx.Settings.Account.Bindings);
            _conflict = null;
            LoadDrafts();
            ShowNotice();
        }

        private void Defaults(Tab tab)
        {
            if (Ctx.Settings == null)
            {
                return;
            }
            AccountSettingsSnapshot d = Ctx.Settings.DefaultAccount();
            switch (tab)
            {
                case Tab.Graphics:
                    _local = Ctx.Settings.DefaultLocal();
                    break;
                case Tab.Controls:
                    _account.CameraSensitivity = d.CameraSensitivity;
                    _account.InvertY = d.InvertY;
                    _account.ZoomSpeed = d.ZoomSpeed;
                    Ctx.Settings.Keys.Apply(null);
                    break;
                case Tab.Interface:
                    _account.UiScale = d.UiScale;
                    _account.PanelOpacity = d.PanelOpacity;
                    _account.DamageNumbers = d.DamageNumbers;
                    _account.Nameplates = d.Nameplates;
                    _account.NameplateHealth = d.NameplateHealth;
                    break;
            }
            RefreshAll();
        }

        private void ShowNotice()
        {
            bool confirming = _confirmUntil > 0f;
            bool conflict = !confirming && _conflict != null;
            _keep.gameObject.SetActive(confirming);
            _revert.gameObject.SetActive(confirming);
            _replace.gameObject.SetActive(conflict);
            _swap.gameObject.SetActive(conflict);
            _keepOld.gameObject.SetActive(conflict);
            if (confirming)
            {
                _notice.text = Ctx.Text.Format(UiKeys.SettingsConfirmResolution, Mathf.CeilToInt(_confirmUntil - Time.unscaledTime));
            }
            else if (conflict)
            {
                _notice.text = Ctx.Text.Format(UiKeys.SettingsConflict, KeyRebinder.Display(_conflict.Slot), SlotName(_conflict.Other));
            }
            else
            {
                _notice.text = string.Empty;
            }
        }

        private void SelectTab(Tab tab)
        {
            _tab = tab;
            for (int i = 0; i < _panels.Length; i++)
            {
                _panels[i].gameObject.SetActive(i == (int)tab);
                _tabLabels[i].color = i == (int)tab ? HudPalette.Cast : HudPalette.Text;
            }
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (_local == null || _account == null)
            {
                return;
            }
            foreach (Action refresh in _refreshers)
            {
                refresh();
            }
        }

        private string OnOff(bool value) => Ctx.Text.Get(value ? UiKeys.SettingsOn : UiKeys.SettingsOff);

        // ---- Row builders ----

        /// <summary>Label on the left, a button showing the value that cycles or toggles it.</summary>
        private void CycleRow(RectTransform panel, ref float y, string labelKey, Action change, Func<string> value)
        {
            RectTransform row = LabelRow(panel, ref y, labelKey);
            Button button = UiFactory.Button("Value", row, Ctx.Font, string.Empty, () => { change(); RefreshAll(); });
            UiFactory.Place((RectTransform)button.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(LabelWidth + HudLayout.Gap, 0f), new Vector2(ValueWidth, RowHeight));
            Text text = button.GetComponentInChildren<Text>();
            _refreshers.Add(() => text.text = value());
        }

        /// <summary>Label, "−", value, "+".</summary>
        private void StepRow(RectTransform panel, ref float y, string labelKey, float down, float up, Action<float> change, Func<string> value)
        {
            RectTransform row = LabelRow(panel, ref y, labelKey);
            Button minus = UiFactory.Button("Minus", row, Ctx.Font, Ctx.Text.Get(UiKeys.SettingsMinus), () => { change(down); RefreshAll(); });
            UiFactory.Place((RectTransform)minus.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(LabelWidth + HudLayout.Gap, 0f), new Vector2(StepWidth, RowHeight));
            Text text = MarketUi.Cell(Ctx, row, LabelWidth + HudLayout.Gap * 2f + StepWidth, ValueWidth - StepWidth * 2f - HudLayout.Gap * 2f, TextAnchor.MiddleCenter);
            text.fontSize = HudLayout.FontNormal;
            Button plus = UiFactory.Button("Plus", row, Ctx.Font, Ctx.Text.Get(UiKeys.SettingsPlus), () => { change(up); RefreshAll(); });
            UiFactory.Place((RectTransform)plus.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(LabelWidth + HudLayout.Gap + ValueWidth - StepWidth, 0f), new Vector2(StepWidth, RowHeight));
            _refreshers.Add(() => text.text = value());
        }

        private RectTransform LabelRow(RectTransform panel, ref float y, string labelKey)
        {
            RectTransform row = MarketUi.Row(labelKey, panel, y, LabelWidth + HudLayout.Gap + ValueWidth, RowHeight);
            Text label = MarketUi.Cell(Ctx, row, 0f, LabelWidth);
            label.fontSize = HudLayout.FontNormal;
            label.text = Ctx.Text.Get(labelKey);
            y -= RowHeight + HudLayout.Gap;
            return row;
        }

        private void Hint(RectTransform panel, ref float y, float width, string key)
        {
            Text hint = MarketUi.Cell(Ctx, MarketUi.Row("Hint", panel, y, width, RowHeight), 0f, width);
            hint.text = Ctx.Text.Get(key);
            hint.color = HudPalette.TextDim;
            y -= RowHeight + HudLayout.Gap;
        }

        private Button PanelButton(RectTransform panel, ref float y, string caption, UnityEngine.Events.UnityAction onClick)
        {
            Button button = UiFactory.Button("Button", panel, Ctx.Font, caption, onClick);
            UiFactory.Place((RectTransform)button.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(LabelWidth, HudLayout.MenuButtonHeight));
            y -= HudLayout.MenuButtonHeight + HudLayout.Gap;
            return button;
        }

        private void BottomButton(string key, int column, UnityEngine.Events.UnityAction onClick)
        {
            Button button = UiFactory.Button(key, Content, Ctx.Font, Ctx.Text.Get(key), onClick);
            UiFactory.Place((RectTransform)button.transform, Vector2.zero, Vector2.zero, new Vector2(column * (BottomButtonWidth + HudLayout.Gap), 0f), new Vector2(BottomButtonWidth, HudLayout.MenuButtonHeight));
        }

        /// <summary>Small buttons at the right of the notice line; slot 0 = rightmost.</summary>
        private Button NoticeButton(string key, int slot, float y, UnityEngine.Events.UnityAction onClick)
        {
            float width = BottomButtonWidth * 0.7f;
            Button button = UiFactory.Button(key, Content, Ctx.Font, Ctx.Text.Get(key), onClick);
            UiFactory.Place((RectTransform)button.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-slot * (width + HudLayout.Gap), y), new Vector2(width, HudLayout.MenuButtonHeight));
            button.GetComponentInChildren<Text>().fontSize = HudLayout.FontSmall;
            return button;
        }
    }
}
