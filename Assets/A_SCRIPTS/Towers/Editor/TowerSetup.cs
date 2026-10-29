using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menús de la torre mortero:
//
//  Tools > Botes > Crear torre mortero
//      Crea el prefab de prueba (formas simples) en Assets/A_SCRIPTS/Towers si no existe y pone uno en la escena.
//
//  Tools > Botes > Ordenar referencias de la torre   (o en el componente: ⋮ > Ordenar referencias)
//      Para cuando le cambiás el modelo. Con la torre seleccionada (en la escena o abierta en modo prefab):
//       - Visual Root: el hijo que tiene el modelo (si hay varios hijos con modelos, los junta en "Visual")
//       - Head / Pulse Target: un hijo que se llame "Head" (si no hay, la torre no gira y se infla el Visual)
//       - Fire Point: un hijo "FirePoint"; si no hay lo crea arriba del modelo
//       - Collider: ajusta la cápsula del root al tamaño del modelo nuevo
//       - Tag "Enemy" + layer "EnemyLayer" en todo (para que tus balas le peguen)
//       - HitFlash: le carga los renderers del modelo nuevo
//       - Barra de vida: la pone un metro arriba del modelo
//      Las partículas (VFX) son opcionales: si están vacías no pasa nada.
public static class TowerSetup
{
    private const string Folder = "Assets/A_SCRIPTS/Towers";
    private const string MortarPath = Folder + "/TorreMortero.prefab";

    // ------------------------------------------------------------ crear

    [MenuItem("Tools/Botes/Crear torre mortero")]
    public static void Run()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/A_SCRIPTS", "Towers");

        var mortar = AssetDatabase.LoadAssetAtPath<GameObject>(MortarPath);
        if (mortar == null) mortar = BuildPrefab();

        Vector3 pivot = Vector3.zero;
        if (SceneView.lastActiveSceneView != null) pivot = SceneView.lastActiveSceneView.pivot;
        pivot.y = 0f;

        var b = (GameObject)PrefabUtility.InstantiatePrefab(mortar);
        b.transform.position = pivot;
        Undo.RegisterCreatedObjectUndo(b, "Crear TorreMortero");

