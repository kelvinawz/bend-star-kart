using UnityEngine;

namespace BendStarKart
{
    public sealed class RaceHud : MonoBehaviour
    {
        [SerializeField] private RaceManager race;
        [SerializeField] private KartController kart;
        [SerializeField] private Transform[] mapWaypoints;
        [SerializeField, Min(10f)] private float mapSize = 190f;

        private Vector2 mapMin;
        private Vector2 mapMax;

        private void Awake()
        {
            if (mapWaypoints == null || mapWaypoints.Length == 0) return;
            mapMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            mapMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (Transform point in mapWaypoints)
            {
                Vector2 position = new Vector2(point.position.x, point.position.z);
                mapMin = Vector2.Min(mapMin, position);
                mapMax = Vector2.Max(mapMax, position);
            }
        }

        private void OnGUI()
        {
            if (race == null || kart == null) return;
            GUI.depth = -100;
            DrawRaceStats();
            DrawSpeedometer();
            DrawMinimap();
            if (race.HasFinished) DrawFinish();
        }

        private void DrawRaceStats()
        {
            string bestLap = race.BestLap > 0f ? FormatTime(race.BestLap) : "--:--.--";
            GUI.Box(new Rect(14f, 14f, 225f, 151f),
                $"弯星巡回赛道\n第 {race.PlayerRank}/4 名" +
                $"   第 {race.CurrentLap}/{race.TotalLaps} 圈\n" +
                $"下一检查点  {race.NextCheckpoint}/{race.CheckpointCount - 1}\n" +
                $"本圈  {FormatTime(race.CurrentLapTime)}\n" +
                $"总计  {FormatTime(race.TotalTime)}\n" +
                $"最佳单圈  {bestLap}");
        }

        private void DrawSpeedometer()
        {
            GUI.Box(new Rect(Screen.width - 199f, Screen.height - 109f, 185f, 94f),
                $"速度\n{kart.DisplaySpeed:0} km/h");
        }

        private void DrawMinimap()
        {
            if (mapWaypoints == null || mapWaypoints.Length < 2) return;
            Rect area = new Rect(Screen.width - mapSize - 14f, 16f, mapSize, mapSize);
            GUI.Box(area, "小地图");
            for (int i = 0; i < mapWaypoints.Length; i++)
            {
                Vector2 a = MapPoint(mapWaypoints[i].position, area);
                Vector2 b = MapPoint(mapWaypoints[(i + 1) % mapWaypoints.Length].position,
                    area);
                DrawLine(a, b, 2f, new Color(0.8f, 0.88f, 0.93f));
            }
            Vector2 player = MapPoint(kart.transform.position, area);
            Color old = GUI.color;
            KartAiDriver[] opponents = race.Opponents;
            if (opponents != null)
            {
                Color[] colors =
                {
                    new Color(0.83f, 0.42f, 0.96f),
                    new Color(1f, 0.74f, 0.17f),
                    new Color(1f, 0.39f, 0.43f)
                };
                for (int i = 0; i < opponents.Length; i++)
                {
                    if (opponents[i] == null) continue;
                    Vector2 dot = MapPoint(opponents[i].transform.position, area);
                    GUI.color = colors[i % colors.Length];
                    GUI.DrawTexture(new Rect(dot.x - 4f, dot.y - 4f, 8f, 8f),
                        Texture2D.whiteTexture);
                }
            }
            GUI.color = new Color(1f, 0.72f, 0.13f);
            GUI.DrawTexture(new Rect(player.x - 5f, player.y - 5f, 10f, 10f),
                Texture2D.whiteTexture);
            GUI.color = old;
        }

        private Vector2 MapPoint(Vector3 world, Rect area)
        {
            float x = Mathf.InverseLerp(mapMin.x, mapMax.x, world.x);
            float y = Mathf.InverseLerp(mapMin.y, mapMax.y, world.z);
            return new Vector2(area.x + 14f + x * (area.width - 28f),
                area.y + 24f + (1f - y) * (area.height - 38f));
        }

        private static void DrawLine(Vector2 start, Vector2 end, float width, Color color)
        {
            Vector2 delta = end - start;
            if (delta.sqrMagnitude < 0.01f) return;
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg,
                start);
            GUI.DrawTexture(new Rect(start.x, start.y - width * 0.5f,
                delta.magnitude, width), Texture2D.whiteTexture);
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        private void DrawFinish()
        {
            Rect panel = new Rect(Screen.width * 0.5f - 195f,
                Screen.height * 0.5f - 180f, 390f, 360f);
            GUI.Box(panel, "完赛 · 弯星巡回赛道");
            GUI.Label(new Rect(panel.x + 25f, panel.y + 42f, 340f, 26f),
                $"最终名次  第 {race.PlayerRank}/4 名");
            GUI.Label(new Rect(panel.x + 25f, panel.y + 72f, 340f, 26f),
                "总成绩  " + FormatTime(race.FinishTime));
            string bestTotal = race.BestTotal > 0f ? FormatTime(race.BestTotal) : "--:--.--";
            GUI.Label(new Rect(panel.x + 25f, panel.y + 102f, 340f, 26f),
                "历史最佳  " + bestTotal);
            GUI.Label(new Rect(panel.x + 25f, panel.y + 132f, 340f, 26f),
                "最佳单圈  " + FormatTime(race.BestLap));
            for (int i = 0; i < race.LapSplits.Count; i++)
                GUI.Label(new Rect(panel.x + 25f, panel.y + 167f + i * 29f, 340f, 25f),
                    $"第 {i + 1} 圈  {FormatTime(race.LapSplits[i])}");
            if (GUI.Button(new Rect(panel.x + 100f, panel.y + 282f, 190f, 42f),
                "重新比赛"))
                race.Restart();
        }

        public static string FormatTime(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int centiseconds = Mathf.FloorToInt(seconds * 100f);
            int minutes = centiseconds / 6000;
            int remainder = centiseconds % 6000;
            return $"{minutes:00}:{remainder / 100:00}.{remainder % 100:00}";
        }

        public void Configure(RaceManager manager, KartController controller,
            Transform[] waypoints)
        {
            race = manager;
            kart = controller;
            mapWaypoints = waypoints;
        }
    }
}
