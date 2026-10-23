using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tools > Botes > Crear escena Overworld
// Arma la escena entera: cámara, luz, mapa con océanos, barquito, UI y manager,
// la guarda en Assets/Scenes/Overworld.unity y la agrega a Build Settings.
public static class OverworldSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Overworld.unity";
    private const string DataFolder = "Assets/A_DATA/Overworld";
    private const string MaterialPath = DataFolder + "/OverworldOcean.mat";
    private const string LevelScene = "TestScene";
    private const string GameWaterMaterial = "Assets/ImportedAssets/Bitgem/StylisedWater/URP/Materials/WaterCirecle.mat";
    private const string OverworldWaterPath = DataFolder + "/OverworldWater.mat";

    private struct OceanDef
    {
        public string name, description, scene;
        public Color color;
        public Vector2 pos;
        public bool unlocked;
    }

    [MenuItem("Tools/Botes/Crear escena Overworld")]
    public static void Build()
    {
        if (File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog("Overworld", "Ya existe Overworld.unity. ¿La piso con una nueva?", "Pisar", "Cancelar"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Material oceanMat = GetOrCreateOceanMaterial();

        // Luz (para el barquito; el agua es unlit)
        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.65f);

        // Mapa
        var mapGo = new GameObject("OverworldMap");
        var map = mapGo.AddComponent<OverworldMap>();
        var so = new SerializedObject(map);
        so.FindProperty("_oceanMaterial").objectReferenceValue = oceanMat;
        so.FindProperty("_waterMaterial").objectReferenceValue = GetOrCreateWaterMaterial();
        so.ApplyModifiedPropertiesWithoutUndo();

        OceanRegion first = null;
        foreach (var def in GetOceans())
        {
            var go = new GameObject("Ocean_" + def.name.Replace(" ", ""));
            go.transform.SetParent(mapGo.transform, false);
            go.transform.localPosition = new Vector3(def.pos.x, 0f, def.pos.y);
            var r = go.AddComponent<OceanRegion>();
            r.displayName = def.name;
            r.description = def.description;
            r.sceneName = def.scene;
            r.unlocked = def.unlocked;
            r.color = def.color;
            if (first == null) first = r;
        }
        map.Generate();

        // Barquito
        var boatGo = new GameObject("OverworldBoat");
        boatGo.transform.position = first != null ? first.transform.position + new Vector3(0f, 0.2f, -2f) : Vector3.zero;
        var visual = new GameObject("Visual").transform;
        visual.SetParent(boatGo.transform, false);
        BuildPlaceholderBoat(visual);
        var boat = boatGo.AddComponent<OverworldBoat>();
        var boatSo = new SerializedObject(boat);
        boatSo.FindProperty("_map").objectReferenceValue = map;
        boatSo.FindProperty("_visual").objectReferenceValue = visual;
        boatSo.ApplyModifiedPropertiesWithoutUndo();

        // Cámara
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.11f, 0.12f, 0.14f);
        cam.fieldOfView = 40f;
        cam.farClipPlane = 300f;
        camGo.AddComponent<AudioListener>();
        var follow = camGo.AddComponent<OverworldCamera>();
        var camSo = new SerializedObject(follow);
        camSo.FindProperty("_target").objectReferenceValue = boatGo.transform;
        camSo.ApplyModifiedPropertiesWithoutUndo();
        camGo.transform.position = boatGo.transform.position + new Vector3(0f, 22f, -16f);
        camGo.transform.rotation = Quaternion.LookRotation(-new Vector3(0f, 22f, -16f).normalized, Vector3.up);

        // UI
        BuildUI(out var panel, out var title, out var desc, out var button, out var buttonLabel, out var fade);

        // Manager
        var managerGo = new GameObject("OverworldManager");
        var manager = managerGo.AddComponent<OverworldManager>();
        var mSo = new SerializedObject(manager);
        mSo.FindProperty("_map").objectReferenceValue = map;
        mSo.FindProperty("_boat").objectReferenceValue = boat;
        mSo.FindProperty("_promptPanel").objectReferenceValue = panel;
        mSo.FindProperty("_promptTitle").objectReferenceValue = title;
        mSo.FindProperty("_promptDescription").objectReferenceValue = desc;
        mSo.FindProperty("_sailButton").objectReferenceValue = button;
        mSo.FindProperty("_sailButtonLabel").objectReferenceValue = buttonLabel;
        mSo.FindProperty("_fade").objectReferenceValue = fade;
        mSo.FindProperty("_mainMenuScene").stringValue = LevelScene;
        mSo.ApplyModifiedPropertiesWithoutUndo();

        LayoutPanelLeft(panel, title, desc, button);
        SetupVignette(panel.root);
        fade.alpha = 0f;

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);

        Selection.activeGameObject = mapGo;
        EditorUtility.DisplayDialog("Overworld",
            "Listo: Assets/Scenes/Overworld.unity creada y agregada a Build Settings.\n\n" +
            "Mové los objetos Ocean_* (hijos de OverworldMap) para cambiar la forma del mapa.", "OK");
    }

    // Tools > Botes > Overworld: usar shader de agua
    // Para una escena Overworld ya creada: copia el material de agua del juego y se lo pone al mapa.
    [MenuItem("Tools/Botes/Overworld: usar shader de agua")]
    public static void AssignWaterToOpenMap()
    {
        var map = Object.FindFirstObjectByType<OverworldMap>();
        if (map == null)
        {
            EditorUtility.DisplayDialog("Overworld", "No encontré un OverworldMap en la escena abierta.", "OK");
            return;
        }
        var mat = GetOrCreateWaterMaterial();
        if (mat == null)
        {
            EditorUtility.DisplayDialog("Overworld", "No encontré el material de agua del juego:\n" + GameWaterMaterial, "OK");
            return;
        }
        var so = new SerializedObject(map);
        so.FindProperty("_waterMaterial").objectReferenceValue = mat;
        so.ApplyModifiedProperties();
        map.Generate();
        EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
        Selection.activeObject = mat;
    }

    private static Material GetOrCreateWaterMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(OverworldWaterPath);
        if (mat != null) return mat;
        if (AssetDatabase.LoadAssetAtPath<Material>(GameWaterMaterial) == null) return null;

        Directory.CreateDirectory(DataFolder);
        AssetDatabase.CopyAsset(GameWaterMaterial, OverworldWaterPath);
        mat = AssetDatabase.LoadAssetAtPath<Material>(OverworldWaterPath);
        if (mat != null && mat.HasProperty("_ClipRadius")) mat.SetFloat("_ClipRadius", 100000f);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }

    private const string VignettePath = "Assets/A_VFX/UI/OverworldVignette.png";

    // Tools > Botes > Overworld: UI (panel a la izquierda + viñeta)
    // Acomoda el cartel del océano a la izquierda (más alto que ancho) y agrega una imagen
    // de viñeta a pantalla completa. La viñeta se toma de Assets/A_VFX/UI/OverworldVignette.png.
    [MenuItem("Tools/Botes/Overworld: UI (panel a la izquierda + viñeta)")]
    public static void ApplyUILayout()
    {
        var manager = Object.FindFirstObjectByType<OverworldManager>();
        if (manager == null)
        {
            EditorUtility.DisplayDialog("Overworld", "No encontré un OverworldManager en la escena abierta.", "OK");
            return;
        }
        var so = new SerializedObject(manager);
        var panel = so.FindProperty("_promptPanel").objectReferenceValue as RectTransform;
        var title = so.FindProperty("_promptTitle").objectReferenceValue as TMP_Text;
        var desc = so.FindProperty("_promptDescription").objectReferenceValue as TMP_Text;
        var button = so.FindProperty("_sailButton").objectReferenceValue as Button;
        var fade = so.FindProperty("_fade").objectReferenceValue as CanvasGroup;
        if (panel == null)
        {
            EditorUtility.DisplayDialog("Overworld", "El OverworldManager no tiene asignado el panel.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(panel.root.gameObject, "UI Overworld");
        LayoutPanelLeft(panel, title, desc, button);
        bool hasImage = SetupVignette(panel.root);
        if (fade != null) fade.alpha = 0f; // en el editor no tapa nada; en Play el manager hace el fade

        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        EditorUtility.DisplayDialog("Overworld",
            hasImage
                ? "Listo: panel a la izquierda y viñeta puesta."
                : "Panel acomodado. Para la viñeta exportá tu imagen como PNG a:\n" + VignettePath + "\ny volvé a correr este menú.",
            "OK");
    }

    private static void LayoutPanelLeft(RectTransform panel, TMP_Text title, TMP_Text desc, Button button)
    {
        // Panel: pegado a la izquierda, centrado en vertical, más alto que ancho
        panel.anchorMin = panel.anchorMax = new Vector2(0f, 0.5f);
        panel.pivot = new Vector2(0f, 0.5f);
        panel.anchoredPosition = new Vector2(48f, 0f);
        panel.sizeDelta = new Vector2(440f, 720f);

        if (title != null)
        {
            var rt = title.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(32f, -150f); rt.offsetMax = new Vector2(-32f, -36f);
            title.alignment = TextAlignmentOptions.TopLeft;
            title.fontSize = 52f;
            title.enableAutoSizing = true;
            title.fontSizeMin = 30f; title.fontSizeMax = 52f;
        }
        if (desc != null)
        {
            var rt = desc.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(32f, 130f); rt.offsetMax = new Vector2(-32f, -170f);
            desc.alignment = TextAlignmentOptions.TopLeft;
            desc.fontSize = 28f;
            desc.textWrappingMode = TextWrappingModes.Normal;
        }
        if (button != null)
        {
            var rt = (RectTransform)button.transform;
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(32f, 36f); rt.offsetMax = new Vector2(-32f, 100f);
        }
    }

    private static bool SetupVignette(Transform canvasRoot)
    {
        Sprite sprite = null;
        var importer = AssetImporter.GetAtPath(VignettePath) as TextureImporter;
        if (importer != null)
        {
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(VignettePath);
        }

        var existing = canvasRoot.Find("Vignette");
        Image img;
        if (existing != null) img = existing.GetComponent<Image>();
        else
        {
            var go = new GameObject("Vignette", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "Viñeta");
            go.transform.SetParent(canvasRoot, false);
            img = go.GetComponent<Image>();
        }
        var rt = (RectTransform)img.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.SetSiblingIndex(0); // detrás del panel y del fade
        img.raycastTarget = false;
        img.preserveAspect = false;
        img.sprite = sprite;
        img.color = Color.white;
        img.enabled = sprite != null;
        return sprite != null;
    }

    private const string WakePrefabPath = "Assets/A_VFX/PARTICLES/Trail/TrailPartices.prefab";

    // Tools > Botes > Overworld: estela del barquito
    // Le pone al barquito las mismas partículas de estela que usa el barco del juego (achicadas).
    [MenuItem("Tools/Botes/Overworld: estela del barquito")]
    public static void AssignWake()
    {
        var boat = Object.FindFirstObjectByType<OverworldBoat>();
        if (boat == null)
        {
            EditorUtility.DisplayDialog("Overworld", "No encontré un OverworldBoat en la escena abierta.", "OK");
            return;
        }
        var prefab = AssetDatabase.LoadAssetAtPath<ParticleSystem>(WakePrefabPath);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Overworld", "No encontré " + WakePrefabPath + ".\nArrastrá a mano el prefab de estela al campo Wake Prefab del barquito.", "OK");
            Selection.activeObject = boat;
            return;
        }
        var so = new SerializedObject(boat);
        so.FindProperty("_wakePrefab").objectReferenceValue = prefab;
        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(boat.gameObject.scene);
        Selection.activeObject = boat;
    }

    private const string SmallWaterShader = "Botes/OverworldWater";
    private const string WaterNormalMap = "Assets/ImportedAssets/Bitgem/StylisedWater/URP/Textures/waves_n.png";

    // Tools > Botes > Overworld: material de agua propio para cada océano
    // Crea (o reusa) un material con el shader Botes/OverworldWater por cada OceanRegion,
    // teñido con su color, y lo asigna. Después tocás cada material a gusto.
    [MenuItem("Tools/Botes/Overworld: material de agua propio para cada océano")]
    public static void CreatePerOceanMaterials()
    {
        var map = Object.FindFirstObjectByType<OverworldMap>();
        if (map == null)
        {
            EditorUtility.DisplayDialog("Overworld", "No encontré un OverworldMap en la escena abierta.", "OK");
            return;
        }
        var shader = Shader.Find(SmallWaterShader);
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Overworld", "No encontré el shader " + SmallWaterShader + ". ¿Compiló bien?", "OK");
            return;
        }

        string folder = DataFolder + "/OceanWater";
        Directory.CreateDirectory(folder);
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(WaterNormalMap);

        // Material base del mapa (lo usan los océanos que no tengan propio)
        string basePath = DataFolder + "/OverworldWaterSmall.mat";
        var baseMat = AssetDatabase.LoadAssetAtPath<Material>(basePath);
        if (baseMat == null)
        {
            baseMat = new Material(shader) { name = "OverworldWaterSmall" };
            if (normal != null) baseMat.SetTexture("_NormalMap", normal);
            AssetDatabase.CreateAsset(baseMat, basePath);
        }

        int created = 0;
        foreach (var region in map.GetComponentsInChildren<OceanRegion>(true))
        {
            string path = $"{folder}/Water_{region.name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(baseMat) { name = "Water_" + region.name };
                OverworldMap.SetWaterColors(mat, region.color, Color.Lerp(region.color, Color.black, 0.6f));
                AssetDatabase.CreateAsset(mat, path);
                created++;
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
                if (normal != null) mat.SetTexture("_NormalMap", normal);
                EditorUtility.SetDirty(mat);
            }
            Undo.RecordObject(region, "Material de agua");
            region.waterMaterialOverride = mat;
            EditorUtility.SetDirty(region);
        }

        var so = new SerializedObject(map);
        so.FindProperty("_waterMaterial").objectReferenceValue = baseMat;
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        map.Generate();
        EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(folder);
        EditorUtility.DisplayDialog("Overworld", $"Listo. Materiales en {folder} ({created} nuevos). Cada océano usa el suyo.", "OK");
    }

    private static List<OceanDef> GetOceans()
    {
        var list = new List<OceanDef>
        {
            new OceanDef { name = "Mar Abierto", scene = LevelScene, unlocked = true, pos = new Vector2(0f, 0f),
                color = new Color(0.16f, 0.45f, 0.47f),
                description = "El primer mar. Sobreviví a las olas de enemigos y hundí al jefe." },
        };

        string[] names = { "Aguas Doradas", "Mar Helado", "Arrecife Esmeralda", "Mar Carmesí" };
        Color[] colors =
        {
            new Color(0.95f, 0.67f, 0.22f),
            new Color(0.80f, 0.91f, 0.95f),
            new Color(0.15f, 0.60f, 0.38f),
            new Color(0.80f, 0.27f, 0.40f),
        };
        float[] radii = { 15f, 14f, 16f, 14.5f };
        for (int i = 0; i < names.Length; i++)
        {
            float a = (i / (float)names.Length) * Mathf.PI * 2f + 0.35f;
            list.Add(new OceanDef
            {
                name = names[i], scene = "", unlocked = false, color = colors[i],
                pos = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radii[i],
                description = ""
            });
        }
        return list;
    }

    private static Material GetOrCreateOceanMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat != null) return mat;

        Directory.CreateDirectory(DataFolder);
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        mat = new Material(shader) { name = "OverworldOcean" };
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f); // dos caras
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        AssetDatabase.CreateAsset(mat, MaterialPath);
        AssetDatabase.SaveAssets();
        return mat;
    }

    private static void BuildPlaceholderBoat(Transform parent)
    {
        // Barquito temporal con primitivas: reemplazalo por el modelo que quieras (hijo de "Visual")
        var hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hull.name = "Hull";
        Object.DestroyImmediate(hull.GetComponent<Collider>());
        hull.transform.SetParent(parent, false);
        hull.transform.localScale = new Vector3(0.7f, 0.3f, 1.4f);
        hull.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        SetColor(hull, new Color(0.55f, 0.33f, 0.18f));

        var bow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bow.name = "Bow";
        Object.DestroyImmediate(bow.GetComponent<Collider>());
        bow.transform.SetParent(parent, false);
        bow.transform.localScale = new Vector3(0.5f, 0.3f, 0.5f);
        bow.transform.localPosition = new Vector3(0f, 0.15f, 0.75f);
        bow.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        SetColor(bow, new Color(0.55f, 0.33f, 0.18f));

        var mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mast.name = "Mast";
        Object.DestroyImmediate(mast.GetComponent<Collider>());
        mast.transform.SetParent(parent, false);
        mast.transform.localScale = new Vector3(0.07f, 0.55f, 0.07f);
        mast.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        SetColor(mast, new Color(0.35f, 0.22f, 0.12f));

        var sail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sail.name = "Sail";
        Object.DestroyImmediate(sail.GetComponent<Collider>());
        sail.transform.SetParent(parent, false);
        sail.transform.localScale = new Vector3(0.7f, 0.65f, 0.04f);
        sail.transform.localPosition = new Vector3(0f, 0.85f, 0.05f);
        SetColor(sail, new Color(0.96f, 0.94f, 0.88f));
    }

    private static void SetColor(GameObject go, Color c)
    {
        var r = go.GetComponent<Renderer>();
        var mat = new Material(r.sharedMaterial) { color = c };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        string path = AssetDatabase.GenerateUniqueAssetPath(DataFolder + "/Boat_" + go.name + ".mat");
        Directory.CreateDirectory(DataFolder);
        AssetDatabase.CreateAsset(mat, path);
        r.sharedMaterial = mat;
    }

    private static void BuildUI(out RectTransform panel, out TMP_Text title, out TMP_Text desc,
                                out Button button, out TMP_Text buttonLabel, out CanvasGroup fade)
    {
        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // Panel inferior
        var panelGo = new GameObject("OceanPrompt", typeof(RectTransform), typeof(Image));
        panel = panelGo.GetComponent<RectTransform>();
        panel.SetParent(canvasGo.transform, false);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 60f);
        panel.sizeDelta = new Vector2(720f, 210f);
        var panelImg = panelGo.GetComponent<Image>();
        panelImg.sprite = rounded;
        panelImg.type = Image.Type.Sliced;
        panelImg.color = new Color(0.08f, 0.09f, 0.11f, 0.88f);

        title = CreateText(panel, "Title", 46, FontStyles.Bold, new Vector2(0f, -22f), new Vector2(660f, 60f));
        title.text = "Océano";
        desc = CreateText(panel, "Description", 26, FontStyles.Normal, new Vector2(0f, -82f), new Vector2(660f, 60f));
        desc.text = "";
        desc.color = new Color(1f, 1f, 1f, 0.75f);

        var btnGo = new GameObject("SailButton", typeof(RectTransform), typeof(Image), typeof(Button));
        var btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.SetParent(panel, false);
        btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.anchoredPosition = new Vector2(0f, 20f);
        btnRt.sizeDelta = new Vector2(320f, 56f);
        var btnImg = btnGo.GetComponent<Image>();
        btnImg.sprite = rounded;
        btnImg.type = Image.Type.Sliced;
        btnImg.color = new Color(0.98f, 0.78f, 0.25f);
        button = btnGo.GetComponent<Button>();
        var colors = button.colors;
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        button.colors = colors;

        buttonLabel = CreateText(btnRt, "Label", 28, FontStyles.Bold, Vector2.zero, Vector2.zero);
        var lrt = buttonLabel.rectTransform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        lrt.pivot = new Vector2(0.5f, 0.5f);
        buttonLabel.color = new Color(0.12f, 0.1f, 0.08f);
        buttonLabel.text = "Zarpar  [Espacio]";

        // Ayuda arriba a la izquierda
        var help = CreateText(canvasGo.transform as RectTransform, "Help", 24, FontStyles.Normal, Vector2.zero, new Vector2(700f, 40f));
        var hrt = help.rectTransform;
        hrt.anchorMin = hrt.anchorMax = new Vector2(0f, 1f);
        hrt.pivot = new Vector2(0f, 1f);
        hrt.anchoredPosition = new Vector2(30f, -24f);
        help.alignment = TextAlignmentOptions.Left;
        help.color = new Color(1f, 1f, 1f, 0.6f);
        help.text = "WASD navegar  ·  Espacio zarpar  ·  Esc volver al menú";

        // Fade
        var fadeGo = new GameObject("Fade", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        var frt = fadeGo.GetComponent<RectTransform>();
        frt.SetParent(canvasGo.transform, false);
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = frt.offsetMax = Vector2.zero;
        fadeGo.GetComponent<Image>().color = Color.black;
        fade = fadeGo.GetComponent<CanvasGroup>();
        fade.alpha = 1f;

        panelGo.SetActive(false);
    }

    private static TMP_Text CreateText(RectTransform parent, string name, float size, FontStyles style, Vector2 pos, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        var t = go.GetComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    private static void AddToBuildSettings(string path)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        var existing = scenes.FirstOrDefault(s => s.path == path);
        if (existing != null)
        {
            existing.enabled = true;
        }
        else
        {
            scenes.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
