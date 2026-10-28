using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Arma las dos torres de prueba (con formas simples) listas para jugar.
// Menú: Tools > Botes > Crear torres (Espiral y Mortero)
//
// - Crea los prefabs en Assets/A_SCRIPTS/Towers (si ya existen NO los pisa, solo los vuelve a poner en la escena).
// - Pone una de cada una en la escena, cerca de donde está mirando la Scene view.
// - Tag "Enemy" + layer "EnemyLayer" para que tus balas les peguen, collider, HitFlash.
// Después reemplazá los hijos de "Visual" por tus modelos (dejá FirePoint y Head donde corresponda).
public static class TowerSetup
{
    private const string Folder = "Assets/A_SCRIPTS/Towers";
    private const string SpiralPath = Folder + "/TorreEspiral.prefab";
    private const string MortarPath = Folder + "/TorreMortero.prefab";

    [MenuItem("Tools/Botes/Crear torres (Espiral y Mortero)")]
    public static void Run()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/A_SCRIPTS", "Towers");

        var spiral = AssetDatabase.LoadAssetAtPath<GameObject>(SpiralPath);
        if (spiral == null) spiral = BuildPrefab(false);

        var mortar = AssetDatabase.LoadAssetAtPath<GameObject>(MortarPath);
        if (mortar == null) mortar = BuildPrefab(true);

        Vector3 pivot = Vector3.zero;
        if (SceneView.lastActiveSceneView != null) pivot = SceneView.lastActiveSceneView.pivot;
        pivot.y = 0f;

        var a = (GameObject)PrefabUtility.InstantiatePrefab(spiral);
        a.transform.position = pivot + new Vector3(-8f, 0f, 0f);
        Undo.RegisterCreatedObjectUndo(a, "Crear TorreEspiral");

        var b = (GameObject)PrefabUtility.InstantiatePrefab(mortar);
        b.transform.position = pivot + new Vector3(8f, 0f, 0f);
        Undo.RegisterCreatedObjectUndo(b, "Crear TorreMortero");

        EditorSceneManager.MarkSceneDirty(a.scene);
        Selection.objects = new Object[] { a, b };
        AssetDatabase.SaveAssets();
        Debug.Log("[TowerSetup] Listo: una TorreEspiral y una TorreMortero en la escena. Movelas a donde quieras y guardá la escena.");
    }

    private static GameObject BuildPrefab(bool mortar)
    {
        string name = mortar ? "TorreMortero" : "TorreEspiral";
        var root = new GameObject(name);

        // ---- visual (formas simples para probar) ----
        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        Material bodyMat = GetOrCreateMat(mortar ? "M_TowerMortar" : "M_TowerSpiral",
                                          mortar ? new Color(0.35f, 0.3f, 0.28f) : new Color(0.3f, 0.22f, 0.45f));
        Material accentMat = GetOrCreateMat(mortar ? "M_TowerMortarAccent" : "M_TowerSpiralAccent",
                                            mortar ? new Color(0.85f, 0.35f, 0.15f) : new Color(1f, 0.35f, 0.75f));

        var body = Prim(PrimitiveType.Cylinder, "Base", visual.transform, new Vector3(0f, 1.5f, 0f), new Vector3(2.2f, 1.5f, 2.2f), bodyMat);
        Prim(PrimitiveType.Cylinder, "Ring", visual.transform, new Vector3(0f, 3.05f, 0f), new Vector3(2.6f, 0.12f, 2.6f), accentMat);

        var head = new GameObject("Head");
        head.transform.SetParent(visual.transform, false);
        head.transform.localPosition = new Vector3(0f, 3.6f, 0f);

        Transform firePoint;
        if (mortar)
        {
            Prim(PrimitiveType.Cube, "HeadBox", head.transform, Vector3.zero, new Vector3(1.6f, 0.9f, 1.6f), bodyMat);
            var barrel = Prim(PrimitiveType.Cylinder, "Barrel", head.transform, new Vector3(0f, 0.7f, 0.35f), new Vector3(0.7f, 0.7f, 0.7f), accentMat);
            barrel.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
            firePoint = new GameObject("FirePoint").transform;
            firePoint.SetParent(head.transform, false);
            firePoint.localPosition = new Vector3(0f, 1.4f, 0.75f);
        }
        else
        {
            Prim(PrimitiveType.Sphere, "Orb", head.transform, Vector3.zero, Vector3.one * 1.6f, accentMat);
            firePoint = new GameObject("FirePoint").transform;
            firePoint.SetParent(root.transform, false);
            firePoint.localPosition = new Vector3(0f, 1.5f, 0f); // a la altura del barco, para que las balas le peguen
        }

        // ---- colisión: una sola cápsula en el root ----
        foreach (var c in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        var capsule = root.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 2f, 0f);
        capsule.radius = 1.3f;
        capsule.height = 4.5f;

        // ---- tag / layer para que tus balas le peguen ----
        int layer = LayerMask.NameToLayer("EnemyLayer");
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.tag = "Enemy";
            if (layer >= 0) t.gameObject.layer = layer;
        }

        root.AddComponent<HitFlash>();

        // ---- script de la torre ----
        SpecialTower tower = mortar ? root.AddComponent<MortarTower>() : (SpecialTower)root.AddComponent<SpiralTower>();
        var so = new SerializedObject(tower);
        so.FindProperty("_visualRoot").objectReferenceValue = visual;
        so.FindProperty("_head").objectReferenceValue = head.transform;
        so.FindProperty("_pulseTarget").objectReferenceValue = head.transform;
        so.FindProperty("_firePoint").objectReferenceValue = firePoint;

        var deathVfx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/A_VFX/PARTICLES/Explosions/WoodExplosionVFX.prefab");
        if (deathVfx != null) so.FindProperty("_deathVfx").objectReferenceValue = deathVfx;
        if (mortar)
        {
            var splash = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/A_VFX/PARTICLES/Explosions/WaterSplash.prefab");
            if (splash != null) so.FindProperty("_landingVfx").objectReferenceValue = splash;
            so.FindProperty("_maxHealth").floatValue = 350f;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        string path = mortar ? MortarPath : SpiralPath;
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[TowerSetup] Prefab creado: {path}");
        return prefab;
    }

    private static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    private static Material GetOrCreateMat(string name, Color color)
    {
        string path = $"{Folder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
