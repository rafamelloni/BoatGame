using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Arma el Heavy Ranged Enemy y su proyectil, listos para el sistema de enemigos.
// Menú: Tools > Botes > Preparar Heavy Ranged Enemy
//
// - Si NO existe el prefab: seleccioná en la escena el modelo del barco que quieras usar y corré el menú.
//   Hace una copia y le pone todo (no toca el original).
// - Si YA existe: lo repara completando solo lo que falte (no pisa tus valores).
//
// Lo que asegura:
//  - tag y layer del Ship (le pegan tus balas; si te choca, daño + knockback como el Ship)
//  - EnemyHealth + SO_HeavyEnemy, collider, Rigidbody kinematic
//  - HeavyRangedEnemy con FirePoint (lo crea adelante del modelo si no hay) y el proyectil asignado
//  - EnemyEmerge y EnemyHealthBar (si faltan, copiados del Ship)
//  - HeavyProjectile.prefab (esfera + trigger + TrailRenderer) si no existe
//  - asignado en el campo Heavy de la EnemyFactory de la escena
public static class HeavyEnemySetup
{
    private const string Folder = "Assets/A_SCRIPTS/Enemies/HeavyRanged";
    private const string PrefabPath = Folder + "/HeavyRanged.prefab";
    private const string ProjectilePath = Folder + "/HeavyProjectile.prefab";
    private const string DataPath = Folder + "/SO_HeavyEnemy.asset";
    private const string ProjMatPath = Folder + "/M_HeavyProjectile.mat";
    private const string TrailMatPath = Folder + "/M_HeavyTrail.mat";
    private const float DefaultHealth = 200f;

