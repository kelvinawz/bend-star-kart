using System;
using UnityEngine;

namespace BendStarKart
{
    [RequireComponent(typeof(Rigidbody), typeof(KartInput), typeof(KartDrift))]
    public sealed class KartController : MonoBehaviour
    {
        [SerializeField] private KartTuning tuningAsset;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField, Min(0.05f)] private float groundCastRadius = 0.28f;
        [SerializeField, Min(0.1f)] private float groundCastDistance = 1.1f;
        [SerializeField, Min(0f)] private float castStartHeight = 0.35f;
        [SerializeField] private KartTrackBounds trackBounds;
        [SerializeField, Min(0.1f)] private float outsideResetDelay = 1.5f;

        private Rigidbody body;
        private KartInput input;
        private KartDrift drift;
        private KartBoost boost;
        private RaceStartSignal startSignal;
        private KartTuning runtimeTuning;
        private Vector3 groundNormal = Vector3.up;
        private Vector3 lastCheckpointPosition;
        private Quaternion lastCheckpointRotation;
        private bool wasGrounded;
        private float previousVerticalSpeed;
        private float outsideTime;

        public KartTuning Tuning => runtimeTuning;
        public float Speed => body == null ? 0f : Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).magnitude;
        public float DisplaySpeed => Speed * (runtimeTuning == null ? 5.5f : runtimeTuning.speedDisplayMultiplier);
        public float SlipAngle { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsDrifting => drift != null && drift.IsDrifting;
        public bool IsOnGrass { get; private set; }
        public bool RaceFinished { get; private set; }
        public event Action<float> Landed;
        public event Action<float> WallHit;
        public event Action<bool> DriftChanged;
        public event Action<float, float, bool, bool, float> PhysicsSampled;
        public event Action<float, bool> WallCollision;
        public event Action ResetPerformed;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            input = GetComponent<KartInput>();
            drift = GetComponent<KartDrift>();
            boost = GetComponent<KartBoost>();
            startSignal = GetComponent<RaceStartSignal>();
            if (tuningAsset == null)
            {
                Debug.LogError("KartController 缺少 KartTuning 资源；请重新生成测试赛道。", this);
                enabled = false;
                return;
            }
            // 每次 Play 使用运行时副本；调参面板的“保存”才会写回资源。
            runtimeTuning = Instantiate(tuningAsset);
            runtimeTuning.name = tuningAsset.name + " (Runtime)";
            drift.Initialize(runtimeTuning);
            drift.DriftChanged += OnDriftChanged;
            body.linearDamping = 0f;
            body.angularDamping = 8f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.None;
            lastCheckpointPosition = body.position;
            lastCheckpointRotation = body.rotation;
        }

        private void OnDestroy()
        {
            if (drift != null) drift.DriftChanged -= OnDriftChanged;
            if (runtimeTuning != null) Destroy(runtimeTuning);
        }

        private void Update()
        {
            if (input.ResetPressed) ResetToCheckpoint();
            if (input.PausePressed) Time.timeScale = Time.timeScale < 0.5f ? 1f : 0f;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            // 越过护栏或掉下赛道时自动回到最近有效检查点，避免无限坠落。
            outsideTime = trackBounds != null && trackBounds.IsOutside(body.position) ?
                outsideTime + dt : 0f;
            if (outsideTime >= outsideResetDelay)
            {
                ResetToCheckpoint();
                return;
            }
            body.angularVelocity = Vector3.zero;
            DetectGround();
            if (IsGrounded && !wasGrounded && previousVerticalSpeed < -2f)
            {
                float impact = Mathf.Min(1f, -previousVerticalSpeed / 14f);
                body.AddForce(groundNormal * runtimeTuning.landingBounce * impact, ForceMode.VelocityChange);
                Landed?.Invoke(impact);
            }
            wasGrounded = IsGrounded;

            Vector3 velocity = body.linearVelocity;
            if (IsGrounded)
            {
                float speed = Vector3.ProjectOnPlane(velocity, groundNormal).magnitude;
                float steer = input.Steer;
                float driftOffset = drift.Step(steer, input.Drift, speed, dt);

                Vector3 forward = Vector3.ProjectOnPlane(body.rotation * Vector3.forward, groundNormal).normalized;
                Vector3 flatVelocity = Vector3.ProjectOnPlane(velocity, groundNormal);
                if ((drift.IsDrifting || Mathf.Abs(driftOffset) > 0.01f) && flatVelocity.sqrMagnitude > 1f)
                {
                    // 车头绕速度方向甩出。松开 Shift 后由 KartDrift 逐步回正。
                    forward = Quaternion.AngleAxis(driftOffset, groundNormal) * flatVelocity.normalized;
                    if (!drift.IsDrifting)
                        forward = Quaternion.AngleAxis(steer * runtimeTuning.driftExitSteerRate * dt,
                            groundNormal) * forward;
                }
                else if (speed > 0.5f)
                {
                    float steeringRate = runtimeTuning.steeringBySpeed.Evaluate(
                        Mathf.Clamp01(speed / Mathf.Max(1f, runtimeTuning.maxSpeed)));
                    float reverse = Vector3.Dot(flatVelocity, forward) < -0.5f ? -1f : 1f;
                    forward = Quaternion.AngleAxis(steer * reverse * steeringRate *
                        runtimeTuning.steeringSensitivity * dt, groundNormal) * forward;
                }

                Vector3 visualUp = Vector3.Slerp(body.rotation * Vector3.up, groundNormal,
                    1f - Mathf.Exp(-runtimeTuning.groundAlignSpeed * dt)).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(
                    Vector3.ProjectOnPlane(forward, visualUp).normalized, visualUp);
                body.MoveRotation(targetRotation);

                forward = Vector3.ProjectOnPlane(targetRotation * Vector3.forward, groundNormal).normalized;
                Vector3 right = Vector3.Cross(groundNormal, forward).normalized;
                float forwardSpeed = Vector3.Dot(flatVelocity, forward);
                float sideSpeed = Vector3.Dot(flatVelocity, right);
                SlipAngle = flatVelocity.sqrMagnitude < 1f ? 0f :
                    Vector3.SignedAngle(forward, flatVelocity.normalized, groundNormal);
                PhysicsSampled?.Invoke(speed, SlipAngle, true, drift.IsDrifting, dt);

                float grip = drift.IsDrifting ? runtimeTuning.driftGrip : runtimeTuning.lateralGrip;
                if (IsOnGrass) grip *= runtimeTuning.grassGripMultiplier;
                // 把每个 0.02 秒物理步的抓地率换算到当前固定步长。
                float remainingSide = Mathf.Pow(1f - Mathf.Clamp01(grip), dt / 0.02f);
                sideSpeed *= remainingSide;

                float topSpeed = runtimeTuning.maxSpeed *
                    (IsOnGrass ? runtimeTuning.grassSpeedMultiplier : 1f);
                if (boost != null) topSpeed *= boost.SpeedMultiplier;
                bool canDrive = (startSignal == null || startSignal.IsGo) &&
                    (boost == null || !boost.IsSkidding) && !RaceFinished;
                if (!canDrive)
                    forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f,
                        runtimeTuning.brakeDeceleration * dt);
                else if (input.Throttle > 0.1f)
                {
                    if (forwardSpeed < -0.5f)
                        forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f,
                            runtimeTuning.brakeDeceleration * dt);
                    else
                        forwardSpeed += runtimeTuning.acceleration *
                            runtimeTuning.accelerationBySpeed.Evaluate(
                                Mathf.Clamp01(forwardSpeed / Mathf.Max(1f, topSpeed))) * dt;
                }
                else if (input.Brake > 0.1f)
                {
                    if (forwardSpeed > 0.5f)
                        forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f,
                            runtimeTuning.brakeDeceleration * dt);
                    else
                        forwardSpeed -= runtimeTuning.reverseAcceleration * dt;
                }
                else
                {
                    forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f,
                        runtimeTuning.coastDeceleration * dt);
                }
                if (drift.IsDrifting)
                    forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f,
                        runtimeTuning.driftCoastDeceleration * dt);
                if (canDrive && boost != null)
                    forwardSpeed += boost.ForwardThrust * dt;

                forwardSpeed = Mathf.Clamp(forwardSpeed, -runtimeTuning.reverseMaxSpeed, topSpeed);
                Vector3 horizontal = forward * forwardSpeed + right * sideSpeed;
                if (horizontal.magnitude > topSpeed && forwardSpeed >= 0f)
                    horizontal = horizontal.normalized * topSpeed;

                // 保留重力及跳台产生的竖直速度，仅限水平速度。
                float worldVertical = Vector3.Dot(velocity, Vector3.up);
                body.linearVelocity = horizontal + Vector3.up * worldVertical;
                body.AddForce(-groundNormal * runtimeTuning.groundStickAcceleration,
                    ForceMode.Acceleration);
            }
            else
            {
                // 空中没有驱动或转向，完全由 Rigidbody 的重力和碰撞处理。
                SlipAngle = 0f;
                PhysicsSampled?.Invoke(Speed, 0f, false, drift.IsDrifting, dt);
            }
            previousVerticalSpeed = Vector3.Dot(body.linearVelocity, Vector3.up);
        }

        private void DetectGround()
        {
            Vector3 start = body.position + Vector3.up * castStartHeight;
            IsGrounded = Physics.SphereCast(start, groundCastRadius, Vector3.down,
                out RaycastHit hit, groundCastDistance, groundMask,
                QueryTriggerInteraction.Ignore);
            groundNormal = IsGrounded ? hit.normal : Vector3.up;
            IsOnGrass = IsGrounded && hit.collider.GetComponentInParent<KartSurface>()?.IsGrass == true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.contactCount == 0) return;
            float impact = 0f;
            Vector3 normal = Vector3.zero;
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint point = collision.GetContact(i);
                if (Mathf.Abs(point.normal.y) < 0.45f)
                {
                    normal = point.normal;
                    impact = Mathf.Max(impact, collision.relativeVelocity.magnitude);
                }
            }
            if (impact < 1f) return;
            Vector3 velocity = body.linearVelocity;
            float headOn = Mathf.Clamp01(-Vector3.Dot(transform.forward, normal));
            float retained = Mathf.Lerp(0.88f, 0.38f, headOn);
            // 移除推向墙内的分量，保留沿墙切向运动。
            Vector3 intoWall = Vector3.Project(velocity, normal);
            if (Vector3.Dot(intoWall, normal) < 0f) velocity -= intoWall;
            body.linearVelocity = velocity * retained;
            WallHit?.Invoke(Mathf.Clamp01(impact / 20f));
            WallCollision?.Invoke(impact, headOn > 0.65f);
        }

        private void OnDriftChanged(bool active) { DriftChanged?.Invoke(active); }

        public void SetCheckpoint(Vector3 position, Quaternion rotation)
        {
            lastCheckpointPosition = position;
            lastCheckpointRotation = rotation;
        }

        public void ConfigureTrackBounds(KartTrackBounds bounds) { trackBounds = bounds; }

        public void ResetToCheckpoint()
        {
            drift.ResetState();
            // 检查点生成在道路中心；先抬起车身再清空速度，确保 R 从场外回到路面。
            body.position = lastCheckpointPosition + Vector3.up * 1.2f;
            body.rotation = lastCheckpointRotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            wasGrounded = false;
            outsideTime = 0f;
            ResetPerformed?.Invoke();
        }

        public void EndRace()
        {
            RaceFinished = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        public void SaveTuningToAsset()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.CopySerialized(runtimeTuning, tuningAsset);
            UnityEditor.EditorUtility.SetDirty(tuningAsset);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        public void ReloadTuningFromAsset()
        {
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(tuningAsset), runtimeTuning);
        }
    }
}
