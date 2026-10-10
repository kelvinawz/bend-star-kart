using UnityEngine;

namespace BendStarKart
{
    [RequireComponent(typeof(Camera))]
    public sealed class KartChaseCamera : MonoBehaviour
    {
        [SerializeField] private KartController target;
        [SerializeField, Min(1f)] private float followDistance = 7f;
        [SerializeField, Min(0f)] private float followHeight = 3.2f;
        [SerializeField, Min(0f)] private float lookHeight = 0.9f;
        [SerializeField, Min(0f)] private float positionDamping = 6f;
        [SerializeField, Min(0f)] private float rotationDamping = 8f;
        [SerializeField, Range(0f, 1f)] private float driftVelocityBias = 0.7f;
        [SerializeField, Min(0f)] private float airborneHeight = 1.2f;
        [SerializeField, Min(0f)] private float landingDip = 0.35f;
        [SerializeField, Min(0f)] private float collisionShake = 0.18f;
        [SerializeField, Min(0f)] private float shakeDecay = 5f;
        [SerializeField, Min(1f)] private float defaultFov = 66f;
        [SerializeField, Min(1f)] private float boostFov = 76f;
        [SerializeField, Min(0f)] private float boostDistance = 1f;

        private Rigidbody targetBody;
        private KartBoost targetBoost;
        private Camera cameraComponent;
        private float shake;
        private float dip;
        private bool boostActive;

        private void Awake()
        {
            cameraComponent = GetComponent<Camera>();
            if (target != null) targetBody = target.GetComponent<Rigidbody>();
            if (target != null) targetBoost = target.GetComponent<KartBoost>();
            cameraComponent.fieldOfView = defaultFov;
        }

        private void OnEnable()
        {
            if (target != null)
            {
                target.Landed += OnLanded;
                target.WallHit += OnWallHit;
            }
            if (targetBoost != null) targetBoost.NitroChanged += SetBoostActive;
        }

        private void OnDisable()
        {
            if (target != null)
            {
                target.Landed -= OnLanded;
                target.WallHit -= OnWallHit;
            }
            if (targetBoost != null) targetBoost.NitroChanged -= SetBoostActive;
        }

        private void LateUpdate()
        {
            if (target == null || targetBody == null) return;
            float dt = Time.unscaledDeltaTime;
            Vector3 heading = Vector3.ProjectOnPlane(target.transform.forward, Vector3.up).normalized;
            Vector3 motion = Vector3.ProjectOnPlane(targetBody.linearVelocity, Vector3.up);
            if (target.IsDrifting && motion.sqrMagnitude > 4f)
                heading = Vector3.Slerp(heading, motion.normalized, driftVelocityBias).normalized;

            float air = target.IsGrounded ? 0f : airborneHeight;
            Vector3 desired = target.transform.position - heading *
                (followDistance + (boostActive ? boostDistance : 0f)) +
                Vector3.up * (followHeight + air - dip);
            float positionBlend = 1f - Mathf.Exp(-positionDamping * dt);
            transform.position = Vector3.Lerp(transform.position, desired, positionBlend);
            Vector3 aim = target.transform.position + Vector3.up * (lookHeight + air * 0.35f);
            Quaternion desiredRotation = Quaternion.LookRotation(aim - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation,
                1f - Mathf.Exp(-rotationDamping * dt));
            cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView,
                boostActive ? boostFov : defaultFov, 1f - Mathf.Exp(-5f * dt));
            if (shake > 0.001f)
                transform.position += Random.insideUnitSphere * shake * collisionShake;
            shake = Mathf.MoveTowards(shake, 0f, shakeDecay * dt);
            dip = Mathf.MoveTowards(dip, 0f, dt);
        }

        private void OnLanded(float amount) { dip = landingDip * amount; }
        private void OnWallHit(float amount) { shake = Mathf.Max(shake, amount); }

        public void SetBoostActive(bool value) { boostActive = value; }
        public void SetTarget(KartController value) { target = value; }
    }
}
