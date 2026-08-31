using UnityEngine;
using UnityEngine.SceneManagement;
public class UpgradeDebug : MonoBehaviour
{
    [Header("Upgrades")]
    [SerializeField] private UpgradeSystem _upgradeSystem;
    [SerializeField] private SO_UpgradePath[] _paths;
    [SerializeField] private RT_PlayerUpgrades _playerUpgrades;
    [Header("Cannon Debug")]
    [SerializeField] private AbilityController _abilityController;
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
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (Input.GetKeyDown(KeyCode.Alpha1)) { if (shift) MaxPath(0); else StepPath(0); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { if (shift) MaxPath(1); else StepPath(1); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { if (shift) MaxPath(2); else StepPath(2); }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { if (shift) MaxPath(3); else StepPath(3); }
        if (Input.GetKeyDown(KeyCode.Alpha5)) { if (shift) MaxPath(4); else StepPath(4); }
        if (Input.GetKeyDown(KeyCode.Alpha6)) { if (shift) MaxPath(5); else StepPath(5); }
        if (Input.GetKeyDown(KeyCode.Alpha7)) { if (shift) MaxPath(6); else StepPath(6); }
        if (Input.GetKeyDown(KeyCode.R)) ResetUpgrades();
        if (Input.GetKeyDown(KeyCode.F1)) _bossSequenceManager.ActivateBoss(_mortarBossIndex);
        if (Input.GetKeyDown(KeyCode.F2)) _bossSequenceManager.ActivateBoss(_dashBossIndex);
        if (Input.GetKeyDown(KeyCode.F3)) _bossSequenceManager.ActivateBoss(_finalBossIndex);
        if (Input.GetKeyDown(KeyCode.F5)) _bossSequenceManager.ResetAll();
        if (Input.GetKeyDown(KeyCode.F9)) ReloadScene();
    }
    private void StepPath(int index)
    {
        if (_paths == null || index >= _paths.Length || _paths[index] == null)
        {
            Debug.LogWarning($"[Debug] Path {index + 1} no asignado.");
            return;
        }
        _upgradeSystem.ApplyUpgrade(_paths[index]);
        Debug.Log($"[Debug] Path {_paths[index].pathName} +1 tier");
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
    private void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Debug.Log("[Debug] Escena recargada");
    }
    private void OnGUI()
    {
        if (!_showDebug) return;
        GUI.Box(new Rect(10, 10, 280, 460), "DEBUG - Controles");
        GUI.Label(new Rect(20, 35, 260, 20), "1-7     → +1 tier al path");
        GUI.Label(new Rect(20, 55, 260, 20), "Shift+1-7 → Maxear path");
        GUI.Label(new Rect(20, 75, 260, 20), "R       → Reset upgrades");
        GUI.Label(new Rect(20, 95, 260, 20), "F1      → Spawn Mortar Boss");
        GUI.Label(new Rect(20, 115, 260, 20), "F2      → Spawn Dash Boss");
        GUI.Label(new Rect(20, 135, 260, 20), "F3      → Spawn Final Boss");
        GUI.Label(new Rect(20, 155, 260, 20), "F5      → Reset Bosses");
        GUI.Label(new Rect(20, 175, 260, 20), "F9      → Reload escena");
        GUI.Label(new Rect(20, 195, 260, 20), "Tab     → Toggle este menu");

        if (_abilityController != null && _abilityController.CannonAbility != null)
        {
            var data = _abilityController.CannonAbility._rtData;
            bool hasChargedShot = _playerUpgrades != null && _playerUpgrades.HasAbility(SpecialAbilityType.ChargedShot);

            GUI.Box(new Rect(20, 220, 260, 230), "");
            GUI.Label(new Rect(30, 225, 260, 20), "Cannon stats en vivo:");
            GUI.Label(new Rect(30, 245, 260, 20), $"Damage: {data.damage:F2}");
            GUI.Label(new Rect(30, 265, 260, 20), $"Cooldown: {data.cooldown:F2}");
            GUI.Label(new Rect(30, 285, 260, 20), $"TimeBetweenShots: {data.timeBetweenShots:F2}");
            GUI.Label(new Rect(30, 305, 260, 20), $"ShotsPerBurst: {data.shotsPerBurst}");
            GUI.Label(new Rect(30, 325, 260, 20), $"ExplosionRadius: {data.explosionRadius:F2}");
            GUI.Label(new Rect(30, 345, 260, 20), $"ChargedShot: {(hasChargedShot ? "Unlocked" : "Locked")}");
            GUI.Label(new Rect(30, 365, 260, 20), $"  Intervalo: cada {data.chargedShotInterval} disparos");
            GUI.Label(new Rect(30, 385, 260, 20), $"  Escala bala: x{data.chargedBulletScale:F2}");
            GUI.Label(new Rect(30, 405, 260, 20), $"  Mult. daño: x{data.chargedDamageMultiplier:F2}");
            GUI.Label(new Rect(30, 425, 260, 20), $"  Mult. explosión: x{data.chargedExplosionMultiplier:F2}");
        }

        if (_playerUpgrades != null && _paths != null)
        {
            GUI.Box(new Rect(300, 10, 220, 20 + _paths.Length * 20), "");
            for (int i = 0; i < _paths.Length; i++)
            {
                if (_paths[i] == null) continue;
                int level = _playerUpgrades.GetLevel(_paths[i]);
                GUI.Label(new Rect(310, 15 + i * 20, 200, 20), $"{i + 1}. {_paths[i].pathName}: {level}/4");
            }
        }
    }
}