    [MenuItem("Tools/Botes/Preparar Heavy Ranged Enemy")]
    public static void Run()
    {
        var factory = Object.FindFirstObjectByType<EnemyFactory>(FindObjectsInactive.Include);
        GameObject shipPrefab = null;
        if (factory != null)
            shipPrefab = new SerializedObject(factory).FindProperty("_shipPrefab").objectReferenceValue as GameObject;
        if (shipPrefab == null)
            shipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/A_SCRIPTS/Enemies/ShipEnemy/Ship(Blue)01.prefab");

        HeavyProjectile projectile = GetOrCreateProjectile();
        SO_EnemyData data = GetOrCreateData(shipPrefab);
        GameObject prefab;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Configure(root, shipPrefab, data, projectile);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Debug.Log("[HeavyEnemySetup] Prefab existente reparado (no se tocaron tus valores).");
        }
        else
        {
            var source = Selection.activeGameObject;
            if (source == null || EditorUtility.IsPersistent(source))
            {
                EditorUtility.DisplayDialog("Heavy Ranged Enemy",
                    "Seleccioná en la escena el modelo del barco que quieras usar para este enemigo y volvé a correr el menú.", "OK");
                return;
            }

            var go = Object.Instantiate(source);
            go.name = "HeavyRanged";
            go.SetActive(true);
            go.transform.position = Vector3.zero;
            Configure(go, shipPrefab, data, projectile);
            prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath, out bool ok);
            Object.DestroyImmediate(go);
            if (!ok || prefab == null) { Debug.LogError("[HeavyEnemySetup] No se pudo guardar el prefab."); return; }
            Debug.Log("[HeavyEnemySetup] Prefab creado.");
        }

        if (factory != null)
        {
            var fso = new SerializedObject(factory);
            fso.FindProperty("_heavyPrefab").objectReferenceValue = prefab;
            fso.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(factory.gameObject.scene);
        }
        else Debug.LogWarning("[HeavyEnemySetup] No encontré EnemyFactory en la escena: arrastrá el prefab al campo Heavy a mano.");

        AssetDatabase.SaveAssets();
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"[HeavyEnemySetup] Listo: {PrefabPath}. Guardá la escena y configurá Heavy en las fases del PhaseManager.");
    }

    // ---------------- enemigo ----------------
    private static void Configure(GameObject go, GameObject shipPrefab, SO_EnemyData data, HeavyProjectile projectile)
    {
        if (shipPrefab != null)
        {
            go.tag = shipPrefab.tag;
            SetLayerRecursive(go, shipPrefab.layer);
        }
        else go.tag = "ShipEnemy";

        var health = go.GetComponent<EnemyHealth>();
        if (health == null) health = go.AddComponent<EnemyHealth>();

        bool hadCollider = go.GetComponentInChildren<Collider>() != null;
        if (!hadCollider)
        {
            var box = go.AddComponent<BoxCollider>();
            if (TryGetLocalBounds(go, out Bounds lb)) { box.center = lb.center; box.size = lb.size; }
        }

        var rb = go.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        var enemy = go.GetComponent<HeavyRangedEnemy>();
        if (enemy == null) enemy = go.AddComponent<HeavyRangedEnemy>();

        var so = new SerializedObject(enemy);
        var bd = so.FindProperty("baseData");
        if (bd.objectReferenceValue == null) bd.objectReferenceValue = data;
        so.FindProperty("_enemyHealth").objectReferenceValue = health;
        var pp = so.FindProperty("_projectilePrefab");
        if (pp.objectReferenceValue == null) pp.objectReferenceValue = projectile;
        var fp = so.FindProperty("_firePoint");
        if (fp.objectReferenceValue == null) fp.objectReferenceValue = CreateFirePoint(go);
        so.ApplyModifiedPropertiesWithoutUndo();

        AddIfMissingFromShip<EnemyEmerge>(shipPrefab, go);
        AddIfMissingFromShip<EnemyHealthBar>(shipPrefab, go);
        var bar = go.GetComponent<EnemyHealthBar>();
        if (bar != null)
        {
            var barSo = new SerializedObject(bar);
            var h = barSo.FindProperty("_health");
            if (h != null) h.objectReferenceValue = health;
            barSo.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static Transform CreateFirePoint(GameObject go)
    {
        var existing = go.transform.Find("FirePoint");
        if (existing != null) return existing;
        var fp = new GameObject("FirePoint").transform;
        fp.SetParent(go.transform, false);
        // adelante del modelo, a media altura
        if (TryGetLocalBounds(go, out Bounds lb))
            fp.localPosition = new Vector3(lb.center.x, lb.center.y + lb.extents.y * 0.2f, lb.max.z);
        return fp;
    }

    private static SO_EnemyData GetOrCreateData(GameObject shipPrefab)
    {
        EnsureFolder();
        var data = AssetDatabase.LoadAssetAtPath<SO_EnemyData>(DataPath);
        if (data != null) return data;
        data = ScriptableObject.CreateInstance<SO_EnemyData>();
        data.maxHealth = DefaultHealth;
        var ship = shipPrefab != null ? shipPrefab.GetComponent<ShipEnemy>() : null;
        if (ship != null && ship.baseData != null)
            data.woodExplosionVFX = ship.baseData.woodExplosionVFX;
        AssetDatabase.CreateAsset(data, DataPath);
        return data;
    }

    // ---------------- proyectil ----------------
    private static HeavyProjectile GetOrCreateProjectile()
    {
        EnsureFolder();
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePath);
        if (existing != null) return existing.GetComponent<HeavyProjectile>();

        var root = new GameObject("HeavyProjectile");

        // modelo: esfera (la escala la maneja el enemigo con Projectile Size)
        var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "Visual";
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(root.transform, false);
        visual.GetComponent<MeshRenderer>().sharedMaterial = GetOrCreateMat(ProjMatPath, "Universal Render Pipeline/Lit",
            new Color(0.12f, 0.12f, 0.14f), false);

        var col = root.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.5f;

        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var trail = root.AddComponent<TrailRenderer>();
        trail.time = 1.2f;
        trail.minVertexDistance = 0.2f;
        trail.widthMultiplier = 0.8f;
        trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.emitting = false;
        trail.sharedMaterial = GetOrCreateMat(TrailMatPath, "Universal Render Pipeline/Particles/Unlit", Color.white, true);

        var proj = root.AddComponent<HeavyProjectile>();
        var pso = new SerializedObject(proj);
        pso.FindProperty("_trail").objectReferenceValue = trail;
        pso.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, ProjectilePath);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<HeavyProjectile>();
    }

    private static Material GetOrCreateMat(string path, string shaderName, Color color, bool transparent)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        var shader = Shader.Find(shaderName) ?? Shader.Find("Sprites/Default");
        mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (transparent)
        {
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetOverrideTag("RenderType", "Transparent");
        }
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // ---------------- helpers ----------------
    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/A_SCRIPTS/Enemies", "HeavyRanged");
    }

    private static void AddIfMissingFromShip<T>(GameObject shipPrefab, GameObject target) where T : Component
    {
        if (target.GetComponent<T>() != null) return;
        var dst = target.AddComponent<T>();
        var src = shipPrefab != null ? shipPrefab.GetComponent<T>() : null;
        if (src != null) EditorUtility.CopySerialized(src, dst);
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    // bounds del modelo en el espacio local de la raíz
    private static bool TryGetLocalBounds(GameObject go, out Bounds local)
    {
        local = new Bounds(Vector3.zero, Vector3.one);
        bool any = false;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer || r is TrailRenderer) continue;
            Bounds wb = r.bounds;
            Vector3 c = wb.center, e = wb.extents;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                Vector3 lp = go.transform.InverseTransformPoint(corner);
                if (!any) { local = new Bounds(lp, Vector3.zero); any = true; }
                else local.Encapsulate(lp);
            }
        }
        return any;
    }
}
