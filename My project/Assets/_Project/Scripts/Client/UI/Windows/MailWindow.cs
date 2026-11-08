using System;
using System.Collections.Generic;
using System.Text;
using Game.Client.Localization;
using Game.Client.UI.Widgets;
using Game.Shared.Content.Dto;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.UI.Windows
{
    /// <summary>
    /// Mail (05, 7.14.3), opened at a mailbox: letters on the left (unread marked), the selected letter on the right with
    /// its attachments, "Claim" (everything at once) and "Delete"; "New letter" writes one: recipient, subject, text, bag
    /// items (click to attach / detach) and gold, the fee per attachment shown. The server keeps and checks everything.
    /// </summary>
    public sealed class MailWindow : HudWindow
    {
        private static readonly Vector2 Size = new Vector2(800f, 640f);
        private const float ListWidth = 300f;
        private const float RowHeight = 32f;
        private const int ListRows = 12;
        private const float SlotSize = 52f;
        private const int BagSlots = 8;
        private const int NameLimit = 32;
        private const int NumberLimit = 10;

        private readonly RectTransform[] _rows = new RectTransform[ListRows];
        private readonly Text[] _rowLabels = new Text[ListRows];
        private readonly PagerWidget _pager;
        private readonly RectTransform _readPanel;
        private readonly Text _letter;
        private readonly SlotView[] _attachments;
        private readonly Button _claim;
        private readonly Button _delete;
        private readonly RectTransform _writePanel;
        private readonly InputField _to;
        private readonly InputField _subject;
        private readonly InputField _body;
        private readonly InputField _gold;
        private readonly Text _fee;
        private readonly SlotView[] _chosenViews;
        private readonly SlotView[] _bagViews = new SlotView[BagSlots];
        private readonly PagerWidget _bagPager;
        private readonly List<ItemSlotSnapshot> _bagItems = new List<ItemSlotSnapshot>();
        private readonly List<ItemSlotSnapshot> _chosen = new List<ItemSlotSnapshot>();
        private readonly StringBuilder _sb = new StringBuilder();
        private MailSnapshot[] _mail = new MailSnapshot[0];
        private long _selected = -1;
        private int _first;
        private int _bagFirst;
        private int _seenVersion = -1;

        public MailWindow(HudWindowDto definition, RectTransform parent, Canvas canvas, ClientContext ctx, Tooltip tooltip,
            Action<HudWindow> focus, Action<HudWindow> close) : base(definition, parent, canvas, ctx, tooltip, focus, close, Size)
        {
            float h = HudLayout.MenuButtonHeight;
            float gap = HudLayout.Gap;
            Button write = UiFactory.Button("Write", Content, ctx.Font, ctx.Text.Get(UiKeys.MailWrite), StartWriting);
            UiFactory.Place((RectTransform)write.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(ListWidth, h));
            for (int i = 0; i < ListRows; i++)
            {
                int index = i;
                Button row = UiFactory.Button($"Letter_{i}", Content, ctx.Font, string.Empty, () => Select(index));
                _rows[i] = UiFactory.Place((RectTransform)row.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -h - gap * 2f - i * (RowHeight + gap)), new Vector2(ListWidth, RowHeight));
                _rowLabels[i] = row.GetComponentInChildren<Text>();
                _rowLabels[i].fontSize = HudLayout.FontSmall;
                _rowLabels[i].alignment = TextAnchor.MiddleLeft;
            }
            _pager = new PagerWidget(Content, ctx, new Vector2(0f, 0f), new Vector2(ListWidth * 0.5f - 88f, h * 0.5f), ShowList);

            float x = ListWidth + HudLayout.Margin;
            float width = Size.x - HudLayout.Margin * 3f - ListWidth;
            int maxAttachments = ctx.Content.Config.MailMaxAttachments;

            _readPanel = Panel("Read", x, width);
            _letter = UiFactory.Label("Letter", _readPanel, ctx.Font, HudLayout.FontNormal, TextAnchor.UpperLeft, HudPalette.Text);
            _letter.supportRichText = false;
            UiFactory.Stretch(_letter.rectTransform);
            _letter.rectTransform.offsetMin = new Vector2(0f, SlotSize + h + gap * 3f);
            _attachments = new SlotView[maxAttachments];
            for (int i = 0; i < maxAttachments; i++)
            {
                int index = i;
                _attachments[i] = new SlotView($"Attachment_{i}", _readPanel, ctx.Font, SlotSize);
                UiFactory.Place(_attachments[i].Rect, Vector2.zero, Vector2.zero, new Vector2(i * (SlotSize + gap), h + gap * 2f), Vector2.one * SlotSize);
                _attachments[i].Bind(() => ShowAttachment(index), Tooltip.Hide, null);
            }
            _claim = AddButton(_readPanel, UiKeys.MailClaim, 0f, () => { if (_selected >= 0) Ctx.Commands?.ClaimMail(_selected); });
            _delete = AddButton(_readPanel, UiKeys.MailDelete, 160f, () => { if (_selected >= 0) { Ctx.Commands?.DeleteMail(_selected); _selected = -1; } });

            _writePanel = Panel("Write", x, width);
            float y = 0f;
            _to = Input(_writePanel, ref y, width, NameLimit, UiKeys.MailTo);
            _subject = Input(_writePanel, ref y, width, ctx.Content.Config.MailSubjectMax, UiKeys.MailSubject);
            _body = MarketUi.Field(ctx, _writePanel, new Vector2(0f, 1f), new Vector2(0f, y), width, h * 3f, ctx.Content.Config.MailBodyMax, UiKeys.MailBody, false);
            _body.lineType = InputField.LineType.MultiLineNewline;
            y -= h * 3f + gap;
            _gold = Input(_writePanel, ref y, width * 0.5f, NumberLimit, UiKeys.MailGold);
            _gold.contentType = InputField.ContentType.IntegerNumber;
            _fee = MarketUi.Cell(ctx, MarketUi.Row("Fee", _writePanel, y, width, HudLayout.RowHeight), 0f, width);
            _fee.color = HudPalette.TextDim;
            y -= HudLayout.RowHeight + gap;
            _chosenViews = new SlotView[maxAttachments];
            for (int i = 0; i < maxAttachments; i++)
            {
                int index = i;
                _chosenViews[i] = new SlotView($"Chosen_{i}", _writePanel, ctx.Font, SlotSize);
                UiFactory.Place(_chosenViews[i].Rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(i * (SlotSize + gap), y), Vector2.one * SlotSize);
                _chosenViews[i].Bind(() => { if (index < _chosen.Count) MarketUi.ShowTooltip(Ctx, Tooltip, _chosen[index], UiKeys.MailClickToDetach); }, Tooltip.Hide,
                    button => { if (index < _chosen.Count) { _chosen.RemoveAt(index); Tooltip.Hide(); ShowWriting(); } });
            }
            y -= SlotSize + gap * 2f;
            Text bagHint = MarketUi.Cell(ctx, MarketUi.Row("BagHint", _writePanel, y, width, HudLayout.RowHeight), 0f, width);
            bagHint.text = ctx.Text.Get(UiKeys.MailBagHint);
            bagHint.color = HudPalette.TextDim;
            y -= HudLayout.RowHeight + gap;
            for (int i = 0; i < BagSlots; i++)
            {
                int index = i;
                _bagViews[i] = new SlotView($"Bag_{i}", _writePanel, ctx.Font, SlotSize);
                UiFactory.Place(_bagViews[i].Rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(i * (SlotSize + gap), y), Vector2.one * SlotSize);
                _bagViews[i].Bind(() => { if (index + _bagFirst < _bagItems.Count) MarketUi.ShowTooltip(Ctx, Tooltip, _bagItems[index + _bagFirst], UiKeys.MailClickToAttach); }, Tooltip.Hide,
                    button => Attach(index, button));
            }
            _bagPager = new PagerWidget(_writePanel, ctx, new Vector2(1f, 0f), new Vector2(0f, h + gap), ShowWriting);
            AddButton(_writePanel, UiKeys.MailSend, 0f, Send);
            AddButton(_writePanel, UiKeys.MailCancel, 160f, () => { _writePanel.gameObject.SetActive(false); _readPanel.gameObject.SetActive(true); });
            _writePanel.gameObject.SetActive(false);
            ctx.State.MarketData += OnData;
        }

        public override void Refresh(PlayerStateSnapshot state)
        {
            if (!IsNearActiveStation(state))
            {
                RequestClose();
                return;
            }
            if (state.Market != null && state.Market.MailVersion != _seenVersion)
            {
                _seenVersion = state.Market.MailVersion;
                Ctx.Commands?.ViewMailbox();
            }
            if (_writePanel.gameObject.activeSelf)
            {
                ShowWriting();
            }
        }

        protected override void OnVisibilityChanged(bool open)
        {
            if (open)
            {
                _seenVersion = -1;
            }
        }

        private void OnData()
        {
            if (IsOpen && Ctx.State.LastMailbox != null)
            {
                _mail = Ctx.State.LastMailbox.Mail ?? new MailSnapshot[0];
                ShowList();
                ShowLetter();
            }
        }

        private void ShowList()
        {
            _first = _pager.Apply(_mail.Length, ListRows);
            for (int i = 0; i < ListRows; i++)
            {
                int index = _first + i;
                bool shown = index < _mail.Length;
                _rows[i].gameObject.SetActive(shown);
                if (!shown)
                {
                    continue;
                }
                MailSnapshot mail = _mail[index];
                string from = string.IsNullOrEmpty(mail.From) ? Ctx.Text.Get(UiKeys.MailFromSystem) : mail.From;
                _rowLabels[i].text = Ctx.Text.Format(mail.Read ? UiKeys.MailRowRead : UiKeys.MailRowUnread, MarketUi.Subject(Ctx, mail), from);
                _rowLabels[i].color = mail.Id == _selected ? HudPalette.Cast : mail.Read ? HudPalette.TextDim : HudPalette.Text;
            }
        }

        private void Select(int row)
        {
            int index = _first + row;
            if (index >= _mail.Length)
            {
                return;
            }
            _selected = _mail[index].Id;
            if (!_mail[index].Read)
            {
                Ctx.Commands?.ReadMail(_selected);
            }
            _writePanel.gameObject.SetActive(false);
            _readPanel.gameObject.SetActive(true);
            ShowList();
            ShowLetter();
        }

        private bool TryGetSelected(out MailSnapshot mail)
        {
            foreach (MailSnapshot candidate in _mail)
            {
                if (candidate.Id == _selected)
                {
                    mail = candidate;
                    return true;
                }
            }
            mail = default;
            return false;
        }

        private void ShowLetter()
        {
            bool has = TryGetSelected(out MailSnapshot mail);
            ItemSlotSnapshot[] items = has ? mail.Items ?? new ItemSlotSnapshot[0] : new ItemSlotSnapshot[0];
            _sb.Clear();
            if (has)
            {
                _sb.Append(MarketUi.Subject(Ctx, mail)).Append('\n');
                _sb.Append(Ctx.Text.Format(UiKeys.MailFromLine, string.IsNullOrEmpty(mail.From) ? Ctx.Text.Get(UiKeys.MailFromSystem) : mail.From)).Append('\n');
                _sb.Append(Ctx.Text.Format(UiKeys.MailExpiresLine, MarketUi.TimeLeft(Ctx, mail.SecondsLeft))).Append("\n\n");
                if (!string.IsNullOrEmpty(mail.Body))
                {
                    _sb.Append(mail.Body).Append("\n\n");
                }
                if (mail.Gold > 0)
                {
                    _sb.Append(Ctx.Text.Format(UiKeys.MailGoldLine, mail.Gold)).Append('\n');
                }
            }
            else
            {
                _sb.Append(Ctx.Text.Get(_mail.Length == 0 ? UiKeys.MailEmpty : UiKeys.MailPick));
            }
            _letter.text = _sb.ToString();
            for (int i = 0; i < _attachments.Length; i++)
            {
                bool filled = i < items.Length;
                _attachments[i].Rect.gameObject.SetActive(filled);
                if (filled)
                {
                    _attachments[i].SetFilled(true);
                    _attachments[i].SetCaption(MarketUi.ItemLine(Ctx, items[i]), MarketUi.ItemColor(Ctx, items[i]));
                    _attachments[i].SetCount(items[i].Count > 1 ? items[i].Count.ToString() : string.Empty);
                }
            }
            bool attached = has && (items.Length > 0 || mail.Gold > 0);
            _claim.gameObject.SetActive(attached);
            _delete.gameObject.SetActive(has && !attached);
        }

        private void ShowAttachment(int index)
        {
            if (TryGetSelected(out MailSnapshot mail) && mail.Items != null && index < mail.Items.Length)
            {
                MarketUi.ShowTooltip(Ctx, Tooltip, mail.Items[index], null);
            }
        }

        private void StartWriting()
        {
            _chosen.Clear();
            _to.text = string.Empty;
            _subject.text = string.Empty;
            _body.text = string.Empty;
            _gold.text = string.Empty;
            _readPanel.gameObject.SetActive(false);
            _writePanel.gameObject.SetActive(true);
            ShowWriting();
        }

        private void ShowWriting()
        {
            PlayerStateSnapshot state = Ctx.State.Player;
            _bagItems.Clear();
            foreach (ItemSlotSnapshot slot in state?.Bag ?? new ItemSlotSnapshot[0])
            {
                if (slot.DefinitionId != null && !string.IsNullOrEmpty(slot.InstanceId) && !_chosen.Exists(c => c.InstanceId == slot.InstanceId))
                {
                    _bagItems.Add(slot);
                }
            }
            _bagFirst = _bagPager.Apply(_bagItems.Count, BagSlots);
            Fill(_bagViews, _bagItems, _bagFirst);
            Fill(_chosenViews, _chosen, 0);
            _fee.text = Ctx.Text.Format(UiKeys.MailFeeLine, Ctx.Content.Config.MailFeePerAttachment * _chosen.Count);
        }

        private void Fill(SlotView[] views, List<ItemSlotSnapshot> items, int first)
        {
            for (int i = 0; i < views.Length; i++)
            {
                int index = first + i;
                bool filled = index < items.Count;
                views[i].SetFilled(filled);
                views[i].SetCaption(filled ? MarketUi.ItemLine(Ctx, items[index]) : string.Empty, filled ? MarketUi.ItemColor(Ctx, items[index]) : HudPalette.Text);
                views[i].SetCount(filled && items[index].Count > 1 ? items[index].Count.ToString() : string.Empty);
            }
        }

        private void Attach(int slot, PointerEventData.InputButton button)
        {
            int index = _bagFirst + slot;
            if (button != PointerEventData.InputButton.Left || index >= _bagItems.Count || _chosen.Count >= Ctx.Content.Config.MailMaxAttachments)
            {
                return;
            }
            _chosen.Add(_bagItems[index]);
            Tooltip.Hide();
            ShowWriting();
        }

        private void Send()
        {
            if (Ctx.Commands == null)
            {
                return;
            }
            var ids = new string[_chosen.Count];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = _chosen[i].InstanceId;
            }
            Ctx.Commands.SendMail(_to.text, _subject.text, _body.text, ids, MarketUi.ReadLong(_gold, 0));
            _writePanel.gameObject.SetActive(false);
            _readPanel.gameObject.SetActive(true);
        }

        private RectTransform Panel(string name, float x, float width)
        {
            RectTransform panel = UiFactory.Rect(name, Content);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 0.5f);
            panel.offsetMin = new Vector2(x, 0f);
            panel.offsetMax = new Vector2(x + width, 0f);
            return panel;
        }

        private InputField Input(RectTransform parent, ref float y, float width, int limit, string placeholderKey)
        {
            InputField field = MarketUi.Field(Ctx, parent, new Vector2(0f, 1f), new Vector2(0f, y), width, HudLayout.MenuButtonHeight, limit, placeholderKey, false);
            y -= HudLayout.MenuButtonHeight + HudLayout.Gap;
            return field;
        }

        private Button AddButton(RectTransform parent, string key, float x, UnityEngine.Events.UnityAction onClick)
        {
            Button button = UiFactory.Button(key, parent, Ctx.Font, Ctx.Text.Get(key), onClick);
            UiFactory.Place((RectTransform)button.transform, Vector2.zero, Vector2.zero, new Vector2(x, 0f), new Vector2(150f, HudLayout.MenuButtonHeight));
            return button;
        }
    }
}
