using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Menú: Tools > Botes > Crear HUD de reliquias
// Arma en tu Canvas una fila de slots (uno por reliquia del RelicInventory), arriba a la derecha.
// Si no hay RelicInventory en la escena lo crea y le carga las reliquias de Assets/A_DATA/Relics.
public static class RelicHUDBuilder
{
    private const string RelicsFolder = "Assets/A_DATA/Relics";

    [MenuItem("Tools/Botes/Crear HUD de reliquias")]
    private static void Create()
    {
        // Canvas
        Canvas canvas = null;
        if (Selection.activeGameObject != null) canvas = Selection.activeGameObject.GetComponentInParent<Canvas>();
        if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Reliquias", "No hay ningún Canvas en la escena.", "Ok");
            return;
        }

        // Inventario
        var inv = Object.FindFirstObjectByType<RelicInventory>();
        if (inv == null)
        {
            var go = new GameObject("RelicInventory");
            Undo.RegisterCreatedObjectUndo(go, "Crear RelicInventory");
            inv = go.AddComponent<RelicInventory>();
        }
        var so = new SerializedObject(inv);
        var arr = so.FindProperty("_relics");
        if (arr.arraySize == 0)
        {
            var guids = AssetDatabase.FindAssets("t:SO_RelicData", new[] { RelicsFolder });
            arr.arraySize = guids.Length;
            for (int i = 0; i < guids.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<SO_RelicData>(AssetDatabase.GUIDToAssetPath(guids[i]));
            so.ApplyModifiedProperties();
        }
        var relics = inv.Relics;
        if (relics == null || relics.Length == 0)
        {
            EditorUtility.DisplayDialog("Reliquias", $"El RelicInventory no tiene reliquias y no encontré ninguna en {RelicsFolder}.", "Ok");
            return;
        }

        // Contenedor arriba a la derecha
        var rootGo = new GameObject("RelicHUD", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        Undo.RegisterCreatedObjectUndo(rootGo, "Crear HUD de reliquias");
        var root = (RectTransform)rootGo.transform;
        root.SetParent(canvas.transform, false);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(1f, 1f);
        root.anchoredPosition = new Vector2(-30f, -30f);
        root.sizeDelta = new Vector2(relics.Length * 110f, 120f);
        var layout = rootGo.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperRight;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;

        var hud = rootGo.AddComponent<RelicHUD>();
        var slots = new List<RelicHUD.Slot>();

        foreach (var relic in relics)
        {
            if (relic == null) continue;
            var slotGo = new GameObject(relic.name.Replace("SO_Relic_", "Slot_"), typeof(RectTransform), typeof(Image));
            var slot = (RectTransform)slotGo.transform;
            slot.SetParent(root, false);
            slot.sizeDelta = new Vector2(96f, 96f);
            var frame = slotGo.GetComponent<Image>();
            frame.color = new Color(0f, 0f, 0f, 0f); // invisible, solo para detectar el mouse
            frame.raycastTarget = true;

            var sil = MakeImage("Silhouette", slot, relic.icon, new Color(0f, 0f, 0f, 0.45f), 8f);
            var fill = MakeImage("Fill", slot, relic.icon, Color.white, 8f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Vertical;
            fill.fillOrigin = (int)Image.OriginVertical.Bottom;
            fill.fillAmount = 0f;

            // 3 puntitos abajo del slot
            var pips = new Image[RelicInventory.FragmentsToComplete];
            for (int i = 0; i < pips.Length; i++)
            {
                var pGo = new GameObject($"Pip{i + 1}", typeof(RectTransform), typeof(Image));
                var p = (RectTransform)pGo.transform;
                p.SetParent(slot, false);
                p.anchorMin = p.anchorMax = new Vector2(0.5f, 0f);
                p.sizeDelta = new Vector2(14f, 14f);
                p.anchoredPosition = new Vector2((i - (pips.Length - 1) * 0.5f) * 20f, -14f);
                pips[i] = pGo.GetComponent<Image>();
                pips[i].color = new Color(0f, 0f, 0f, 0.45f);
                pips[i].raycastTarget = false;
            }

            slots.Add(new RelicHUD.Slot { relic = relic, root = slot, silhouette = sil, fill = fill, pips = pips });
        }

        hud.EditorSetSlots(slots);
        EditorUtility.SetDirty(hud);
        EditorUtility.SetDirty(inv);
        EditorSceneManager.MarkSceneDirty(rootGo.scene);
        Selection.activeGameObject = rootGo;
        Debug.Log($"[RelicHUD] HUD de reliquias creado en '{canvas.name}' con {slots.Count} slots. Movelo/retocalo como quieras.");
    }

    private static Image MakeImage(string n, RectTransform parent, Sprite sprite, Color c, float inset)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Image));
        var r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(inset, inset);
        r.offsetMax = new Vector2(-inset, -inset);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = c;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }
}
