using UnityEngine;

namespace BendStarKart
{
    public sealed class KartTuningPanel : MonoBehaviour
    {
        [SerializeField] private KartController kart;
        [SerializeField] private KartInput input;
        [SerializeField] private bool visible;
        // 避开左上方的圈数与计时 HUD，仍允许玩家拖动窗口。
        private Rect window = new Rect(18f, 180f, 370f, 540f);
        private string notice = "F1 打开或关闭面板";

        private void Update()
        {
            if (input != null && input.TuningPressed) visible = !visible;
        }

        private void OnGUI()
        {
            if (kart == null) return;
            GUI.Box(new Rect(12f, Screen.height - 96f, 290f, 84f),
                $"速度 {kart.DisplaySpeed:0} km/h  |  F1 调参\n" +
                $"侧滑 {kart.SlipAngle:0}°  着地 {(kart.IsGrounded ? "是" : "否")}  " +
                $"漂移 {(kart.IsDrifting ? "是" : "否")}");
            if (Time.timeScale < 0.5f)
                GUI.Box(new Rect(Screen.width * 0.5f - 75f, 20f, 150f, 45f), "已暂停 · Esc 继续");
            if (visible) window = GUI.Window(7711, window, DrawWindow, "弯星卡丁 · 实时调参");
        }

        private void DrawWindow(int id)
        {
            KartTuning t = kart.Tuning;
            if (t == null) return;
            GUILayout.Space(8f);
            t.maxSpeed = Slider("最高速度 m/s", t.maxSpeed, 12f, 60f);
            t.acceleration = Slider("加速度", t.acceleration, 5f, 60f);
            t.steeringSensitivity = Slider("转向灵敏度", t.steeringSensitivity, 0.2f, 2f);
            t.lateralGrip = Slider("正常抓地力", t.lateralGrip, 0.5f, 0.999f);
            t.driftGrip = Slider("漂移抓地力", t.driftGrip, 0.01f, 0.7f);
            t.driftMaxAngle = Slider("漂移最大角度", t.driftMaxAngle, 5f, 60f);
            t.straightenSpeed = Slider("回正速度 °/s", t.straightenSpeed, 20f, 300f);
            t.chargeCoefficient = Slider("集气系数", t.chargeCoefficient, 0f, 0.02f);
            t.nitroThrust = Slider("氮气推力", t.nitroThrust, 0f, 60f);
            GUILayout.Space(8f);
            GUILayout.Label($"速度 {kart.Speed:0.0} m/s  ·  {kart.DisplaySpeed:0} km/h");
            GUILayout.Label($"侧滑角 {kart.SlipAngle:0.0}°  ·  着地：{kart.IsGrounded}");
            GUILayout.Label($"漂移：{kart.IsDrifting}  ·  草地：{kart.IsOnGrass}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("保存到 KartTuning.asset"))
            {
#if UNITY_EDITOR
                kart.SaveTuningToAsset();
                notice = "已保存到 ScriptableObject 资源";
#else
                notice = "运行时构建不可写入项目资源；请在编辑器 Play 模式保存";
#endif
            }
            if (GUILayout.Button("从资源读取"))
            {
                kart.ReloadTuningFromAsset();
                notice = "已从资源重载";
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(notice);
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 25f));
        }

        private static float Slider(string label, float value, float minimum, float maximum)
        {
            GUILayout.Label($"{label}: {value:0.###}");
            return GUILayout.HorizontalSlider(value, minimum, maximum);
        }

        public void Configure(KartController targetKart, KartInput targetInput)
        {
            kart = targetKart;
            input = targetInput;
        }
    }
}
