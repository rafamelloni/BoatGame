using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Deja el prefab del Spike listo para el sistema de enemigos.
// Menú: Tools > Botes > Preparar Spike Enemy
//
// - Si YA existe SpikeEnemy.prefab: lo REPARA. Completa solo lo que falta y no toca
//   lo que ya configuraste (altura del Emerge, barra, etc.).
// - Si no existe: lo crea a partir del SpikeEnemy de la escena abierta (o el seleccionado).
//
// Lo que asegura:
//  - tag y layer del Ship (le pegan tus balas; al embestirte te hace daño + knockback)
//  - EnemyHealth + SO_SpikeEnemy (vida) asignados en SpikeBallEnemy
//  - collider y Rigidbody kinematic
//  - EnemyEmerge y EnemyHealthBar (si faltan, copiados del Ship) y el indicador de dash
//  - asignado en el campo Spike de la EnemyFactory de la escena
public static class SpikeEnemySetup
{
    private const string Folder = "Assets/A_SCRIPTS/Enemies/Spike Enenmy";
    private const string PrefabPath = Folder + "/SpikeEnemy.prefab";
    private const string DataPath = Folder + "/SO_SpikeEnemy.asset";
    private const float DefaultHealth = 300f;

    [MenuItem("Tools/Botes/Preparar Spike Enemy")]
    public static void Run()
    {
        var factory = Object.FindFirstObjectByType<EnemyFactory>(FindObjectsInactive.Include);
        GameObject shipPrefab = null;
        if (factory != null)
            shipPrefab = new SerializedObject(factory).FindProperty("_shipPrefab").objectReferenceValue as GameObject;
        if (shipPrefab == null)
            shipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/A_SCRIPTS/Enemies/ShipEnemy/Ship(Blue)01.prefab");

        SO_EnemyData data = GetOrCreateData(shipPrefab);
        GameObject prefab;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            // ---- REPARAR el existente
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Configure(root, shipPrefab, data);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Debug.Log("[SpikeEnemySetup] Prefab existente reparado (no se tocaron tus valores).");
        }
        else
        {
            // ---- CREAR desde la escena
            SpikeBallEnemy source = null;
            if (Selection.activeGameObject != null)
                source = Selection.activeGameObject.GetComponentInParent<SpikeBallEnemy>(true);
            if (source == null)
            {
                foreach (var s in Object.FindObjectsByType<SpikeBallEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (!EditorUtility.IsPersistent(s)) { source = s; break; }
            }
            if (source == null)
            {
                EditorUtility.DisplayDialog("Spike Enemy", "No hay SpikeEnemy.prefab ni un SpikeBallEnemy en la escena abierta.", "OK");
                return;
            }

            var go = Object.Instantiate(source.gameObject);
            go.name = "SpikeEnemy";
            go.SetActive(true);
            go.transform.position = Vector3.zero;
            Configure(go, shipPrefab, data);
            prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath, out bool ok);
            Object.DestroyImmediate(go);
            if (!ok || prefab == null) { Debug.LogError("[SpikeEnemySetup] No se pudo guardar el prefab."); return; }
            Debug.Log("[SpikeEnemySetup] Prefab creado.");
        }

        if (factory != null)
        {
            var fso = new SerializedObject(factory);
            fso.FindProperty("_spikePrefab").objectReferenceValue = prefab;
            fso.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(factory.gameObject.scene);
        }
        else Debug.LogWarning("[SpikeEnemySetup] No encontré EnemyFactory en la escena: arrastrá el prefab al campo Spike a mano.");

        AssetDatabase.SaveAssets();
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"[SpikeEnemySetup] Listo: {PrefabPath} (vida {data.maxHealth}). Guardá la escena y configurá Spikes en las fases.");
    }

    private static SO_EnemyData GetOrCreateData(GameObject shipPrefab)
    {
        var data = AssetDatabase.LoadAssetAtPath<SO_EnemyData>(DataPath);
        if (data != null) return data;
        data = ScriptableObject.CreateInstance<SO_EnemyData>();
        data.maxHealth = DefaultHealth;
        var ship = shipPrefab != null ? shipPrefab.GetComponent<ShipEnemy>() : null;
        if (ship != null && ship.baseData != null)
            data.woodExplosionVFX = ship.baseData.woodExplosionVFX; // misma explosión al morir
        AssetDatabase.CreateAsset(data, DataPath);
        return data;
    }

    // Completa lo que falte sin pisar lo que ya está configurado
    private static void Configure(GameObject go, GameObject shipPrefab, SO_EnemyData data)
    {
        if (shipPrefab != null)
        {
            go.tag = shipPrefab.tag;
            SetLayerRecursive(go, shipPrefab.layer);
        }
        else go.tag = "ShipEnemy";

        var health = go.GetComponent<EnemyHealth>();
        if (health == null) health = go.AddComponent<EnemyHealth>();

        var spike = go.GetComponent<SpikeBallEnemy>();
        if (spike != null)
        {
            var so = new SerializedObject(spike);
            var bd = so.FindProperty("baseData");
            if (bd != null && bd.objectReferenceValue == null) bd.objectReferenceValue = data;
            var eh = so.FindProperty("_enemyHealth");
            if (eh != null) eh.objectReferenceValue = health;
            var pl = so.FindProperty("_player");
            if (pl != null) pl.objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else Debug.LogError("[SpikeEnemySetup] El prefab no tiene SpikeBallEnemy en la raíz.");

        if (go.GetComponentInChildren<Collider>() == null)
        {
            var col = go.AddComponent<SphereCollider>();
            if (TryGetLocalBounds(go, out Vector3 center, out float radius))
            {
                col.center = center;
                col.radius = radius;
            }
            col.isTrigger = false;
        }

        var rb = go.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        if (go.GetComponent<SpikeDashTelegraph>() == null) go.AddComponent<SpikeDashTelegraph>();
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

    private static void AddIfMissingFromShip<T>(GameObject shipPrefab, GameObject target) where T : Component
    {
        if (target.GetComponent<T>() != null) return; // ya está: no se toca
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

    private static bool TryGetLocalBounds(GameObject go, out Vector3 center, out float radius)
    {
        center = Vector3.zero; radius = 1f;
        bool any = false;
        Bounds b = default;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer || r is TrailRenderer) continue; // solo el modelo
            if (!any) { b = r.bounds; any = true; }
            else b.Encapsulate(r.bounds);
        }
        if (!any) return false;

        center = go.transform.InverseTransformPoint(b.center);
        Vector3 s = go.transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(s.x), Mathf.Max(Mathf.Abs(s.y), Mathf.Abs(s.z)));
        radius = Mathf.Max(b.extents.x, b.extents.z) / Mathf.Max(0.0001f, maxScale);
        return true;
    }
}
