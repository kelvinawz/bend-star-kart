using UnityEngine;

namespace BendStarKart
{
    public sealed class KartSurface : MonoBehaviour
    {
        [SerializeField] private bool isGrass;
        public bool IsGrass => isGrass;
        public void SetGrass(bool value) { isGrass = value; }
    }
}
