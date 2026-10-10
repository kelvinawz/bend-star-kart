using System;
using UnityEngine;

namespace BendStarKart
{
    [RequireComponent(typeof(KartController), typeof(KartInput), typeof(KartDrift))]
    public sealed class KartBoost : MonoBehaviour
    {
        private KartController kart;
        private KartInput input;
        private KartDrift drift;
        private RaceStartSignal startSignal;
        private bool nitroRequested;
        private bool awaitingStraighten;
        private bool miniKeyReleased;
        private bool startPressPending;
        private bool earlyPressPending;
        private bool startResolved;
        private bool goHandled;
        private bool currentDriftHitWall;
        private float lastNitroEndTime = -100f;
        private float nitroRemaining;
        private float miniRemaining;
        private float miniWindowRemaining;
        private float startRemaining;
        private float skidRemaining;
        private bool doubleNitro;

        public float Charge { get; private set; }
        public int NitroStock { get; private set; }
        public bool NitroActive => nitroRemaining > 0f;
        public bool MiniActive => miniRemaining > 0f;
        public bool StartBoostActive => startRemaining > 0f;
        public bool IsSkidding => skidRemaining > 0f;
        public bool IsDoubleNitro => doubleNitro && NitroActive;
        public float NitroRemaining => nitroRemaining;
        public float MiniWindowRemaining => miniWindowRemaining;

        public float SpeedMultiplier
        {
            get
            {
                KartTuning t = kart == null ? null : kart.Tuning;
                if (t == null) return 1f;
                float bonus = NitroActive ? t.nitroSpeedMultiplier - 1f : 0f;
                if (IsDoubleNitro) bonus += t.doubleNitroExtraSpeed;
                if (MiniActive) bonus += t.miniBoostExtraSpeed;
                if (StartBoostActive) bonus += t.startBoostExtraSpeed;
                return 1f + bonus;
            }
        }

        public float ForwardThrust
        {
            get
            {
                KartTuning t = kart == null ? null : kart.Tuning;
                if (t == null || IsSkidding) return 0f;
                float thrust = NitroActive ? t.nitroThrust : 0f;
                if (IsDoubleNitro) thrust += t.doubleNitroExtraThrust;
                if (MiniActive) thrust += t.miniBoostThrust;
                if (StartBoostActive) thrust += t.startBoostThrust;
                return thrust;
            }
        }

        public event Action<bool> NitroChanged;
        public event Action MiniBoostTriggered;
        public event Action StartBoostTriggered;

        private void Awake()
        {
            kart = GetComponent<KartController>();
            input = GetComponent<KartInput>();
            drift = GetComponent<KartDrift>();
            startSignal = GetComponent<RaceStartSignal>();
        }

        private void OnEnable()
        {
            if (kart != null)
            {
                kart.PhysicsSampled += OnPhysicsSampled;
                kart.WallCollision += OnWallCollision;
                kart.ResetPerformed += OnReset;
            }
            if (drift != null) drift.DriftChanged += OnDriftChanged;
        }

        private void OnDisable()
        {
            if (kart != null)
            {
                kart.PhysicsSampled -= OnPhysicsSampled;
                kart.WallCollision -= OnWallCollision;
                kart.ResetPerformed -= OnReset;
            }
            if (drift != null) drift.DriftChanged -= OnDriftChanged;
        }

        private void Update()
        {
            if (kart == null || kart.Tuning == null || input == null) return;
            KartTuning t = kart.Tuning;
            if (input.NitroPressed) nitroRequested = true;

            if (input.ThrottleReleased && miniWindowRemaining > 0f)
                miniKeyReleased = true;

            if (!input.ThrottlePressed) return;
            if (startSignal != null && !startSignal.IsGo)
            {
                if (startSignal.SecondsToGo <= t.startWindowBeforeGo && !earlyPressPending)
                    startPressPending = true;
                else
                {
                    earlyPressPending = true;
                    startPressPending = false;
                }
                return;
            }

            if (startSignal != null && !startResolved && !earlyPressPending &&
                startSignal.SecondsSinceGo <= t.startWindowAfterGo)
            {
                startResolved = true;
                StartBoost();
            }

            // 先松开再重新按加速，且车头已回正，才触发小喷。
            if (miniWindowRemaining > 0f && miniKeyReleased && kart.IsGrounded)
            {
                miniWindowRemaining = 0f;
                miniKeyReleased = false;
                miniRemaining = t.miniBoostDuration;
                MiniBoostTriggered?.Invoke();
            }
        }

