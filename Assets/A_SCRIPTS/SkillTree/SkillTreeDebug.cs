using UnityEngine;

// Harness de prueba para validar el mecanismo del arbol (subir nivel ->
// desbloquear nodo rojo) sin todavia tener UI ni efectos de gameplay.
// Mismo estilo que UpgradeDebug.cs.
public class SkillTreeDebug : MonoBehaviour
{
    [Header("Arbol")]
    [SerializeField] private RT_SkillTree _tree;

    [Header("Nodos de prueba")]
    [SerializeField] private SO_NormalNode[] _normalNodes;
    [SerializeField] private SO_SpecialNode[] _specialNodes;

    private bool _showDebug = true;
    private int _selectedNormalIndex = 0;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            _showDebug = !_showDebug;

        if (_normalNodes == null || _normalNodes.Length == 0) return;

        if (Input.GetKeyDown(KeyCode.RightArrow))
            _selectedNormalIndex = (_selectedNormalIndex + 1) % _normalNodes.Length;
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            _selectedNormalIndex = (_selectedNormalIndex - 1 + _normalNodes.Length) % _normalNodes.Length;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            LevelUpSelected();

        if (Input.GetKeyDown(KeyCode.U))
            TryUnlockAllSpecials();

        if (Input.GetKeyDown(KeyCode.R))
            ResetTree();
    }

    private void LevelUpSelected()
    {
        var node = _normalNodes[_selectedNormalIndex];
        if (node == null) return;

        bool leveled = _tree.LevelUp(node);
        Debug.Log(leveled
            ? $"[SkillTreeDebug] {node.displayName} -> nivel {_tree.GetLevel(node)}"
            : $"[SkillTreeDebug] {node.displayName} ya esta al maximo ({node.maxLevel})");
    }

    private void TryUnlockAllSpecials()
    {
        if (_specialNodes == null) return;

        foreach (var special in _specialNodes)
        {
            if (special == null || _tree.IsSpecialUnlocked(special)) continue;
            if (_tree.UnlockSpecial(special))
                Debug.Log($"[SkillTreeDebug] Desbloqueado nodo especial: {special.displayName}");
        }
    }

    private void ResetTree()
    {
        _tree.ResetAll();
        Debug.Log("[SkillTreeDebug] Arbol reseteado");
    }

    private void OnGUI()
    {
        if (!_showDebug) return;

        GUI.Box(new Rect(10, 10, 340, 280), "DEBUG - Skill Tree");
        GUI.Label(new Rect(20, 35, 320, 20), "<- / ->    Elegir nodo normal");
        GUI.Label(new Rect(20, 55, 320, 20), "Enter      Subir nivel del nodo elegido");
        GUI.Label(new Rect(20, 75, 320, 20), "U          Intentar desbloquear especiales");
        GUI.Label(new Rect(20, 95, 320, 20), "R          Reset arbol");
        GUI.Label(new Rect(20, 115, 320, 20), "Tab        Toggle este menu");

        int y = 145;
        if (_normalNodes != null)
        {
            for (int i = 0; i < _normalNodes.Length; i++)
            {
                var node = _normalNodes[i];
                if (node == null) continue;
                string marker = i == _selectedNormalIndex ? ">> " : "   ";
                GUI.Label(new Rect(20, y, 320, 20),
                    $"{marker}{node.displayName}: {_tree.GetLevel(node)}/{node.maxLevel}");
                y += 18;
            }
        }

        y += 10;
        if (_specialNodes != null)
        {
            foreach (var special in _specialNodes)
            {
                if (special == null) continue;
                string state = _tree.IsSpecialUnlocked(special) ? "DESBLOQUEADO"
                    : _tree.IsSpecialUnlockable(special) ? "listo para desbloquear (U)"
                    : "bloqueado";
                GUI.Label(new Rect(20, y, 320, 20), $"[{special.displayName}] {state}");
                y += 18;
            }
        }
    }
}
