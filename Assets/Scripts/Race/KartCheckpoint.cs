using UnityEngine;

namespace BendStarKart
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class KartCheckpoint : MonoBehaviour
    {
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private RaceManager race;
        [SerializeField, Min(0)] private int checkpointIndex;

        private void OnTriggerEnter(Collider other)
        {
            KartController kart = other.GetComponentInParent<KartController>();
            if (kart == null || respawnPoint == null) return;
            if (race != null)
                race.TryPassCheckpoint(checkpointIndex, kart, transform.forward,
                    respawnPoint.position, respawnPoint.rotation);
            else
                kart.SetCheckpoint(respawnPoint.position, respawnPoint.rotation);
        }

        public void SetRespawnPoint(Transform point) { respawnPoint = point; }
        public void ConfigureRace(RaceManager manager, int index)
        {
            race = manager;
            checkpointIndex = index;
        }
    }
}
