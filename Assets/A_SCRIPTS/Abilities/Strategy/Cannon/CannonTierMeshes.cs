using System;
using UnityEngine;

public class CannonTierMeshes : MonoBehaviour
{
    [Serializable]
    public struct TierMesh
    {
        [Tooltip("Tier del barco a partir del cual se muestra este mesh (1, 2 o 3).")]
        public int tier;
        [Tooltip("Hijo con el mesh. Si ponés el MISMO cañón, se prende/apaga solo su MeshRenderer (los scripts siguen andando).")]
        public GameObject mesh;
    }

    [SerializeField] private TierMesh[] _meshes;

    // shipTier: 1, 2 o 3
    public void SetTier(int shipTier)
    {
        // Buscar el mesh con el tier más alto que no supere el tier actual
        int best = -1;
        for (int i = 0; i < _meshes.Length; i++)
        {
            if (_meshes[i].tier > shipTier) continue;
            if (best == -1 || _meshes[i].tier > _meshes[best].tier)
                best = i;
        }

        for (int i = 0; i < _meshes.Length; i++)
            SetVisible(_meshes[i].mesh, i == best);
    }

    private void SetVisible(GameObject target, bool visible)
    {
        if (target == null) return;

        if (target == gameObject)
        {
            // Es el propio cañón: no lo apagamos (tiene scripts e hijos),
            // solo ocultamos su renderer.
            var r = GetComponent<Renderer>();
            if (r != null) r.enabled = visible;
        }
        else
        {
            target.SetActive(visible);
        }
    }
}