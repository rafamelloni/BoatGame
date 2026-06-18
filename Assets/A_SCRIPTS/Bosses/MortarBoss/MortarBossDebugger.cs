using System.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Attach al mismo GameObject que MortarBossController.
/// Teclas de debug configurables desde el Inspector.
/// Desactivar en build final.
/// </summary>
public class MortarBossDebugger : MonoBehaviour
{
    [Header("─── Teclas de Debug ───────────────────────")]
    [SerializeField] private KeyCode _keyFireCross = KeyCode.Alpha1;
    [SerializeField] private KeyCode _keyFireRandom = KeyCode.Alpha2;
    [SerializeField] private KeyCode _keyCharge = KeyCode.Alpha3;
    [SerializeField] private KeyCode _keyAllAtOnce = KeyCode.Alpha4;
    [SerializeField] private KeyCode _keyDamage25 = KeyCode.Alpha5;
    [SerializeField] private KeyCode _keyDamage50 = KeyCode.Alpha6;
    [SerializeField] private KeyCode _keyKill = KeyCode.Alpha0;
    [SerializeField] private KeyCode _keyResetHealth = KeyCode.R;

    [Header("─── Referencias ───────────────────────────")]
    [SerializeField] private BossCrossPattern _crossPattern;
    [SerializeField] private BossRandomPattern _randomPattern;
    [SerializeField] private BossChargeAttack _chargeAttack;
    [SerializeField] private MortarBossHealth _health;
    [SerializeField] private MortarBossController _controller;

    [Header("─── Opciones ──────────────────────────────")]
    [SerializeField] private bool _showHUD = true;
    [SerializeField] private bool _logToConsole = true;
    [SerializeField] private bool _showGizmos = true;
    [SerializeField] private Transform _player;

    // ── colores de log ────────────────────────────────────────────────────────
    private const string COL_CROSS = "#FF6B6B";
    private const string COL_RANDOM = "#FFD93D";
    private const string COL_CHARGE = "#6BCB77";
    private const string COL_DAMAGE = "#FF922B";
    private const string COL_INFO = "#74C0FC";

    // ── estado interno para HUD ──────────────────────────────────────────────
    private string _lastAction = "—";
    private float _lastActionTime = -99f;

    // ─────────────────────────────────────────────────────────────────────────

