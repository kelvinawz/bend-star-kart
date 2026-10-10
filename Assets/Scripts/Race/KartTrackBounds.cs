using UnityEngine;

namespace BendStarKart
{
    // 记录赛道中心线，检测车辆是否已脱离可驾驶区域。
    public sealed class KartTrackBounds : MonoBehaviour
    {
        [SerializeField] private Vector3[] centerLine;
        [SerializeField, Min(1f)] private float outerHalfWidth = 15f;
        [SerializeField, Min(0f)] private float safetyMargin = 4f;
        [SerializeField] private float minimumHeight = -5f;

        public bool IsOutside(Vector3 position)
        {
            if (position.y < minimumHeight) return true;
            if (centerLine == null || centerLine.Length < 2) return false;
            float limit = outerHalfWidth + safetyMargin;
            float best = float.PositiveInfinity;
            Vector2 point = new Vector2(position.x, position.z);
            for (int i = 0; i < centerLine.Length; i++)
            {
                Vector3 a3 = centerLine[i];
                Vector3 b3 = centerLine[(i + 1) % centerLine.Length];
                Vector2 a = new Vector2(a3.x, a3.z);
                Vector2 b = new Vector2(b3.x, b3.z);
                Vector2 segment = b - a;
                float lengthSquared = segment.sqrMagnitude;
                float t = lengthSquared > 0.0001f ?
                    Mathf.Clamp01(Vector2.Dot(point - a, segment) / lengthSquared) : 0f;
                best = Mathf.Min(best, (point - (a + segment * t)).sqrMagnitude);
            }
            return best > limit * limit;
        }

        public void Configure(Vector3[] points, float width)
        {
            centerLine = points;
            outerHalfWidth = width;
        }
    }
}
