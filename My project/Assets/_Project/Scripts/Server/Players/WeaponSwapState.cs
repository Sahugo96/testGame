namespace Game.Server.Players
{
    /// <summary>
    /// In-combat weapon swap timers (question 10, confirmed by question 56).
    /// Main hand (one- and two-handed alike): after a swap the swap is locked for MainHandSwapCooldownSeconds;
    /// then a risk window of MainHandSwapRiskWindowSeconds follows — a swap there is allowed but locks swaps for
    /// MainHandSwapPenaltySeconds with no risk window afterwards; after the window swaps are free.
    /// Off hand: OffHandSwapCooldownSeconds lock, no risk window.
    /// </summary>
    public sealed class WeaponSwapState
    {
        public float MainHandLockRemaining;
        public float MainHandRiskRemaining;
        public float OffHandLockRemaining;
        private bool _riskWindowPending;
        private float _riskWindowLength;

        public bool CanSwapMainHand => MainHandLockRemaining <= 0f;

        public bool CanSwapOffHand => OffHandLockRemaining <= 0f;

        public void OnMainHandSwapped(float cooldown, float riskWindow, float penalty)
        {
            if (MainHandRiskRemaining > 0f)
            {
                MainHandRiskRemaining = 0f;
                MainHandLockRemaining = penalty;
                _riskWindowPending = false;
                return;
            }
            MainHandLockRemaining = cooldown;
            _riskWindowPending = true;
            _riskWindowLength = riskWindow;
        }

        public void OnOffHandSwapped(float cooldown) => OffHandLockRemaining = cooldown;

        public void Tick(float dt)
        {
            if (MainHandLockRemaining > 0f)
            {
                MainHandLockRemaining -= dt;
                if (MainHandLockRemaining <= 0f)
                {
                    MainHandLockRemaining = 0f;
                    if (_riskWindowPending)
                    {
                        MainHandRiskRemaining = _riskWindowLength;
                        _riskWindowPending = false;
                    }
                }
            }
            else if (MainHandRiskRemaining > 0f)
            {
                MainHandRiskRemaining = System.Math.Max(0f, MainHandRiskRemaining - dt);
            }
            if (OffHandLockRemaining > 0f)
            {
                OffHandLockRemaining = System.Math.Max(0f, OffHandLockRemaining - dt);
            }
        }
    }
}
