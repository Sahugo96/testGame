using System;
using System.Collections.Generic;
using System.Text;
using Game.Client.Localization;
using Game.Client.UI.Widgets;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using Game.Shared.Core;
using Game.Shared.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI.Windows
{
    /// <summary>
    /// Combat log (module 8): damage, healing, deaths and statuses that concern the player or their target, newest at the
    /// top. Lines are collected even while the window is closed; the server sends the events, the client only formats them.
    /// </summary>
    public sealed class CombatLogWindow : HudWindow
    {
        private static readonly Vector2 Size = new Vector2(560f, 420f);
        private const int MaxLines = 18;

        private readonly Text _text;
        private readonly StringBuilder _sb = new StringBuilder();

        public CombatLogWindow(HudWindowDto definition, RectTransform parent, Canvas canvas, ClientContext ctx, Tooltip tooltip,
            Action<HudWindow> focus, Action<HudWindow> close) : base(definition, parent, canvas, ctx, tooltip, focus, close, Size)
        {
            _text = UiFactory.Label("Log", Content, ctx.Font, HudLayout.FontSmall, TextAnchor.UpperLeft, HudPalette.Text);
            UiFactory.Stretch(_text.rectTransform);
            ctx.State.CombatEvent += message =>
            {
                if (IsOpen)
                {
                    Redraw();
                }
            };
        }

        public override void Refresh(PlayerStateSnapshot state) => Redraw();

        private string Format(CombatEventMessage message)
        {
            ILocalizationService text = Ctx.Text;
            string source = message.SourceId.IsValid ? EntityNames.Of(Ctx, message.SourceId) : string.Empty;
            string target = EntityNames.Of(Ctx, message.TargetId);
            string what = What(message.SkillId);
            switch (message.Kind)
            {
                case CombatEventKind.Damage:
                    string crit = message.CritTier > 0 ? " " + text.Format(UiKeys.CritTier, message.CritTier) : string.Empty;
                    string absorbed = message.Absorbed > 0 ? " " + text.Format(UiKeys.AbsorbedShort, message.Absorbed) : string.Empty;
                    return text.Format(UiKeys.LogDamage, source, target, message.Amount, what) + crit + absorbed;
                case CombatEventKind.Heal:
                case CombatEventKind.ResourceRestore:
                    return text.Format(UiKeys.LogHeal, source, target, message.Amount, what);
                case CombatEventKind.Death:
                    return text.Format(UiKeys.LogDeath, target, source);
                case CombatEventKind.StatusApplied:
                    return text.Format(UiKeys.LogStatusOn, target, what, message.Amount);
                default:
                    return text.Format(UiKeys.LogStatusOff, target, what);
            }
        }

        /// <summary>Skill or status name of the event (empty for plain hits).</summary>
        private string What(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return string.Empty;
            }
            if (Ctx.Content.Skills.TryGetValue(id, out SkillDefinition skill))
            {
                return Ctx.Text.Get(skill.NameKey);
            }
            return Ctx.Content.StatusEffects.TryGetValue(id, out StatusEffectDefinition status) ? Ctx.Text.Get(status.NameKey) : id;
        }

        private void Redraw()
        {
            List<CombatEventMessage> log = Ctx.State.CombatLog;
            _sb.Clear();
            for (int i = log.Count - 1; i >= 0 && i >= log.Count - MaxLines; i--)
            {
                _sb.Append(Format(log[i])).Append('\n');
            }
            _text.text = log.Count > 0 ? _sb.ToString() : Ctx.Text.Get(UiKeys.Empty);
        }
    }
}
