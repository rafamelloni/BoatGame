#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tools > Botes > Crear escena MainMenu
// Arma Assets/Scenes/MainMenu.unity copiando de la escena del juego: el agua (WORLD), la luz,
// el Global Volume, la cámara (fija, con la misma rotación y offset que en el juego), el barco
// (solo con lo visual + Movement/olas/velas, manejado por MenuBoatAutopilot) y la UI del menú.
// La escena del juego no se modifica. Se puede volver a correr para regenerarla.
public static class MainMenuSceneBuilder
{
    private const string GameScenePath = "Assets/Scenes/TestScene.unity";
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";

    // Componentes que se quedan en la raíz del barco. Todo lo demás (vida, cañones, upgrades...) se saca.
    private static readonly HashSet<string> BoatKeep = new HashSet<string>
    {
        "Movement", "PlayerWaveMovement", "RT_PlayerStats", "SailController",
    };

    [MenuItem("Tools/Botes/Crear escena MainMenu")]
    public static void Build()
    {
        if (!File.Exists(GameScenePath))
        {
            EditorUtility.DisplayDialog("MainMenu", "No encontré " + GameScenePath, "OK");
            return;
        }
        if (File.Exists(MenuScenePath) &&
            !EditorUtility.DisplayDialog("MainMenu", "Ya existe MainMenu.unity. ¿La piso con una nueva?", "Pisar", "Cancelar"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene game = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        Scene menu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        CopyLighting(game, menu);

        // --- Mundo ---
        Clone(game, menu, "Directional Light");
        Clone(game, menu, "Global Volume");
        Clone(game, menu, "XX"); // EventSystem
        GameObject world = Clone(game, menu, "WORLD");
        if (world != null)
        {
            foreach (var zone in world.GetComponentsInChildren<CircularDamageZone>(true))
                Object.DestroyImmediate(zone.gameObject);
        }

        // --- Barco ---
        GameObject boat = Clone(game, menu, "BOAT");
        if (boat == null) { Abort(game, menu, "No encontré el objeto BOAT en la escena del juego."); return; }
        StripBoat(boat);

        // --- Cámara fija, igual que la del juego ---
        GameObject camGo = Clone(game, menu, "Main Camera");
        if (camGo == null) { Abort(game, menu, "No encontré la Main Camera en la escena del juego."); return; }
        camGo.SetActive(true);
        camGo.tag = "MainCamera";
        var follow = camGo.GetComponent<followPlayer>();
        Vector3 offset = follow != null ? follow.offset : new Vector3(0f, 20f, -18f);
        if (follow != null) Object.DestroyImmediate(follow);
        Vector3 boatPos = boat.transform.position;
        boatPos.y = 7.49f; // la misma altura que fija Movement.SetMovementEnabled
        camGo.transform.position = boatPos + offset;
        Camera cam = camGo.GetComponent<Camera>();

        var pilot = boat.AddComponent<MenuBoatAutopilot>();
        SetRef(pilot, "_camera", cam);

        // --- UI del menú ---
        BuildMenuUI(game, menu, cam);

        // Nada de la escena nueva puede apuntar a la del juego
        ClearCrossSceneReferences(menu);

        SceneManager.SetActiveScene(menu);
        bool saved = EditorSceneManager.SaveScene(menu, MenuScenePath);
        EditorSceneManager.CloseScene(game, true);

        if (!saved)
        {
            EditorUtility.DisplayDialog("MainMenu", "No se pudo guardar " + MenuScenePath, "OK");
            return;
        }

        UpdateBuildSettings();

        EditorUtility.DisplayDialog("MainMenu",
            "Listo: Assets/Scenes/MainMenu.unity creada y puesta primera en Build Settings.\n\n" +
            "Dale Play desde esta escena para probar. El barco se maneja con MenuBoatAutopilot " +
            "(márgenes de pantalla, velocidad, giro) y Play carga TestScene (MainMenuController).", "OK");
    }

    // ---------------------------------------------------------------- barco

    private static void StripBoat(GameObject boat)
    {
        // Raíz: solo lo que hace falta para moverse y verse igual que en el juego
        for (int pass = 0; pass < 4; pass++)
        {
            foreach (var mb in boat.GetComponents<MonoBehaviour>())
            {
                if (mb == null) continue;
                if (!BoatKeep.Contains(mb.GetType().Name))
                    Object.DestroyImmediate(mb);
            }
        }
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(boat);

        // Hijos: efectos de muerte / fuego de daño / level up no se necesitan
        string[] removeChildren = { "PuffUpgradeParticleSystem", "WoodExplosionVFXDeath", "ExplosionDeath", "FireVelas", "FireVelas (1)", "FireBoat (1)" };
        foreach (Transform child in boat.transform.Cast<Transform>().ToList())
        {
            if (removeChildren.Contains(child.name))
                Object.DestroyImmediate(child.gameObject);
        }

        // Sprint apagado: el menú no usa la barra de sprint
        var movement = boat.GetComponent<Movement>();
        if (movement != null) movement.sprintBarImage = null;
    }

    // ---------------------------------------------------------------- UI

    private static void BuildMenuUI(Scene game, Scene menu, Camera cam)
    {
        GameObject canvasGo = Clone(game, menu, "Canvas");
        if (canvasGo == null) { Debug.LogWarning("[MainMenu] No encontré el Canvas del juego, la escena queda sin UI."); return; }

        // Sacamos el HUD del juego: solo queda el objeto "Menu"
        Transform menuUi = canvasGo.transform.Find("Menu");
        foreach (Transform child in canvasGo.transform.Cast<Transform>().ToList())
        {
            if (child != menuUi) Object.DestroyImmediate(child.gameObject);
        }
        foreach (var mb in canvasGo.GetComponents<MonoBehaviour>())
        {
            if (mb == null) continue;
            if (mb is CanvasScaler || mb is GraphicRaycaster) continue;
            if (mb.GetType().Namespace != null && mb.GetType().Namespace.StartsWith("UnityEngine")) continue;
            Object.DestroyImmediate(mb);
        }
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(canvasGo);

        var canvas = canvasGo.GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) canvas.worldCamera = cam;

        if (menuUi == null)
        {
            Debug.LogWarning("[MainMenu] El Canvas no tiene un hijo 'Menu'. Agregá los botones a mano y conectalos a MainMenuController.");
            return;
        }

        menuUi.gameObject.SetActive(true);
        var oldManager = menuUi.GetComponent<MenuManager>();
        if (oldManager != null) Object.DestroyImmediate(oldManager);

        var controller = menuUi.gameObject.AddComponent<MainMenuController>();
        var sequence = menuUi.gameObject.AddComponent<MenuPlaySequence>();
        SetRef(controller, "_sequence", sequence);
        SetRef(sequence, "_menuPanel", menuUi as RectTransform);
        SetRef(sequence, "_camera", cam);
        SetRef(sequence, "_boat", Object.FindObjectsByType<MenuBoatAutopilot>(FindObjectsSortMode.None).FirstOrDefault(p => p.gameObject.scene == menu));

        // Botón Play: lo que antes llamaba a MenuManager.PlayButtonOnClick ahora llama a MainMenuController.Play
        int rewired = 0;
        foreach (var button in menuUi.GetComponentsInChildren<Button>(true))
        {
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                if (button.onClick.GetPersistentMethodName(i) != "PlayButtonOnClick") continue;
                UnityEventTools.RemovePersistentListener(button.onClick, i);
                UnityEventTools.AddPersistentListener(button.onClick, controller.Play);
                rewired++;
            }
        }
        if (rewired == 0)
            Debug.LogWarning("[MainMenu] No encontré el botón de Play. Conectalo a mano a MainMenuController.Play.");

        // Fundido a negro al darle Play
        var fadeGo = new GameObject("Fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        fadeGo.transform.SetParent(canvasGo.transform, false);
        var rt = (RectTransform)fadeGo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = fadeGo.GetComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = true;
        var group = fadeGo.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
        fadeGo.transform.SetAsLastSibling();
        SetRef(controller, "_fade", group);
    }

    // ---------------------------------------------------------------- helpers

    private static GameObject Clone(Scene src, Scene dst, string rootName)
    {
        GameObject original = src.GetRootGameObjects().FirstOrDefault(g => g.name == rootName);
        if (original == null)
        {
            Debug.LogWarning($"[MainMenu] No encontré '{rootName}' en {src.name}.");
            return null;
        }
        GameObject copy = Object.Instantiate(original);
        copy.name = original.name;
        SceneManager.MoveGameObjectToScene(copy, dst);
        copy.transform.SetPositionAndRotation(original.transform.position, original.transform.rotation);
        return copy;
    }

    private static void CopyLighting(Scene src, Scene dst)
    {
        SceneManager.SetActiveScene(src);
        var ambientMode = RenderSettings.ambientMode;
        var sky = RenderSettings.ambientSkyColor;
        var equator = RenderSettings.ambientEquatorColor;
        var ground = RenderSettings.ambientGroundColor;
        var ambientIntensity = RenderSettings.ambientIntensity;
        var skybox = RenderSettings.skybox;
        var fog = RenderSettings.fog;
        var fogColor = RenderSettings.fogColor;
        var fogMode = RenderSettings.fogMode;
        var fogDensity = RenderSettings.fogDensity;
        var fogStart = RenderSettings.fogStartDistance;
        var fogEnd = RenderSettings.fogEndDistance;
        var reflectionMode = RenderSettings.defaultReflectionMode;
        var reflectionIntensity = RenderSettings.reflectionIntensity;
        var subtractive = RenderSettings.subtractiveShadowColor;
        LightingSettings lighting = null;
        try { lighting = Lightmapping.lightingSettings; } catch { }

        SceneManager.SetActiveScene(dst);
        RenderSettings.ambientMode = ambientMode;
        RenderSettings.ambientSkyColor = sky;
        RenderSettings.ambientEquatorColor = equator;
        RenderSettings.ambientGroundColor = ground;
        RenderSettings.ambientIntensity = ambientIntensity;
        RenderSettings.skybox = skybox;
        RenderSettings.fog = fog;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = fogMode;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;
        RenderSettings.defaultReflectionMode = reflectionMode;
        RenderSettings.reflectionIntensity = reflectionIntensity;
        RenderSettings.subtractiveShadowColor = subtractive;
        if (lighting != null) Lightmapping.lightingSettings = lighting;
    }

    private static void ClearCrossSceneReferences(Scene scene)
    {
        int cleared = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Component c in root.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;
                var so = new SerializedObject(c);
                var prop = so.GetIterator();
                bool changed = false;
                while (prop.Next(true))
                {
                    if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                    Object value = prop.objectReferenceValue;
                    Scene valueScene = default;
                    if (value is GameObject go) valueScene = go.scene;
                    else if (value is Component comp) valueScene = comp.gameObject.scene;
                    else continue;

                    if (valueScene.IsValid() && valueScene != scene)
                    {
                        prop.objectReferenceValue = null;
                        changed = true;
                        cleared++;
                    }
                }
                if (changed) so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        if (cleared > 0) Debug.Log($"[MainMenu] Limpié {cleared} referencias que apuntaban a la escena del juego.");
    }

    private static void SetRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null) return;
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void UpdateBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(MenuScenePath, true) };
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.path == MenuScenePath || !File.Exists(s.path)) continue; // saca las que ya no existen
            scenes.Add(s);
        }
        if (!scenes.Any(s => s.path == GameScenePath))
            scenes.Add(new EditorBuildSettingsScene(GameScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
    }

    private static void Abort(Scene game, Scene menu, string msg)
    {
        EditorSceneManager.CloseScene(menu, true);
        EditorUtility.DisplayDialog("MainMenu", msg, "OK");
    }
}
#endif
