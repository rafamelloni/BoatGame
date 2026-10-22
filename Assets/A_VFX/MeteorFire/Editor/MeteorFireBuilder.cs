using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Arma el fuego de meteorito para la bala del jugador.
// Menú: Tools > Botes > Crear Fuego Meteorito en Bullet
// - Crea Assets/A_VFX/MeteorFire/ con la textura, el material y el prefab MeteorFire
// - Lo mete como hijo del prefab Bullet y lo asigna en CannonBullet._fireVfx
// Se puede correr de nuevo cuantas veces quieras: pisa lo anterior con los valores de acá.
public static class MeteorFireBuilder
{
    private const string Folder = "Assets/A_VFX/MeteorFire";
    private const string TexPath = Folder + "/FireGlow.png";
    private const string MatPath = Folder + "/MeteorFire.mat";
    private const string PrefabPath = Folder + "/MeteorFire.prefab";
    private const string BulletPath = "Assets/A_SCRIPTS/Bullets/Cannon/Bullet.prefab";
    private const string ChildName = "MeteorFire";

    [MenuItem("Tools/Botes/Crear Fuego Meteorito en Bullet")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/A_VFX", "MeteorFire");

        Texture2D tex = CreateGlowTexture();
        Material mat = CreateMaterial(tex);
        GameObject prefab = CreatePrefab(mat);
        AttachToBullet(prefab);

        AssetDatabase.SaveAssets();
        Debug.Log("[MeteorFire] Listo: prefab creado en " + PrefabPath + " y agregado a " + BulletPath);
    }

