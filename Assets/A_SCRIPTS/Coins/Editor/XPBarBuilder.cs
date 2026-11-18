// XPBarBuilder.cs — herramienta de una sola vez: Tools > Botes > Armar barra de XP
// Arma la barra nueva (Background, Ghost, Fill, Frame, LevelBadge) alrededor del Fill
// que ya usa CoinBarUI y conecta los campos. Se deshace con Ctrl+Z.
// Una vez que la escena quedó guardada, podés borrar este archivo.
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class XPBarBuilder
{
    private const string Dir = "Assets/PNGs/XPBar/";

    [MenuItem("Tools/Botes/Armar barra de XP")]
    private static void Build()
    {
        var coinBar = Object.FindFirstObjectByType<CoinBarUI>(FindObjectsInactive.Include);
        if (coinBar == null)
        {
            EditorUtility.DisplayDialog("Barra de XP", "No encontré un CoinBarUI en la escena abierta. Abrí TestScene y probá de nuevo.", "OK");
            return;
        }

        var so = new SerializedObject(coinBar);
        var fill = so.FindProperty("_barImage").objectReferenceValue as Image;
        var level = so.FindProperty("_barLevel").objectReferenceValue as TextMeshProUGUI;

        if (fill == null)
        {
            EditorUtility.DisplayDialog("Barra de XP", "El CoinBarUI no tiene asignada la Bar Image.", "OK");
            return;
        }
        if (so.FindProperty("_barRoot").objectReferenceValue != null)
        {
            EditorUtility.DisplayDialog("Barra de XP", "La barra ya está armada (Bar Root ya tiene algo asignado).", "OK");
            return;
        }

        Sprite bgSprite = Load("XPBar_Background");
        Sprite whiteSprite = Load("XPBar_Fill_White");
        Sprite frameSprite = Load("XPBar_Frame");
        Sprite badgeSprite = Load("XPBar_LevelBadge");
        if (bgSprite == null || whiteSprite == null || frameSprite == null || badgeSprite == null)
        {
            EditorUtility.DisplayDialog("Barra de XP", "Faltan los sprites en " + Dir, "OK");
            return;
        }

        Undo.SetCurrentGroupName("Armar barra de XP");
        int group = Undo.GetCurrentGroup();

        RectTransform fillRT = fill.rectTransform;
        Transform parent = fillRT.parent;

        // Tamaño que se ve hoy (la barra vieja tiene escala no uniforme), con la proporción de los PNG nuevos (16:1)
        float width = fillRT.rect.width * fillRT.localScale.x;
        float height = width / 16f;
        Vector3 worldCenter = fillRT.TransformPoint(fillRT.rect.center);

        // Raíz: lo que rebota con cada moneda
        var rootGO = new GameObject("XPBar", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(rootGO, "XPBar");
        var root = (RectTransform)rootGO.transform;
        root.SetParent(parent, false);
        root.SetSiblingIndex(fillRT.GetSiblingIndex());
        root.anchorMin = fillRT.anchorMin;
        root.anchorMax = fillRT.anchorMax;
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(width, height);
        root.localScale = Vector3.one;
        root.position = worldCenter;

        // Capas de abajo hacia arriba
        MakeLayer("Background", bgSprite, false, root);
        Image ghost = MakeLayer("Ghost", whiteSprite, true, root);
        ghost.color = new Color32(255, 244, 200, 255);

        // Los hijos viejos de XPBarra (Fondo, XPBorde) quedan apagados, no borrados
        foreach (Transform child in fillRT)
        {
            Undo.RecordObject(child.gameObject, "Apagar viejo");
            child.gameObject.SetActive(false);
        }

        // El Fill que ya usan CoinBarUI y el pergamino pasa adentro de la raíz
        Undo.SetTransformParent(fillRT, root, "Mover Fill");
        Undo.RecordObject(fillRT, "Fill");
        Undo.RecordObject(fill, "Fill");
        fillRT.localScale = Vector3.one;
        fillRT.localRotation = Quaternion.identity;
        Stretch(fillRT);
        fillRT.SetAsLastSibling();
        fill.sprite = whiteSprite;
        fill.color = Color.white;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.raycastTarget = false;

        MakeLayer("Frame", frameSprite, false, root);

        // Círculo del nivel, sobre la punta izquierda
        float d = height * 1.75f;
        var badgeGO = new GameObject("LevelBadge", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(badgeGO, "LevelBadge");
        var badge = (RectTransform)badgeGO.transform;
        badge.SetParent(root, false);
        badge.anchorMin = badge.anchorMax = new Vector2(0f, 0.5f);
        badge.pivot = new Vector2(0.5f, 0.5f);
        badge.sizeDelta = new Vector2(d, d);
        badge.anchoredPosition = new Vector2(height * 0.2f, 0f);
        var badgeImg = badgeGO.GetComponent<Image>();
        badgeImg.sprite = badgeSprite;
        badgeImg.preserveAspect = true;
        badgeImg.raycastTarget = false;

        // El texto de nivel va adentro del círculo
        if (level != null)
        {
            Undo.SetTransformParent(level.transform, badge, "Mover Level");
            RectTransform lrt = level.rectTransform;
            Undo.RecordObject(lrt, "Level");
            Undo.RecordObject(level, "Level");
            lrt.localScale = Vector3.one;
            lrt.localRotation = Quaternion.identity;
            Stretch(lrt);
            level.alignment = TextAlignmentOptions.Center;
            level.color = new Color32(15, 15, 15, 255);
            level.enableAutoSizing = true;
            level.fontSizeMin = 8f;
            level.fontSizeMax = 500f;
            level.margin = new Vector4(d * 0.2f, d * 0.15f, d * 0.2f, d * 0.15f);
            level.raycastTarget = false;
        }

        // Conectar CoinBarUI
        so.Update();
        so.FindProperty("_ghostImage").objectReferenceValue = ghost;
        so.FindProperty("_barRoot").objectReferenceValue = root;
        so.FindProperty("_badge").objectReferenceValue = badge;
        so.ApplyModifiedProperties();

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(coinBar.gameObject.scene);
        Selection.activeGameObject = rootGO;

        EditorUtility.DisplayDialog("Barra de XP", "Listo. Revisá la posición y el tamaño de 'XPBar' y guardá la escena (Ctrl+S).", "OK");
    }

    private static Image MakeLayer(string name, Sprite sprite, bool filled, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, name);
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        Stretch(rt);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        if (filled)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = 0f;
        }
        return img;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Dir + name + ".png");
}
