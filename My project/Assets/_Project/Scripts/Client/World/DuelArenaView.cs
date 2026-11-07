using Game.Shared.Protocol;
using UnityEngine;

namespace Game.Client.World
{
    /// <summary>
    /// Duel arena circle on the ground (question 70): a ring of posts around the server's arena center. Leaving it loses
    /// the duel. Primitives only, until art arrives.
    /// </summary>
    public sealed class DuelArenaView : MonoBehaviour
    {
        private const int PostCount = 48;
        private const float PostWidth = 0.25f;
        private const float PostHeight = 0.6f;

        private ClientContext _ctx;
        private Transform[] _posts;
        private Renderer[] _renderers;
        private float _radius;
        private Vector2 _center;
        private DuelPhase _phase;

        public void Init(ClientContext ctx)
        {
            _ctx = ctx;
            _posts = new Transform[PostCount];
            _renderers = new Renderer[PostCount];
            for (int i = 0; i < PostCount; i++)
            {
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = $"DuelPost_{i}";
                Destroy(post.GetComponent<Collider>());
                post.transform.SetParent(transform, false);
                post.transform.localScale = new Vector3(PostWidth, PostHeight, PostWidth);
                _posts[i] = post.transform;
                _renderers[i] = post.GetComponent<Renderer>();
            }
            gameObject.SetActive(false);
        }

        /// <summary>Called every frame by the owner (the view stays inactive while there is no duel).</summary>
        public void Tick()
        {
            PlayerStateSnapshot state = _ctx?.State.Player;
            DuelSnapshot duel = state != null ? state.Duel : default;
            bool visible = duel.Phase == DuelPhase.Countdown || duel.Phase == DuelPhase.Active;
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
            if (!visible)
            {
                _phase = DuelPhase.None;
                return;
            }
            var center = new Vector2(duel.CenterX, duel.CenterZ);
            if (duel.Phase == _phase && center == _center && Mathf.Approximately(duel.Radius, _radius))
            {
                return;
            }
            _phase = duel.Phase;
            _center = center;
            _radius = duel.Radius;
            Color color = duel.Phase == DuelPhase.Active ? WorldPalette.DuelArena : WorldPalette.DuelArenaCountdown;
            for (int i = 0; i < PostCount; i++)
            {
                float angle = i * Mathf.PI * 2f / PostCount;
                _posts[i].position = new Vector3(center.x + Mathf.Cos(angle) * _radius, PostHeight * 0.5f, center.y + Mathf.Sin(angle) * _radius);
                _renderers[i].material.color = color;
            }
        }
    }
}
