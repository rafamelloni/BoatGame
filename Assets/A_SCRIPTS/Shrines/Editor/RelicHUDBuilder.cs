using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menús:
//  Tools > Botes > Cargar reliquias en el inventario
//      Llena el array del RelicInventory con todas las reliquias de Assets/A_DATA/Relics
//      (menos Ancla/Timón/Ron, que eran del sistema viejo). Si no hay RelicInventory lo crea.
//  Tools > Botes > Crear HUD de reliquias
//      Arma en tu Canvas el contenedor del HUD arriba a la derecha (los slots se crean solos en runtime).
public static class RelicHUDBuilder
{
    [MenuItem("Tools/Botes/Cargar reliquias en el inventario")]
    private static void LoadRelics()
    {
        var inv = EnsureInventory();
        var relics = RelicInventory.LoadAllFromProject();
        Undo.RecordObject(inv, "Cargar reliquias");
        inv.EditorSetRelics(relics);
        EditorUtility.SetDirty(inv);
        EditorSceneManager.MarkSceneDirty(inv.gameObject.scene);
        Selection.activeGameObject = inv.gameObject;
        Debug.Log($"[Reliquias] Cargué {relics.Length} reliquias en '{inv.name}'. Guardá la escena.");
    }

    [MenuItem("Tools/Botes/Crear HUD de reliquias")]
    private static void CreateHUD()
    {
        Canvas canvas = null;
        if (Selection.activeGameObject != null) canvas = Selection.activeGameObject.GetComponentInParent<Canvas>();
        if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Reliquias", "No hay ningún Canvas en la escena.", "Ok");
            return;
        }

        var existing = Object.FindFirstObjectByType<RelicHUD>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorUtility.DisplayDialog("Reliquias", $"Ya hay un HUD de reliquias ('{existing.name}'). Lo dejé seleccionado.", "Ok");
            return;
        }

        var inv = EnsureInventory();
        bool hasNew = false;
        if (inv.Relics != null) foreach (var r in inv.Relics) if (r != null && !RelicEffects.IsLegacy(r.effectType)) hasNew = true;
        if (!hasNew) LoadRelics();

        var rootGo = new GameObject("RelicHUD", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(rootGo, "Crear HUD de reliquias");
        var root = (RectTransform)rootGo.transform;
        root.SetParent(canvas.transform, false);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(1f, 1f);
        root.anchoredPosition = new Vector2(-30f, -30f);
        root.sizeDelta = new Vector2(8 * 96f, 200f);
        rootGo.AddComponent<RelicHUD>();

        EditorSceneManager.MarkSceneDirty(rootGo.scene);
        Selection.activeGameObject = rootGo;
        Debug.Log($"[Reliquias] HUD creado en '{canvas.name}'. Las reliquias aparecen desde la esquina de arriba a la derecha.");
    }

    private static RelicInventory EnsureInventory()
    {
        var inv = Object.FindFirstObjectByType<RelicInventory>();
        if (inv != null) return inv;
        var go = new GameObject("RelicInventory");
        Undo.RegisterCreatedObjectUndo(go, "Crear RelicInventory");
        return go.AddComponent<RelicInventory>();
    }
}
