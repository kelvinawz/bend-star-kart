using System;
using UnityEngine;

namespace BendStarKart
{
    public sealed class KartDrift : MonoBehaviour
    {
        private KartTuning tuning;
        private float heldTime;
        private float currentOffset;
        private int direction;
        private bool mustReleaseKey;

        public bool IsDrifting { get; private set; }
        public float HeadingOffset => currentOffset;
        public event Action<bool> DriftChanged;

        public void Initialize(KartTuning runtimeTuning) { tuning = runtimeTuning; }

        // 漂移改变的是车头朝向；侧向速度的衰减由 KartController 负责。
        public float Step(float steer, bool held, float speed, float dt)
        {
            if (tuning == null) return 0f;
            if (!held) mustReleaseKey = false;
            if (!IsDrifting && !mustReleaseKey && held && Mathf.Abs(steer) > 0.1f &&
                speed >= tuning.driftMinSpeed)
            {
                IsDrifting = true;
                direction = steer > 0f ? 1 : -1;
                heldTime = 0f;
                DriftChanged?.Invoke(true);
            }

            if (IsDrifting)
            {
                if (!held || speed < tuning.driftMinSpeed * 0.55f || steer * direction < -0.25f)
                {
                    if (held) mustReleaseKey = true;
                    IsDrifting = false;
                    DriftChanged?.Invoke(false);
                }
                else
                {
                    heldTime += dt;
                    float target = direction * tuning.driftMaxAngle *
                        Mathf.Clamp01(heldTime / Mathf.Max(0.01f, tuning.driftBuildTime));
                    // 松开方向键时缩小漂移半径；继续同向输入则加大甩尾。
                    if (Mathf.Abs(steer) < 0.1f) target *= 0.55f;
                    currentOffset = Mathf.MoveTowards(currentOffset, target, tuning.driftYawRate * dt);
                    return currentOffset;
                }
            }

            float rate = held && steer * direction < -0.25f
                ? tuning.counterSteerSpeed : tuning.straightenSpeed;
            currentOffset = Mathf.MoveTowards(currentOffset, 0f, rate * dt);
            return currentOffset;
        }

        public void ResetState()
        {
            if (IsDrifting) DriftChanged?.Invoke(false);
            IsDrifting = false;
            heldTime = 0f;
            currentOffset = 0f;
            direction = 0;
            mustReleaseKey = false;
        }
    }
}
