using UnityEngine;

namespace BendStarKart
{
    public sealed class KartBoostHud : MonoBehaviour
    {
        [SerializeField] private KartBoost boost;
        [SerializeField] private RaceStartSignal startSignal;

        private void OnGUI()
        {
            if (boost == null) return;
            DrawCountdown();
            DrawCharge();
            if (boost.NitroActive) DrawSpeedLines();
        }

        private void DrawCountdown()
        {
            if (startSignal == null) return;
            string message = null;
            if (!startSignal.IsGo)
                message = Mathf.CeilToInt(startSignal.SecondsToGo).ToString();
            else if (startSignal.SecondsSinceGo < 0.8f)
                message = "GO!";
            if (boost.IsSkidding) message = "抢跑 · 打滑";
            if (message != null)
                GUI.Box(new Rect(Screen.width * 0.5f - 90f, 35f, 180f, 55f), message);
        }

        private void DrawCharge()
        {
            float x = Screen.width * 0.5f - 150f;
            float y = Screen.height - 94f;
            GUI.Box(new Rect(x, y, 300f, 80f), "集气与氮气");
            Color previous = GUI.color;
            GUI.color = new Color(0.08f, 0.13f, 0.18f, 1f);
            GUI.DrawTexture(new Rect(x + 12f, y + 28f, 182f, 15f), Texture2D.whiteTexture);
            GUI.color = new Color(0.1f, 0.81f, 0.97f, 1f);
            GUI.DrawTexture(new Rect(x + 12f, y + 28f, 182f * Mathf.Clamp01(boost.Charge), 15f),
                Texture2D.whiteTexture);
            GUI.color = previous;
            for (int i = 0; i < 2; i++)
            {
                GUI.color = i < boost.NitroStock ?
                    new Color(0.1f, 0.81f, 0.97f) : new Color(0.18f, 0.25f, 0.3f);
                GUI.Box(new Rect(x + 205f + i * 39f, y + 25f, 34f, 22f), "N");
            }
            GUI.color = previous;
            string state = boost.IsDoubleNitro ? "双喷" :
                boost.NitroActive ? "氮气" :
                boost.MiniActive ? "小喷" :
                boost.StartBoostActive ? "起步喷" :
                boost.MiniWindowRemaining > 0f ? "小喷窗口：松开再按 ↑" : "Ctrl / Space 使用氮气";
            GUI.Label(new Rect(x + 12f, y + 49f, 276f, 22f), state);
        }

        private static void DrawSpeedLines()
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.57f, 0.93f, 1f, 0.2f);
            for (int i = 0; i < 7; i++)
            {
                float flow = Mathf.Repeat(Time.time * 0.72f + i * 0.15f, 0.85f);
                float y = Screen.height * (0.08f + flow);
                float width = 50f + (i % 3) * 24f;
                GUI.DrawTexture(new Rect(12f, y, width, 2f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(Screen.width - width - 12f, y + 10f,
                    width, 2f), Texture2D.whiteTexture);
            }
            GUI.color = previous;
        }

        public void Configure(KartBoost kartBoost, RaceStartSignal signal)
        {
            boost = kartBoost;
            startSignal = signal;
        }
    }
}