    private void Reset()
    {
        // auto-buscar componentes al agregar el script
        _crossPattern = GetComponentInChildren<BossCrossPattern>();
        _randomPattern = GetComponentInChildren<BossRandomPattern>();
        _chargeAttack = GetComponentInChildren<BossChargeAttack>();
        _health = GetComponent<MortarBossHealth>();
        _controller = GetComponent<MortarBossController>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(_keyFireCross)) DebugFireCross();
        if (Input.GetKeyDown(_keyFireRandom)) DebugFireRandom();
        if (Input.GetKeyDown(_keyCharge)) DebugCharge();
        if (Input.GetKeyDown(_keyAllAtOnce)) DebugAllAtOnce();
        if (Input.GetKeyDown(_keyDamage25)) DebugDamage(0.25f);
        if (Input.GetKeyDown(_keyDamage50)) DebugDamage(0.50f);
        if (Input.GetKeyDown(_keyKill)) DebugKill();
        if (Input.GetKeyDown(_keyResetHealth)) DebugResetHealth();
    }

    // ─── Acciones de debug ────────────────────────────────────────────────────

    [ContextMenu("Fire Cross")]
    public void DebugFireCross()
    {
        if (_crossPattern == null) { WarnMissing("BossCrossPattern"); return; }
        _crossPattern.FireCross();
        Log($"<color={COL_CROSS}>▶ CrossPattern fired</color>");
        SetLastAction("CrossPattern");
    }

    [ContextMenu("Fire Random")]
    public void DebugFireRandom()
    {
        if (_randomPattern == null) { WarnMissing("BossRandomPattern"); return; }
        _randomPattern.FireRandom();
        Log($"<color={COL_RANDOM}>▶ RandomPattern fired</color>");
        SetLastAction("RandomPattern");
    }

    [ContextMenu("Fire Charge")]
    public void DebugCharge()
    {
        if (_chargeAttack == null) { WarnMissing("BossChargeAttack"); return; }
        if (_chargeAttack.IsCharging)
        {
            Log($"<color={COL_CHARGE}>⚠ Charge ya está en curso</color>");
            return;
        }
        _chargeAttack.DoCharge();
        Log($"<color={COL_CHARGE}>▶ ChargeAttack iniciado</color>");
        SetLastAction("ChargeAttack");
    }

    [ContextMenu("Fire All At Once")]
    public void DebugAllAtOnce()
    {
        Log($"<color={COL_INFO}>▶ ALL ATTACKS — Cross + Random + Charge</color>");
        DebugFireCross();
        DebugFireRandom();
        DebugCharge();
        SetLastAction("ALL ATTACKS");
    }

    public void DebugDamage(float percent)
    {
        if (_health == null) { WarnMissing("MortarBossHealth"); return; }
        // calculamos cuánto daño representa el porcentaje del max hp
        // accedemos via reflexión para evitar exponer maxHealth como público
        float damage = GetMaxHealth() * percent;
        _health.TakeDamage(damage);
        Log($"<color={COL_DAMAGE}>💥 Damage aplicado: {percent * 100f:F0}% de HP max ({damage:F0} dmg) — HP actual: {_health.HealthPercent:P0}</color>");
        SetLastAction($"Damage -{percent * 100:F0}%");
    }

    [ContextMenu("Kill Boss")]
    public void DebugKill()
    {
        if (_health == null) { WarnMissing("MortarBossHealth"); return; }
        _health.TakeDamage(999999f);
        Log($"<color={COL_DAMAGE}>☠ Boss killed via debug</color>");
        SetLastAction("KILLED");
    }

    [ContextMenu("Reset Health")]
    public void DebugResetHealth()
    {
        // re-enable para testear de nuevo sin reiniciar la escena
        var go = gameObject;
        go.SetActive(false);
        go.SetActive(true);
        Log($"<color={COL_INFO}>♻ Boss reseteado (GameObject toggle)</color>");
        SetLastAction("Health Reset");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void Log(string msg)
    {
        if (_logToConsole)
            Debug.Log($"[MortarBossDebug] {msg}");
    }

    private void WarnMissing(string component)
    {
        Debug.LogWarning($"[MortarBossDebug] ⚠ Falta asignar {component} en el Inspector.");
    }

    private void SetLastAction(string action)
    {
        _lastAction = action;
        _lastActionTime = Time.time;
    }

    private float GetMaxHealth()
    {
        // usa reflexión para leer el campo privado _maxHealth sin modificar MortarBossHealth
        var field = typeof(MortarBossHealth).GetField("_maxHealth",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (float)field.GetValue(_health) : 300f;
    }

    // ─── HUD en pantalla ─────────────────────────────────────────────────────

    private void OnGUI()
    {
        if (!_showHUD) return;

        GUIStyle box = new GUIStyle(GUI.skin.box);
        GUIStyle label = new GUIStyle(GUI.skin.label) { fontSize = 13 };
        GUIStyle header = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };

        float x = Screen.width - 270f;
        float y = 10f;
        float w = 260f;
        float lineH = 22f;

        GUI.Box(new Rect(x - 5, y - 5, w + 10, lineH * 14 + 10), "");

        GUI.Label(new Rect(x, y, w, lineH), "── MORTAR BOSS DEBUG ──", header);
        y += lineH;

        if (_health != null)
        {
            float hp = _health.HealthPercent;
            string fase = hp > 0.6f ? "1" : hp > 0.3f ? "2" : "3";
            GUI.Label(new Rect(x, y, w, lineH), $"HP: {hp:P0}  |  Fase: {fase}", label); y += lineH;

            // barra de HP
            Rect barBg = new Rect(x, y, w, 12);
            Rect barFill = new Rect(x, y, w * hp, 12);
            GUI.DrawTexture(barBg, Texture2D.whiteTexture);
            Color barColor = hp > 0.6f ? Color.green : hp > 0.3f ? Color.yellow : Color.red;
            GUI.color = barColor;
            GUI.DrawTexture(barFill, Texture2D.whiteTexture);
            GUI.color = Color.white;
            y += 18f;
        }

        if (_chargeAttack != null)
        {
            string chargeStatus = _chargeAttack.IsCharging ? "⚡ CHARGING" : "ready";
            GUI.Label(new Rect(x, y, w, lineH), $"Charge: {chargeStatus}", label); y += lineH;
        }

        float timeSince = Time.time - _lastActionTime;
        string fadeStr = timeSince < 2f ? $"  ({timeSince:F1}s ago)" : "";
        GUI.Label(new Rect(x, y, w, lineH), $"Last: {_lastAction}{fadeStr}", label); y += lineH * 1.3f;

        GUI.Label(new Rect(x, y, w, lineH), "── TECLAS ──────────────────", label); y += lineH;
        GUI.Label(new Rect(x, y, w, lineH), $"[{_keyFireCross}] Cross Pattern", label); y += lineH;
        GUI.Label(new Rect(x, y, w, lineH), $"[{_keyFireRandom}] Random Pattern", label); y += lineH;
        GUI.Label(new Rect(x, y, w, lineH), $"[{_keyCharge}] Charge Attack", label); y += lineH;
        GUI.Label(new Rect(x, y, w, lineH), $"[{_keyAllAtOnce}] All At Once", label); y += lineH;
        GUI.Label(new Rect(x, y, w, lineH), $"[{_keyDamage25}] Dmg 25%  [{_keyDamage50}] Dmg 50%", label); y += lineH;
        GUI.Label(new Rect(x, y, w, lineH), $"[{_keyKill}] Kill  [{_keyResetHealth}] Reset HP", label);
    }

    // ─── Gizmos ──────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (!_showGizmos) return;

        // rango del charge
        if (_chargeAttack != null)
        {
            Gizmos.color = new Color(0.4f, 1f, 0.4f, 0.3f);
            var rangeField = typeof(BossChargeAttack).GetField("_shootRange",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (rangeField != null)
            {
                float range = (float)rangeField.GetValue(_chargeAttack);
                Gizmos.DrawWireSphere(transform.position, range);
#if UNITY_EDITOR
                Handles.Label(transform.position + Vector3.up * (range + 0.3f), "Charge Range");
#endif
            }
        }

        // línea hacia el player
        if (_player != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, _player.position);
            Gizmos.DrawWireSphere(_player.position, 0.5f);
        }

        // posición de origen del boss
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.4f);
#if UNITY_EDITOR
        Handles.Label(transform.position + Vector3.up * 1.5f,
            _health != null ? $"HP: {_health.HealthPercent:P0}" : "No Health");
#endif
    }
}