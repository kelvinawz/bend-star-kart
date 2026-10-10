using UnityEngine;

namespace BendStarKart
{
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class KartAiDriver : MonoBehaviour
    {
        [SerializeField] private Vector3[] route;
        [SerializeField] private RaceStartSignal startSignal;
        [SerializeField, Min(1)] private int totalLaps = 3;
        [SerializeField, Min(1f)] private float cruiseSpeed = 30f;
        [SerializeField, Min(0f)] private float acceleration = 14f;
        [SerializeField] private float laneOffset;
        [SerializeField, Min(0f)] private float startBehind = 8f;
        [SerializeField, Min(0f)] private float jumpArcHeight = 1.2f;

        private Rigidbody body;
        private BoxCollider bodyCollider;
        private float[] distanceAtPoint;
        private float trackLength;
        private float speed;
        private float travelled;

        public float Progress => travelled - startBehind;
        public bool HasFinished { get; private set; }
        public float FinishTime { get; private set; }
        public float TrackLength => trackLength;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            bodyCollider = GetComponent<BoxCollider>();
            body.isKinematic = true;
            // 路径驱动的 AI 不参与实体推挤，避免把停在起点的玩家顶进护栏。
            bodyCollider.isTrigger = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            if (route == null || route.Length < 3)
            {
                Debug.LogError("AI 车辆缺少闭合路径。", this);
                enabled = false;
                return;
            }
            distanceAtPoint = new float[route.Length + 1];
            for (int i = 0; i < route.Length; i++)
                distanceAtPoint[i + 1] = distanceAtPoint[i] +
                    Vector3.Distance(route[i], route[(i + 1) % route.Length]);
            trackLength = distanceAtPoint[route.Length];
            Pose pose = Sample(-startBehind);
            body.position = pose.position;
            body.rotation = pose.rotation;
        }

        private void FixedUpdate()
        {
            if (startSignal == null || !startSignal.IsGo || HasFinished) return;
            float dt = Time.fixedDeltaTime;
            int segment = FindSegment(Mathf.Repeat(Progress, trackLength));
            Vector3 before = (route[segment] - route[(segment - 1 + route.Length) % route.Length]).normalized;
            Vector3 after = (route[(segment + 2) % route.Length] - route[(segment + 1) % route.Length]).normalized;
            float bend = Vector3.Angle(before, after);
            float targetSpeed = cruiseSpeed * Mathf.Lerp(1f, 0.62f, Mathf.Clamp01(bend / 80f));
            speed = Mathf.MoveTowards(speed, targetSpeed, acceleration * dt);
            travelled += speed * dt;
            Pose pose = Sample(Progress);
            body.MovePosition(pose.position);
            body.MoveRotation(pose.rotation);

            if (Progress >= totalLaps * trackLength)
            {
                HasFinished = true;
                FinishTime = Mathf.Max(0f, Time.time - startSignal.GoTime);
                // 完赛 AI 停在起点后，不继续以运动学车身阻挡玩家。
                bodyCollider.enabled = false;
            }
        }

        private Pose Sample(float progress)
        {
            float along = Mathf.Repeat(progress, trackLength);
            int i = FindSegment(along);
            int next = (i + 1) % route.Length;
            float segmentLength = Mathf.Max(0.001f, distanceAtPoint[i + 1] - distanceAtPoint[i]);
            float t = Mathf.Clamp01((along - distanceAtPoint[i]) / segmentLength);
            Vector3 center = Vector3.Lerp(route[i], route[next], t);
            Vector3 forward = (route[next] - route[i]).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            center += right * laneOffset + Vector3.up * 0.36f;
            // 与道路断面匹配的短跳跃弧，避免运动学车在跳台空隙平移。
            if (center.z < -45f && center.x > 11f && center.x < 30f)
                center.y += Mathf.Sin((center.x - 11f) / 19f * Mathf.PI) * jumpArcHeight;
            return new Pose(center, Quaternion.LookRotation(forward, Vector3.up));
        }

        private int FindSegment(float along)
        {
            int low = 0;
            int high = route.Length - 1;
            while (low < high)
            {
                int mid = (low + high + 1) / 2;
                if (distanceAtPoint[mid] <= along) low = mid;
                else high = mid - 1;
            }
            return low;
        }

        public void Configure(Vector3[] points, RaceStartSignal signal, int laps,
            float targetSpeed, float lateralOffset, float behind)
        {
            route = points;
            startSignal = signal;
            totalLaps = laps;
            cruiseSpeed = targetSpeed;
            laneOffset = lateralOffset;
            startBehind = behind;
        }
    }
}
