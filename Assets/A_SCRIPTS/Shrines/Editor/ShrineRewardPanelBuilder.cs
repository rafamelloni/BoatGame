using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Menú: Tools > Botes > Crear panel de templo
// Arma el panel del cofre (ShrineRewardUI) adentro de un Canvas de la escena como UI normal,
// con todas las referencias conectadas, para retocarlo a mano con tus PNGs.
//  - Si tenés seleccionado un Canvas (o algo adentro de uno), lo crea ahí.
//  - Si no, usa el primer Canvas de la escena, o crea uno.
public static class ShrineRewardPanelBuilder
{
    [MenuItem("Tools/Botes/Crear panel de templo")]
    private static void Create()
    {
        Canvas canvas = null;
        if (Selection.activeGameObject != null)
            canvas = Selection.activeGameObject.GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(go, "Crear Canvas");
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        var ui = Object.FindFirstObjectByType<ShrineRewardUI>();
        if (ui == null)
        {
            var go = new GameObject("ShrineRewardUI");
            Undo.RegisterCreatedObjectUndo(go, "Crear ShrineRewardUI");
            ui = go.AddComponent<ShrineRewardUI>();
        }

        Undo.RecordObject(ui, "Crear panel de templo");
        GameObject root = ui.BuildPanelInto((RectTransform)canvas.transform);
        Undo.RegisterCreatedObjectUndo(root, "Crear panel de templo");
        root.transform.SetAsLastSibling();
        root.SetActive(false); // queda apagado; prendelo a mano para editarlo (en Play se maneja solo)

        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Debug.Log($"[ShrineRewardUI] Panel creado en '{canvas.name}'. Queda desactivado: prendelo para editarlo. En Play se oculta solo hasta que se completa un templo.");
    }
}
