using System.Collections.Generic;
using Game.Client.Localization;
using Game.Client.World;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Widgets
{
    /// <summary>Floating combat numbers above entities (damage, heal, restore); crits show their tier (question 25).</summary>
    public sealed class CombatTextWidget
    {
        private const float HeightAboveEntity = 2.2f;
        private static readonly Vector2 LabelSize = new Vector2(200f, 32f);

        private sealed class Entry
        {
            public Text Text;
            public Vector3 World;
            public float Age;
        }

        private readonly ClientContext _ctx;
        private readonly EntityViewManager _views;
        private readonly RectTransform _root;
        private readonly List<Entry> _active = new List<Entry>();
        private readonly Stack<Entry> _pool = new Stack<Entry>();

        public CombatTextWidget(RectTransform root, ClientContext ctx, EntityViewManager views)
        {
            _ctx = ctx;
            _views = views;
            _root = root;
        }

        /// <summary>Interface setting "damage numbers" (module 17).</summary>
        public bool Enabled = true;

        public void Show(CombatEventMessage message)
        {
            if (!Enabled)
            {
                return;
            }
            bool numeric = message.Kind == CombatEventKind.Damage || message.Kind == CombatEventKind.Heal || message.Kind == CombatEventKind.ResourceRestore;
            if (!numeric || (message.Amount <= 0 && message.Absorbed <= 0) || !_views.TryGetView(message.TargetId, out EntityView view))
            {
                return;
            }
            Entry entry = _pool.Count > 0 ? _pool.Pop() : Create();
            entry.Text.gameObject.SetActive(true);
            entry.World = view.transform.position + Vector3.up * HeightAboveEntity;
            entry.Age = 0f;
            string amount = message.Kind == CombatEventKind.Damage ? "-" + message.Amount : "+" + message.Amount;
            if (message.Absorbed > 0)
            {
                amount += " " + _ctx.Text.Format(UiKeys.AbsorbedShort, message.Absorbed);
            }
            if (message.CritTier > 0)
            {
                amount += " " + _ctx.Text.Format(UiKeys.CritTier, message.CritTier);
            }
            entry.Text.text = amount;
            entry.Text.fontSize = message.CritTier > 0 ? HudLayout.FontHuge : HudLayout.FontLarge;
            entry.Text.color = message.Kind == CombatEventKind.Damage ? HudPalette.Damage
                : message.Kind == CombatEventKind.Heal ? HudPalette.Heal : HudPalette.Restore;
            _active.Add(entry);
        }

        public void Tick(float dt)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Entry entry = _active[i];
                entry.Age += dt;
                float t = entry.Age / HudLayout.CombatTextSeconds;
                Vector3 screen = _ctx.Camera.WorldToScreenPoint(entry.World);
                if (t >= 1f || screen.z <= 0f)
                {
                    entry.Text.gameObject.SetActive(false);
                    _active.RemoveAt(i);
                    _pool.Push(entry);
                    continue;
                }
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out Vector2 local);
                entry.Text.rectTransform.anchoredPosition = local + Vector2.up * (HudLayout.CombatTextRise * t);
                Color color = entry.Text.color;
                color.a = 1f - t;
                entry.Text.color = color;
            }
        }

        private Entry Create()
        {
            Text text = UiFactory.Label("CombatText", _root, _ctx.Font, HudLayout.FontLarge, TextAnchor.MiddleCenter, HudPalette.Damage);
            text.rectTransform.sizeDelta = LabelSize;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return new Entry { Text = text };
        }
    }
}
