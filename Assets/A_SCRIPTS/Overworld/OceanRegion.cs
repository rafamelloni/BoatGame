using UnityEngine;

// Un océano del Overworld. La posición de este objeto es la "semilla" de su zona:
// OverworldMap reparte el mapa entre todas las semillas (Voronoi) y arma la forma sola.
// Moviendo estos objetos en el Editor el mapa se rehace en vivo.
[DisallowMultipleComponent]
public class OceanRegion : MonoBehaviour
{
    [Header("Nivel")]
    public string displayName = "Océano";
    [TextArea(2, 4)] public string description;
    [Tooltip("Nombre exacto de la escena del nivel (tiene que estar en Build Settings)")]
    public string sceneName;
    public bool unlocked = false;

    [Header("Visual")]
    public Color color = new Color(0.16f, 0.45f, 0.47f);
    [Tooltip("Opcional: material de agua propio para este océano (variante del shader de agua). " +
             "Si está vacío usa el del OverworldMap teñido con 'color'.")]
    public Material waterMaterialOverride;

    // Lo llena OverworldMap al generar (en espacio local del mapa, plano XZ)
    [System.NonSerialized] public Vector2[] polygon;
    [System.NonSerialized] public Vector2 centroid;
    [System.NonSerialized] public Transform visual;
    [System.NonSerialized] public MeshRenderer visualRenderer;
    [System.NonSerialized] public MeshRenderer waterRenderer;
    [System.NonSerialized] public float currentLift;
    [System.NonSerialized] public float currentHighlight;

    public bool CanEnter => unlocked && !string.IsNullOrEmpty(sceneName);

    // localXZ = posición en espacio local del mapa
    public bool Contains(Vector2 localXZ)
    {
        if (polygon == null || polygon.Length < 3) return false;
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 a = polygon[i], b = polygon[j];
            if (((a.y > localXZ.y) != (b.y > localXZ.y)) &&
                (localXZ.x < (b.x - a.x) * (localXZ.y - a.y) / (b.y - a.y) + a.x))
                inside = !inside;
        }
        return inside;
    }

#if UNITY_EDITOR
    // Click derecho en el componente > "Crear material de agua propio"
    // Duplica el material de agua del mapa, lo tiñe con 'color' y lo asigna acá para tunearlo.
    [ContextMenu("Crear material de agua propio")]
    private void CreateOwnWaterMaterial()
    {
        var map = GetComponentInParent<OverworldMap>();
        if (map == null || map.WaterMaterial == null)
        {
            Debug.LogWarning("[OceanRegion] El OverworldMap no tiene material de agua asignado.");
            return;
        }
        const string folder = "Assets/A_DATA/Overworld";
        System.IO.Directory.CreateDirectory(folder);
        var mat = new Material(map.WaterMaterial) { name = "Water_" + name };
        OverworldMap.SetWaterColors(mat, color, Color.Lerp(color, Color.black, 0.6f));
        string path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath($"{folder}/Water_{name}.mat");
        UnityEditor.AssetDatabase.CreateAsset(mat, path);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.Undo.RecordObject(this, "Asignar material de agua");
        waterMaterialOverride = mat;
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.Selection.activeObject = mat;
    }
#endif

    private void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 0.4f);
    }
}
