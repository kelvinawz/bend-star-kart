using UnityEngine;

namespace BendStarKart
{
    // 阶段 2 的轻量起跑信号；阶段 3 的正式比赛流程可沿用 IsGo 和 GoTime。
    public sealed class RaceStartSignal : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float countdownSeconds = 3f;
        private float goTime;

        public bool IsGo => Time.time >= goTime;
        public float GoTime => goTime;
        public float SecondsToGo => goTime - Time.time;
        public float SecondsSinceGo => Time.time - goTime;

        private void Awake()
        {
            goTime = Time.time + countdownSeconds;
        }
    }
}
