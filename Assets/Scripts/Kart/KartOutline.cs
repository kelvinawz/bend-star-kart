using UnityEngine;

namespace BendStarKart
{
    public sealed class KartOutline : MonoBehaviour
    {
        [SerializeField] private Material outlineMaterial;

        // 在编辑器生成车模时执行：为每个可见几何体添加放大的背面轮廓。
        public void Apply(Material material)
        {
            outlineMaterial = material;
            MeshFilter[] parts = GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter part in parts)
            {
                if (part.gameObject.name == "轮廓" || part.sharedMesh == null) continue;
                GameObject shell = new GameObject("轮廓");
                shell.transform.SetParent(part.transform, false);
                shell.AddComponent<MeshFilter>().sharedMesh = part.sharedMesh;
                shell.AddComponent<MeshRenderer>().sharedMaterial = outlineMaterial;
            }
        }
    }
}