    // ---------- Textura: círculo de borde duro ----------
    private static Texture2D CreateGlowTexture()
    {
        const int size = 128;
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float c = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            // círculo sólido de borde duro (estilo toon), con 1.5 px de antialias
            float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
            float a = Mathf.Clamp01((c * 0.92f - d) / 1.5f + 0.5f);
            t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        t.Apply();
        File.WriteAllBytes(TexPath, t.EncodeToPNG());
        Object.DestroyImmediate(t);

        AssetDatabase.ImportAsset(TexPath, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(TexPath);
        imp.textureType = TextureImporterType.Default;
        imp.alphaIsTransparency = true;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.mipmapEnabled = true;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(TexPath);
    }

    // ---------- Material transparente URP ----------
    private static Material CreateMaterial(Texture2D tex)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, MatPath);
        }
        else mat.shader = shader;

        SetTex(mat, "_BaseMap", tex);
        SetTex(mat, "_MainTex", tex);
        SetColor(mat, "_BaseColor", Color.white);
        SetFloat(mat, "_Surface", 1f);   // transparente
        SetFloat(mat, "_Blend", 0f);     // alpha normal (aditivo se lava a blanco sobre el agua clara)
        SetFloat(mat, "_SrcBlend", (float)BlendMode.SrcAlpha);
        SetFloat(mat, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        SetFloat(mat, "_SrcBlendAlpha", (float)BlendMode.One);
        SetFloat(mat, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        SetFloat(mat, "_ZWrite", 0f);
        SetFloat(mat, "_Cull", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.SetOverrideTag("RenderType", "Transparent");
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ---------- Prefab con los 3 sistemas ----------
    private static GameObject CreatePrefab(Material mat)
    {
        // Raíz = cabeza de fuego pegada a la bala (Local)
        var root = new GameObject(ChildName);
        var head = root.AddComponent<ParticleSystem>();
        SetupHead(head, mat);

        // Estela que queda atrás (World, por distancia)
        var trailGo = new GameObject("FireTrail");
        trailGo.transform.SetParent(root.transform, false);
        SetupTrail(trailGo.AddComponent<ParticleSystem>(), mat);

        // Chispas
        var sparksGo = new GameObject("Sparks");
        sparksGo.transform.SetParent(root.transform, false);
        SetupSparks(sparksGo.AddComponent<ParticleSystem>(), mat);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void SetupHead(ParticleSystem ps, Material mat)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.15f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(1.0f, 1.3f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.4f, 1f), new Color(1f, 0.55f, 0.15f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 50;

        var em = ps.emission;
        em.rateOverTime = 40f;
        em.rateOverDistance = 0f;

        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = 0.05f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(MakeGradient(
            new[] { new GradientColorKey(new Color(1f, 0.95f, 0.75f), 0f), new GradientColorKey(new Color(1f, 0.6f, 0.15f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }));

        SetupRenderer(ps, mat, ParticleSystemRenderMode.Billboard, 1);
    }

    private static void SetupTrail(ParticleSystem ps, Material mat)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.0f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = Color.white;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 300;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.rateOverDistance = 20f;

        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = 0.15f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(MakeGradient(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.95f, 0.7f), 0f),
                new GradientColorKey(new Color(1f, 0.55f, 0.1f), 0.3f),
                new GradientColorKey(new Color(0.6f, 0.1f, 0.02f), 0.7f),
                new GradientColorKey(new Color(0.2f, 0.05f, 0.02f), 1f)
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) }));

        var sz = ps.sizeOverLifetime;
        sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var nz = ps.noise;
        nz.enabled = true;
        nz.strength = 0.3f;
        nz.frequency = 2f;

        SetupRenderer(ps, mat, ParticleSystemRenderMode.Billboard, 0);
    }

    private static void SetupSparks(ParticleSystem ps, Material mat)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.5f, 1f), new Color(1f, 0.5f, 0.1f, 1f));
        main.gravityModifier = 0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 100;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.rateOverDistance = 10f;

        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = 0.2f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(MakeGradient(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.4f, 0.05f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }));

        var sz = ps.sizeOverLifetime;
        sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var r = SetupRenderer(ps, mat, ParticleSystemRenderMode.Stretch, 2);
        r.velocityScale = 0.05f;
        r.lengthScale = 2.5f;
    }

    private static ParticleSystemRenderer SetupRenderer(ParticleSystem ps, Material mat, ParticleSystemRenderMode mode, int sortingFudgeOrder)
    {
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = mode;
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.sortingOrder = sortingFudgeOrder; // la cabeza se dibuja arriba de la estela
        return r;
    }

    // ---------- Engancharlo a Bullet.prefab ----------
    private static void AttachToBullet(GameObject firePrefab)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(BulletPath) == null)
        {
            Debug.LogError("[MeteorFire] No encontré " + BulletPath + ". El prefab MeteorFire quedó creado: arrastralo a mano a tu bala y asignalo en CannonBullet > Fire Vfx.");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(BulletPath);
        try
        {
            Transform old = root.transform.Find(ChildName);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(firePrefab, root.transform);
            inst.name = ChildName;
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;

            Component bullet = root.GetComponent("CannonBullet");
            if (bullet != null)
            {
                var so = new SerializedObject(bullet);
                var prop = so.FindProperty("_fireVfx");
                if (prop != null)
                {
                    prop.objectReferenceValue = inst.GetComponent<ParticleSystem>();
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                else Debug.LogWarning("[MeteorFire] CannonBullet no tiene el campo _fireVfx (¿no compiló el cambio?). Asignalo a mano.");
            }

            PrefabUtility.SaveAsPrefabAsset(root, BulletPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ---------- helpers ----------
    private static Gradient MakeGradient(GradientColorKey[] c, GradientAlphaKey[] a)
    {
        var g = new Gradient();
        g.SetKeys(c, a);
        return g;
    }

    private static void SetFloat(Material m, string n, float v) { if (m.HasProperty(n)) m.SetFloat(n, v); }
    private static void SetColor(Material m, string n, Color v) { if (m.HasProperty(n)) m.SetColor(n, v); }
    private static void SetTex(Material m, string n, Texture v) { if (m.HasProperty(n)) m.SetTexture(n, v); }
}
