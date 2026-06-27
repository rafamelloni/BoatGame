using UnityEngine;
using UnityEngine.SceneManagement;

public class UpgradeDebug : MonoBehaviour
{
    [Header("Upgrades")]
    [SerializeField] private UpgradeSystem _upgradeSystem;
    [SerializeField] private SO_UpgradePath[] _paths;

    [Header("Bosses")]
    [SerializeField] private BossSequenceManager _bossSequenceManager;
    [SerializeField] private int _mortarBossIndex = 0;
    [SerializeField] private int _dashBossIndex = 1;
    [SerializeField] private int _finalBossIndex = 2;

    [Header("Player")]
    [SerializeField] private RT_PlayerHealth _playerHealth;

    [Header("UI")]
    [SerializeField] private PergaminoSlideByVisualBar _pergamino;

    private bool _infiniteHealth;
    private bool _showDebug = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            _showDebug = !_showDebug;

        if (Input.GetKeyDown(KeyCode.Alpha1)) MaxPath(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) MaxPath(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) MaxPath(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) MaxPath(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) MaxPath(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) MaxPath(5);
        if (Input.GetKeyDown(KeyCode.Alpha8)) MaxPath(6);

        if (Input.GetKeyDown(KeyCode.R)) ResetUpgrades();
        if (Input.GetKeyDown(KeyCode.F1)) _bossSequenceManager.ActivateBoss(_mortarBossIndex);
        if (Input.GetKeyDown(KeyCode.F2)) _bossSequenceManager.ActivateBoss(_dashBossIndex);
        if (Input.GetKeyDown(KeyCode.F5)) _bossSequenceManager.ActivateBoss(_finalBossIndex);
        if (Input.GetKeyDown(KeyCode.F3)) _bossSequenceManager.ResetAll();
        if (Input.GetKeyDown(KeyCode.I)) ToggleInfiniteHealth();
        if (Input.GetKeyDown(KeyCode.F9)) ReloadScene();
    }

    private void MaxPath(int index)
    {
        if (_paths == null || index >= _paths.Length || _paths[index] == null)
        {
            Debug.LogWarning($"[Debug] Path {index + 1} no asignado.");
            return;
        }
        for (int tier = 0; tier < 4; tier++)
            _upgradeSystem.ApplyUpgrade(_paths[index]);
        Debug.Log($"[Debug] Path {_paths[index].pathName} maxeado");
    }

    private void ResetUpgrades()
    {
        if (_upgradeSystem == null) return;
        _upgradeSystem.ResetAll();
        Debug.Log("[Debug] Todas las upgrades reseteadas");
    }

    private void ToggleInfiniteHealth()
    {
        if (_playerHealth == null)
        {
            Debug.LogWarning("[Debug] PlayerHealth no asignado.");
            return;
        }
        _infiniteHealth = !_infiniteHealth;
        Debug.Log($"[Debug] Vida infinita: {(_infiniteHealth ? "ON" : "OFF")}");
    }

    private void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Debug.Log("[Debug] Escena recargada");
    }

    private void OnGUI()
    {
        if (!_showDebug) return;

        GUI.Box(new Rect(10, 10, 280, 200), "DEBUG - Controles");
        GUI.Label(new Rect(20, 35, 260, 20), "1-6, 8  → Maxear upgrade path");
        GUI.Label(new Rect(20, 55, 260, 20), "R       → Reset upgrades");
        GUI.Label(new Rect(20, 75, 260, 20), "F1      → Spawn Mortar Boss");
        GUI.Label(new Rect(20, 95, 260, 20), "F2      → Spawn Dash Boss");
        GUI.Label(new Rect(20, 115, 260, 20), "F5      → Spawn Final Boss");
        GUI.Label(new Rect(20, 135, 260, 20), "F3      → Reset Bosses");
        GUI.Label(new Rect(20, 155, 260, 20), "I       → Vida infinita toggle");
        GUI.Label(new Rect(20, 175, 260, 20), "F9      → Reload escena");
        GUI.Label(new Rect(20, 195, 260, 20), "Tab     → Toggle este menu");
    }
}