        EditorSceneManager.MarkSceneDirty(b.scene);
        Selection.activeGameObject = b;
        AssetDatabase.SaveAssets();
        Debug.Log("[TowerSetup] Listo: una TorreMortero en la escena. Movela a donde quieras y guardá la escena.");
    }

    private static GameObject BuildPrefab()
    {
        var root = new GameObject("TorreMortero");

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        Material bodyMat = GetOrCreateMat("M_TowerMortar", new Color(0.35f, 0.3f, 0.28f));
        Material accentMat = GetOrCreateMat("M_TowerMortarAccent", new Color(0.85f, 0.35f, 0.15f));

        Prim(PrimitiveType.Cylinder, "Base", visual.transform, new Vector3(0f, 1.5f, 0f), new Vector3(2.2f, 1.5f, 2.2f), bodyMat);
        Prim(PrimitiveType.Cylinder, "Ring", visual.transform, new Vector3(0f, 3.05f, 0f), new Vector3(2.6f, 0.12f, 2.6f), accentMat);

        var head = new GameObject("Head");
        head.transform.SetParent(visual.transform, false);
        head.transform.localPosition = new Vector3(0f, 3.6f, 0f);
        Prim(PrimitiveType.Cube, "HeadBox", head.transform, Vector3.zero, new Vector3(1.6f, 0.9f, 1.6f), bodyMat);
        var barrel = Prim(PrimitiveType.Cylinder, "Barrel", head.transform, new Vector3(0f, 0.7f, 0.35f), new Vector3(0.7f, 0.7f, 0.7f), accentMat);
        barrel.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
        var firePoint = new GameObject("FirePoint").transform;
        firePoint.SetParent(head.transform, false);
        firePoint.localPosition = new Vector3(0f, 1.4f, 0.75f);

        root.AddComponent<CapsuleCollider>();
        root.AddComponent<HitFlash>();
        root.AddComponent<EnemyHealthBar>();
        var tower = root.AddComponent<MortarTower>();
        var so = new SerializedObject(tower);
        so.FindProperty("_maxHealth").floatValue = 350f;
        so.ApplyModifiedPropertiesWithoutUndo();

        FixReferences(tower, false);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, MortarPath);
        Object.DestroyImmediate(root);
        Debug.Log($"[TowerSetup] Prefab creado: {MortarPath}");
        return prefab;
    }

    // ------------------------------------------------------------ ordenar referencias

    [MenuItem("Tools/Botes/Ordenar referencias de la torre")]
    private static void FixSelected()
    {
        var tower = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<SpecialTower>() : null;
        if (tower == null)
        {
            EditorUtility.DisplayDialog("Torre", "Seleccioná la torre (o cualquier hijo de la torre) en la escena o en modo prefab.", "Ok");
            return;
        }
        FixReferences(tower, true);
    }

    [MenuItem("CONTEXT/MortarTower/Ordenar referencias")]
    private static void FixFromContext(MenuCommand cmd) => FixReferences((SpecialTower)cmd.context, true);

    public static void FixReferences(SpecialTower tower, bool withUndo)
    {
        Transform root = tower.transform;
        if (withUndo) Undo.RegisterFullObjectHierarchyUndo(tower.gameObject, "Ordenar referencias de la torre");
        var log = new List<string>();
        var so = new SerializedObject(tower);

        // 1) Visual: dónde está el modelo
        Transform visual = (so.FindProperty("_visualRoot").objectReferenceValue as GameObject)?.transform;
        if (visual == null || !HasRenderers(visual)) visual = FindChild(root, "Visual");
        if (visual == null || !HasRenderers(visual))
        {
            var withModel = new List<Transform>();
            foreach (Transform c in root) if (HasRenderers(c)) withModel.Add(c);

            if (withModel.Count == 1) visual = withModel[0];
            else if (withModel.Count > 1)
            {
                var container = new GameObject("Visual").transform;
                if (withUndo) Undo.RegisterCreatedObjectUndo(container.gameObject, "Crear Visual");
                container.SetParent(root, false);
                foreach (var c in withModel)
                {
                    if (withUndo) Undo.SetTransformParent(c, container, "Mover a Visual");
                    else c.SetParent(container, true);
                }
                visual = container;
                log.Add($"Junté {withModel.Count} modelos dentro de 'Visual'");
            }
        }
        if (visual == null)
        {
            Debug.LogWarning("[TowerSetup] No encontré el modelo de la torre: tiene que ser un hijo del objeto que tiene MortarTower " +
                             "(no un MeshRenderer en el mismo objeto), así se puede hundir sin mover el collider.", tower);
            return;
        }
        so.FindProperty("_visualRoot").objectReferenceValue = visual.gameObject;
        log.Add($"Visual Root = {visual.name}");

        // 2) Head (opcional)
        Transform head = FindChild(root, "Head");
        so.FindProperty("_head").objectReferenceValue = head;
        so.FindProperty("_pulseTarget").objectReferenceValue = head != null ? head : visual;
        log.Add(head != null ? $"Head = {head.name}" : "Sin Head: la torre no gira, se infla el Visual entero");

        // 3) Medidas del modelo
        Bounds b = RenderBounds(visual);

        // 4) FirePoint
        Transform fire = FindChild(root, "FirePoint");
        if (fire == null)
        {
            fire = new GameObject("FirePoint").transform;
            if (withUndo) Undo.RegisterCreatedObjectUndo(fire.gameObject, "Crear FirePoint");
            fire.SetParent(head != null ? head : root, false);
            fire.position = new Vector3(b.center.x, b.max.y + 0.5f, b.center.z);
            log.Add("Creé FirePoint arriba del modelo");
        }
        so.FindProperty("_firePoint").objectReferenceValue = fire;

        // 5) Collider: cápsula del root ajustada al modelo
        var capsule = root.GetComponent<CapsuleCollider>();
        if (capsule == null)
        {
            capsule = withUndo ? Undo.AddComponent<CapsuleCollider>(root.gameObject) : root.gameObject.AddComponent<CapsuleCollider>();
        }
        else if (withUndo) Undo.RecordObject(capsule, "Ajustar collider");
        Vector3 ls = root.lossyScale;
        float sx = Mathf.Max(0.0001f, Mathf.Abs(ls.x)), sy = Mathf.Max(0.0001f, Mathf.Abs(ls.y)), sz = Mathf.Max(0.0001f, Mathf.Abs(ls.z));
        capsule.direction = 1;
        capsule.isTrigger = false;
        capsule.center = root.InverseTransformPoint(b.center);
        capsule.height = b.size.y / sy;
        capsule.radius = Mathf.Max(b.size.x / sx, b.size.z / sz) * 0.5f * 0.85f;
        log.Add($"Collider: alto {capsule.height:0.0}, radio {capsule.radius:0.0}");

        // 6) Tag + layer para que tus balas le peguen
        int layer = LayerMask.NameToLayer("EnemyLayer");
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (withUndo) Undo.RecordObject(t.gameObject, "Tag/Layer");
            t.gameObject.tag = "Enemy";
            if (layer >= 0) t.gameObject.layer = layer;
        }
        if (layer < 0) log.Add("OJO: no existe el layer 'EnemyLayer'");

        // 7) HitFlash con los renderers del modelo nuevo
        var flash = root.GetComponent<HitFlash>();
        if (flash == null) flash = withUndo ? Undo.AddComponent<HitFlash>(root.gameObject) : root.gameObject.AddComponent<HitFlash>();
        var fso = new SerializedObject(flash);
        var list = fso.FindProperty("renderers");
        var rends = new List<Renderer>();
        foreach (var r in visual.GetComponentsInChildren<Renderer>(true)) if (!(r is ParticleSystemRenderer)) rends.Add(r);
        list.arraySize = rends.Count;
        for (int i = 0; i < rends.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = rends[i];
        fso.ApplyModifiedProperties();
        log.Add($"HitFlash: {rends.Count} renderers");

        // 8) Barra de vida (la misma de los enemigos) arriba del modelo
        var bar = root.GetComponent<EnemyHealthBar>();
        if (bar == null) bar = withUndo ? Undo.AddComponent<EnemyHealthBar>(root.gameObject) : root.gameObject.AddComponent<EnemyHealthBar>();
        var bso = new SerializedObject(bar);
        bso.FindProperty("_worldOffset").vector3Value = new Vector3(0f, b.max.y - root.position.y + 1f, 0f);
        bso.FindProperty("_hideUntilHit").boolValue = true;
        bso.ApplyModifiedProperties();

        so.ApplyModifiedProperties();

        if (withUndo)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(tower)) PrefabUtility.RecordPrefabInstancePropertyModifications(tower);
            EditorUtility.SetDirty(tower);
            if (tower.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(tower.gameObject.scene);
        }

        string extra = PrefabUtility.IsPartOfPrefabInstance(tower)
            ? "\nEstá en la escena: si querés que quede en el prefab, Overrides > Apply All."
            : "";
        Debug.Log("[TowerSetup] Referencias ordenadas:\n - " + string.Join("\n - ", log) + extra, tower);
    }

    private static bool HasRenderers(Transform t)
    {
        foreach (var r in t.GetComponentsInChildren<Renderer>(true))
            if (!(r is ParticleSystemRenderer)) return true;
        return false;
    }

    private static Transform FindChild(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t != root && string.Equals(t.name, name, System.StringComparison.OrdinalIgnoreCase)) return t;
        return null;
    }

    private static Bounds RenderBounds(Transform visual)
    {
        bool has = false;
        var b = new Bounds(visual.position, Vector3.zero);
        foreach (var r in visual.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer) continue;
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }
        return b;
    }

    // ------------------------------------------------------------ util

    private static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        Object.DestroyImmediate(go.GetComponent<Collider>());
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
        // emisión prendida (en negro) para que el HitFlash se vea, igual que en los enemigos
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            mat.SetColor("_EmissionColor", Color.black);
        }
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
