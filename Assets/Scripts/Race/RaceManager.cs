using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BendStarKart
{
    public sealed class RaceManager : MonoBehaviour
    {
        [SerializeField] private KartController player;
        [SerializeField] private RaceStartSignal startSignal;
        [SerializeField] private KartCheckpoint[] checkpoints;
        [SerializeField] private Vector3[] route;
        [SerializeField] private KartAiDriver[] opponents;
        [SerializeField, Min(1)] private int totalLaps = 3;
        [SerializeField, Min(0f)] private float minimumForwardSpeed = 0.5f;
        [SerializeField] private string recordKey = "BendStarKart.Starloop";

        private readonly List<float> lapSplits = new List<float>();
        private float raceStartTime;
        private float lapStartTime;
        private int nextCheckpoint = 1;
        private float[] routeDistances;
        private float routeLength;

        public int CurrentLap { get; private set; } = 1;
        public int TotalLaps => totalLaps;
        public int NextCheckpoint => nextCheckpoint;
        public int CheckpointCount => checkpoints == null ? 0 : checkpoints.Length;
        public bool HasStarted { get; private set; }
        public bool HasFinished { get; private set; }
        public float TotalTime => HasStarted ?
            (HasFinished ? FinishTime : Time.time - raceStartTime) : 0f;
        public float CurrentLapTime => HasStarted && !HasFinished ?
            Time.time - lapStartTime : 0f;
        public float FinishTime { get; private set; }
        public float BestLap { get; private set; }
        public float BestTotal { get; private set; }
        public IReadOnlyList<float> LapSplits => lapSplits;
        public KartAiDriver[] Opponents => opponents;
        public float PlayerProgress => GetPlayerProgress();
        public int PlayerRank
        {
            get
            {
                int rank = 1;
                if (opponents == null) return rank;
                float playerProgress = GetPlayerProgress();
                foreach (KartAiDriver ai in opponents)
                {
                    if (ai == null) continue;
                    if (ai.HasFinished && !HasFinished ||
                        ai.HasFinished && HasFinished && ai.FinishTime < FinishTime ||
                        !ai.HasFinished && !HasFinished && ai.Progress > playerProgress + 0.5f)
                        rank++;
                }
                return rank;
            }
        }
        public event Action<int, float> LapCompleted;
        public event Action<float> RaceFinished;

        private void Awake()
        {
            BestLap = PlayerPrefs.GetFloat(recordKey + ".BestLap", 0f);
            BestTotal = PlayerPrefs.GetFloat(recordKey + ".BestTotal", 0f);
            if (checkpoints == null || checkpoints.Length < 2)
                Debug.LogError("RaceManager 至少需要起终点和一个中途检查点。", this);
            if (route != null && route.Length > 2)
            {
                routeDistances = new float[route.Length + 1];
                for (int i = 0; i < route.Length; i++)
                    routeDistances[i + 1] = routeDistances[i] +
                        Vector3.Distance(route[i], route[(i + 1) % route.Length]);
                routeLength = routeDistances[route.Length];
            }
        }

        private void Update()
        {
            if (HasStarted || startSignal == null || !startSignal.IsGo) return;
            // 使用同一个 GO 时刻，避免比赛计时与起步喷窗口相差一帧。
            raceStartTime = startSignal.GoTime;
            lapStartTime = raceStartTime;
            HasStarted = true;
        }

        public bool TryPassCheckpoint(int index, KartController kart, Vector3 forward,
            Vector3 respawnPosition, Quaternion respawnRotation)
        {
            if (!HasStarted || HasFinished || kart != player ||
                checkpoints == null || index != nextCheckpoint) return false;
            Vector3 velocity = kart.GetComponent<Rigidbody>().linearVelocity;
            if (Vector3.Dot(velocity, forward) < minimumForwardSpeed) return false;

            kart.SetCheckpoint(respawnPosition, respawnRotation);
            if (index == 0)
            {
                float lapTime = Mathf.Max(0f, Time.time - lapStartTime);
                lapSplits.Add(lapTime);
                SaveBestLap(lapTime);
                LapCompleted?.Invoke(CurrentLap, lapTime);
                if (CurrentLap >= totalLaps)
                {
                    FinishTime = Mathf.Max(0f, Time.time - raceStartTime);
                    HasFinished = true;
                    kart.EndRace();
                    SaveBestTotal(FinishTime);
                    RaceFinished?.Invoke(FinishTime);
                }
                else
                {
                    CurrentLap++;
                    lapStartTime = Time.time;
                }
                nextCheckpoint = 1;
            }
            else
            {
                nextCheckpoint = (index + 1) % checkpoints.Length;
            }
            return true;
        }

        private void SaveBestLap(float time)
        {
            if (BestLap > 0f && time >= BestLap) return;
            BestLap = time;
            PlayerPrefs.SetFloat(recordKey + ".BestLap", time);
            PlayerPrefs.Save();
        }

        private void SaveBestTotal(float time)
        {
            if (BestTotal > 0f && time >= BestTotal) return;
            BestTotal = time;
            PlayerPrefs.SetFloat(recordKey + ".BestTotal", time);
            PlayerPrefs.Save();
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0)
                SceneManager.LoadScene(scene.buildIndex);
            else
                Debug.LogError("重开需要先把比赛场景加入 Build Profiles。", this);
        }

        public void Configure(KartController kart, RaceStartSignal signal,
            KartCheckpoint[] orderedCheckpoints, Vector3[] trackPoints,
            KartAiDriver[] aiDrivers)
        {
            player = kart;
            startSignal = signal;
            checkpoints = orderedCheckpoints;
            route = trackPoints;
            opponents = aiDrivers;
        }

        private float GetPlayerProgress()
        {
            if (routeDistances == null || player == null || checkpoints == null ||
                checkpoints.Length < 2) return 0f;
            if (HasFinished) return totalLaps * routeLength;
            int previous = nextCheckpoint == 0 ? checkpoints.Length - 1 : nextCheckpoint - 1;
            int first = previous * route.Length / checkpoints.Length;
            int end = nextCheckpoint == 0 ? route.Length :
                nextCheckpoint * route.Length / checkpoints.Length;
            Vector3 position = player.transform.position;
            float nearest = float.PositiveInfinity;
            float along = routeDistances[first];
            for (int i = first; i < end; i++)
            {
                Vector3 a = route[i];
                Vector3 b = route[(i + 1) % route.Length];
                Vector3 segment = b - a;
                float fraction = Mathf.Clamp01(Vector3.Dot(position - a, segment) /
                    Mathf.Max(0.001f, segment.sqrMagnitude));
                float squareDistance = (position - (a + segment * fraction)).sqrMagnitude;
                if (squareDistance < nearest)
                {
                    nearest = squareDistance;
                    along = Mathf.Lerp(routeDistances[i], routeDistances[i + 1], fraction);
                }
            }
            return (CurrentLap - 1) * routeLength + along;
        }
    }
}
