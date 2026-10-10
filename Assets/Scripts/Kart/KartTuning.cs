using UnityEngine;

namespace BendStarKart
{
    [CreateAssetMenu(menuName = "Kart/Kart Tuning", fileName = "KartTuning")]
    public sealed class KartTuning : ScriptableObject
    {
        [Header("速度与动力")]
        [SerializeField, Min(1f)] public float maxSpeed = 36f;
        [SerializeField, Min(0f)] public float acceleration = 25f;
        [SerializeField] public AnimationCurve accelerationBySpeed = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.6f, 0.65f), new Keyframe(1f, 0.08f));
        [SerializeField, Min(0f)] public float coastDeceleration = 2.2f;
        [SerializeField, Min(0f)] public float brakeDeceleration = 42f;
        [SerializeField, Min(0f)] public float reverseAcceleration = 13f;
        [SerializeField, Min(0f)] public float reverseMaxSpeed = 10f;
        [SerializeField, Min(0f)] public float speedDisplayMultiplier = 5.5f;

        [Header("转向与侧滑")]
        [SerializeField, Min(0f)] public float steeringSensitivity = 1f;
        [SerializeField] public AnimationCurve steeringBySpeed = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.18f, 125f),
            new Keyframe(0.55f, 105f), new Keyframe(1f, 75f));
        [SerializeField, Range(0f, 1f)] public float lateralGrip = 0.95f;
        [SerializeField, Range(0f, 1f)] public float driftGrip = 0.15f;

        [Header("漂移")]
        [SerializeField, Min(0f)] public float driftMinSpeed = 9f;
        [SerializeField, Range(1f, 60f)] public float driftMaxAngle = 35f;
        [SerializeField, Min(0.01f)] public float driftBuildTime = 0.75f;
        [SerializeField, Min(0f)] public float driftYawRate = 150f;
        [SerializeField, Min(0f)] public float driftCoastDeceleration = 3f;
        [SerializeField, Min(0f)] public float straightenSpeed = 110f;
        [SerializeField, Min(0f)] public float counterSteerSpeed = 240f;
        [SerializeField, Min(0f)] public float driftExitSteerRate = 55f;

        [Header("地形与落地")]
        [SerializeField, Range(0.1f, 1f)] public float grassSpeedMultiplier = 0.6f;
        [SerializeField, Range(0.1f, 1f)] public float grassGripMultiplier = 0.8f;
        [SerializeField, Min(0f)] public float groundStickAcceleration = 17f;
        [SerializeField, Min(0f)] public float landingBounce = 1.3f;
        [SerializeField, Min(0f)] public float groundAlignSpeed = 9f;

        [Header("集气与氮气")]
        [SerializeField, Min(0f)] public float chargeCoefficient = 0.002f;
        [SerializeField, Min(0f)] public float nitroThrust = 18f;
        [SerializeField, Min(0.01f)] public float nitroDuration = 1.2f;
        [SerializeField, Min(1f)] public float nitroSpeedMultiplier = 1.25f;
        [SerializeField, Min(0f)] public float nitroDriftTimePenalty = 0.5f;
        [SerializeField, Min(0f)] public float doubleNitroWindow = 0.2f;
        [SerializeField, Min(0f)] public float doubleNitroExtraThrust = 8f;
        [SerializeField, Min(0f)] public float doubleNitroExtraSpeed = 0.08f;

        [Header("小喷")]
        [SerializeField, Min(0f)] public float miniBoostWindow = 0.25f;
        [SerializeField, Min(0f)] public float miniBoostDuration = 0.4f;
        [SerializeField, Min(0f)] public float miniBoostThrust = 9f;
        [SerializeField, Min(0f)] public float miniBoostExtraSpeed = 0.08f;
        [SerializeField, Min(0f)] public float miniStraightenAngle = 2f;

        [Header("起步喷与抢跑")]
        [SerializeField, Min(0f)] public float startWindowBeforeGo = 0.18f;
        [SerializeField, Min(0f)] public float startWindowAfterGo = 0.2f;
        [SerializeField, Min(0f)] public float earlyStartSkidDuration = 1f;
        [SerializeField, Min(0f)] public float startBoostDuration = 0.8f;
        [SerializeField, Min(0f)] public float startBoostThrust = 24f;
        [SerializeField, Min(0f)] public float startBoostExtraSpeed = 0.15f;
    }
}
