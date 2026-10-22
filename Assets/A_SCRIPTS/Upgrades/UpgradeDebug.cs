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
    [Header("Ship Tiers Debug")]
    [SerializeField] private UpgradeShipBOSS _upgradeShip;
    [SerializeField] private float _shootPointRayLength = 4f;

    private bool _infiniteHealth;
    private bool _showDebug = false;
    private ShipHardpoints _hardpoints;
    private GUIStyle _richStyle;

    private void Awake()
    {
        if (_abilityController != null)
            _hardpoints = _abilityController.GetComponent<ShipHardpoints>();
    }

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

        if (Input.GetKeyDown(KeyCode.R)) ResetUpgrades();
        if (Input.GetKeyDown(KeyCode.F1)) _bossSequenceManager.ActivateBoss(_mortarBossIndex);
        if (Input.GetKeyDown(KeyCode.F2)) _bossSequenceManager.ActivateBoss(_dashBossIndex);
        if (Input.GetKeyDown(KeyCode.F3)) _bossSequenceManager.ActivateBoss(_finalBossIndex);
        if (Input.GetKeyDown(KeyCode.F5)) _bossSequenceManager.ResetAll();

        if (_upgradeShip != null)
        {
            if (Input.GetKeyDown(KeyCode.F6)) _upgradeShip.DebugSetTier(0);
            if (Input.GetKeyDown(KeyCode.F7)) _upgradeShip.DebugAnimateToTier(1);
            if (Input.GetKeyDown(KeyCode.F8)) _upgradeShip.DebugAnimateToTier(2);
        }

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

    // ——— Ship tiers / shoot points ———

    // Positivo = el shoot point apunta hacia la derecha del barco.
    private float SideOf(Transform p)
    {
        Vector3 fwd = p.forward; fwd.y = 0f;
        return Vector3.Dot(fwd.normalized, _hardpoints.transform.right);
    }

    private void DrawShipPanel()
    {
        if (_hardpoints == null) return;
        if (_richStyle == null)
            _richStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true };

        GUILayout.BeginArea(new Rect(530, 10, 560, 300), GUI.skin.box);
        string tier = _upgradeShip != null ? $"{_upgradeShip.CurrentTier + 1}/{_upgradeShip.TierCount}" : "?";
        GUILayout.Label($"<b>BARCO TIER {tier}</b>   F6 → tier 1 / F7/F8 → animar a tier 2/3", _richStyle);
        GUILayout.Label("<b>DERECHA</b> (side debería ser +)", _richStyle);
        DrawShootPointList(_hardpoints.rightShootPoints, true);
        GUILayout.Label("<b>IZQUIERDA</b> (side debería ser −)", _richStyle);
        DrawShootPointList(_hardpoints.leftShootPoints, false);
        GUILayout.EndArea();
    }

    private void DrawShootPointList(Transform[] points, bool expectRight)
    {
        if (points == null) return;
        foreach (var p in points)
        {
            if (p == null) { GUILayout.Label("  <color=red>NULL</color>", _richStyle); continue; }
            float side = SideOf(p);
            bool ok = expectRight ? side > 0f : side < 0f;
            bool active = p.gameObject.activeInHierarchy;
            string col = !active ? "grey" : ok ? "lime" : "red";
            string parent = p.parent != null ? p.parent.name : "-";
            GUILayout.Label(
                $"  <color={col}>{parent}/{p.name}   side: {side:+0.00;-0.00}   {(ok ? "OK" : "INVERTIDO")}{(active ? "" : " (apagado)")}</color>",
                _richStyle);
        }
    }

    private void OnDrawGizmos()
    {
        var hp = _hardpoints;
        if (hp == null && _abilityController != null) hp = _abilityController.GetComponent<ShipHardpoints>();
        if (hp == null) return;
        DrawGizmoList(hp, hp.rightShootPoints, true);
        DrawGizmoList(hp, hp.leftShootPoints, false);
    }

    private void DrawGizmoList(ShipHardpoints hp, Transform[] points, bool expectRight)
    {
        if (points == null) return;
        foreach (var p in points)
        {
            if (p == null) continue;
            Vector3 fwd = p.forward; fwd.y = 0f;
            float side = Vector3.Dot(fwd.normalized, hp.transform.right);
            bool ok = expectRight ? side > 0f : side < 0f;
            Gizmos.color = ok ? Color.green : Color.red;
            Gizmos.DrawSphere(p.position, 0.2f);
            Gizmos.DrawRay(p.position, p.forward * _shootPointRayLength);
        }
    }

    private void OnGUI()
    {
        if (!_showDebug) return;
        GUI.Box(new Rect(10, 10, 280, 500), "DEBUG - Controles");
        GUI.Label(new Rect(20, 35, 260, 20), "1-5     → +1 tier al path");
        GUI.Label(new Rect(20, 55, 260, 20), "Shift+1-5 → Maxear path");
        GUI.Label(new Rect(20, 75, 260, 20), "R       → Reset upgrades");
        GUI.Label(new Rect(20, 95, 260, 20), "F1      → Spawn Mortar Boss");
        GUI.Label(new Rect(20, 115, 260, 20), "F2      → Spawn Dash Boss");
        GUI.Label(new Rect(20, 135, 260, 20), "F3      → Spawn Final Boss");
        GUI.Label(new Rect(20, 155, 260, 20), "F5      → Reset Bosses");
        GUI.Label(new Rect(20, 175, 260, 20), "F6/F7/F8 → Barco tier 1/2/3 (anim)");
        GUI.Label(new Rect(20, 195, 260, 20), "F9      → Reload escena");
        GUI.Label(new Rect(20, 215, 260, 20), "Tab     → Toggle este menu");
        if (_abilityController != null && _abilityController.CannonAbility != null)
        {
            var data = _abilityController.CannonAbility._rtData;
            bool hasChargedShot = _playerUpgrades != null && _playerUpgrades.HasAbility(SpecialAbilityType.ChargedShot);
            bool hasBurnShot = _playerUpgrades != null && _playerUpgrades.HasAbility(SpecialAbilityType.BurnShot);
            bool hasGroundFire = _playerUpgrades != null && _playerUpgrades.HasAbility(SpecialAbilityType.GroundFire);
            GUI.Box(new Rect(20, 240, 260, 350), "");
            GUI.Label(new Rect(30, 245, 260, 20), "Cannon stats en vivo:");
            GUI.Label(new Rect(30, 265, 260, 20), $"Damage: {data.damage:F2}");
            GUI.Label(new Rect(30, 285, 260, 20), $"Cooldown: {data.cooldown:F2}");
            GUI.Label(new Rect(30, 305, 260, 20), $"TimeBetweenShots: {data.timeBetweenShots:F2}");
            GUI.Label(new Rect(30, 325, 260, 20), $"Ammo: {_abilityController.CannonAbility.CurrentAmmo}/{data.maxAmmo}{(_abilityController.CannonAbility.IsReloading ? " (recargando)" : "")}");
            GUI.Label(new Rect(30, 345, 260, 20), $"ExplosionRadius: {data.explosionRadius:F2}");
            GUI.Label(new Rect(30, 365, 260, 20), $"ChargedShot: {(hasChargedShot ? "Unlocked" : "Locked")}");
            GUI.Label(new Rect(30, 385, 260, 20), $"  Intervalo: cada {data.chargedShotInterval} disparos");
            GUI.Label(new Rect(30, 405, 260, 20), $"  Escala bala: x{data.chargedBulletScale:F2}");
            GUI.Label(new Rect(30, 425, 260, 20), $"  Mult. daño: x{data.chargedDamageMultiplier:F2}");
            GUI.Label(new Rect(30, 445, 260, 20), $"  Mult. explosión: x{data.chargedExplosionMultiplier:F2}");
            GUI.Label(new Rect(30, 465, 260, 20), $"BurnShot: {(hasBurnShot ? "Unlocked" : "Locked")}");
            GUI.Label(new Rect(30, 485, 260, 20), $"  Daño/tick: {data.burnDamagePerTick:F2} cada {data.burnTickInterval:F2}s");
            GUI.Label(new Rect(30, 505, 260, 20), $"  Duración: {data.burnDuration:F2}s");
            GUI.Label(new Rect(30, 525, 260, 20), $"GroundFire: {(hasGroundFire ? "Unlocked" : "Locked")}");
            GUI.Label(new Rect(30, 545, 260, 20), $"  Radio: {data.groundFireRadius:F2} / Dur: {data.groundFireDuration:F2}s / Dps: {data.groundFireDamagePerSecond:F2}");
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

        DrawShipPanel();
    }
}