        private void OnPhysicsSampled(float speed, float slipAngle, bool grounded,
            bool drifting, float dt)
        {
            KartTuning t = kart.Tuning;
            if (t == null) return;
            HandleStart(t, dt);
            TickTimers(t, drifting, dt);

            if (awaitingStraighten && grounded && !drifting &&
                Mathf.Abs(drift.HeadingOffset) <= t.miniStraightenAngle)
            {
                awaitingStraighten = false;
                miniWindowRemaining = t.miniBoostWindow;
                miniKeyReleased = input.Throttle < 0.1f;
            }

            if (grounded && drifting && (startSignal == null || startSignal.IsGo) &&
                NitroStock < 2)
            {
                // 每秒集气量 = 系数 × 侧滑角（度）× 水平速度（m/s）。
                Charge += t.chargeCoefficient * Mathf.Abs(slipAngle) * speed * dt;
                while (Charge >= 1f && NitroStock < 2)
                {
                    Charge -= 1f;
                    NitroStock++;
                }
                if (NitroStock >= 2) Charge = 0f;
            }

            if (nitroRequested)
            {
                nitroRequested = false;
                if ((startSignal == null || startSignal.IsGo) && !IsSkidding &&
                    !NitroActive && NitroStock > 0)
                    ActivateNitro(t);
            }
        }

        private void HandleStart(KartTuning t, float dt)
        {
            if (startSignal != null && startSignal.IsGo && !goHandled)
            {
                goHandled = true;
                if (earlyPressPending)
                {
                    skidRemaining = t.earlyStartSkidDuration;
                    startResolved = true;
                }
                else if (startPressPending && !startResolved)
                {
                    startResolved = true;
                    StartBoost();
                }
            }
            skidRemaining = Mathf.Max(0f, skidRemaining - dt);
        }

        private void TickTimers(KartTuning t, bool drifting, float dt)
        {
            if (nitroRemaining > 0f)
            {
                float penalty = drifting ? 1f + t.nitroDriftTimePenalty : 1f;
                nitroRemaining = Mathf.Max(0f, nitroRemaining - dt * penalty);
                if (nitroRemaining <= 0f)
                {
                    doubleNitro = false;
                    lastNitroEndTime = Time.time;
                    NitroChanged?.Invoke(false);
                }
            }
            miniRemaining = Mathf.Max(0f, miniRemaining - dt);
            startRemaining = Mathf.Max(0f, startRemaining - dt);
            miniWindowRemaining = Mathf.Max(0f, miniWindowRemaining - dt);
            if (miniWindowRemaining <= 0f) miniKeyReleased = false;
        }

        private void ActivateNitro(KartTuning t)
        {
            NitroStock--;
            doubleNitro = Time.time - lastNitroEndTime <= t.doubleNitroWindow;
            nitroRemaining = t.nitroDuration;
            NitroChanged?.Invoke(true);
        }

        private void StartBoost()
        {
            startRemaining = kart.Tuning.startBoostDuration;
            StartBoostTriggered?.Invoke();
        }

        private void OnDriftChanged(bool active)
        {
            if (active)
            {
                currentDriftHitWall = false;
                awaitingStraighten = false;
                miniWindowRemaining = 0f;
            }
            else if (!currentDriftHitWall)
            {
                awaitingStraighten = true;
            }
        }

        private void OnWallCollision(float impact, bool headOn)
        {
            if (!headOn || impact < 2f) return;
            currentDriftHitWall = true;
            awaitingStraighten = false;
            CancelSprays();
        }

        private void OnReset()
        {
            CancelSprays();
            nitroRequested = false;
            awaitingStraighten = false;
            currentDriftHitWall = false;
            skidRemaining = 0f;
            lastNitroEndTime = -100f;
        }

        private void CancelSprays()
        {
            bool hadNitro = NitroActive;
            nitroRemaining = 0f;
            miniRemaining = 0f;
            startRemaining = 0f;
            miniWindowRemaining = 0f;
            doubleNitro = false;
            lastNitroEndTime = -100f;
            if (hadNitro) NitroChanged?.Invoke(false);
        }
    }
}
