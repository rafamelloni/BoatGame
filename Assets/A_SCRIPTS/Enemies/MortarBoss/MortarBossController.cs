using System.Collections;
using UnityEngine;

public class MortarBossController : MonoBehaviour
{
    // ─── Fases ───────────────────────────────────────────────────────────────
    // Fase 1: 100-30% HP → Cross / Random alternando, Charge cada 3
    // Fase 2:  30-0%  HP → Cross + Random simultáneos, Charge cada 2

    [Header("Patrones")]
    [SerializeField] private BossCrossPattern _crossPattern;
    [SerializeField] private BossRandomPattern _randomPattern;
    [SerializeField] private BossChargeAttack _chargeAttack;

    [Header("Cooldowns por fase")]
    [SerializeField] private float _cooldownFase1 = 2.5f;
    [SerializeField] private float _cooldownFase2 = 1.5f;

    [Header("Duración del ataque (antes de cooldown)")]
    [SerializeField] private float _attackDuration = 1.5f;

    private MortarBossHealth _health;

    private enum State { Idle, Attacking, Cooldown, Dead }
    private State _state = State.Idle;

    private int _fase = 1;
    private int _attackIndex = 0;

    private void Awake()
    {
        _health = GetComponent<MortarBossHealth>();
    }

    private void Start()
    {
        _health.OnHealthChanged += HandleHealthChanged;
        _health.OnDeath += HandleDeath;
        StartCoroutine(BossLoop());
    }

    private void OnDestroy()
    {
        if (_health == null) return;
        _health.OnHealthChanged -= HandleHealthChanged;
        _health.OnDeath -= HandleDeath;
    }

    private IEnumerator BossLoop()
    {
        yield return new WaitForSeconds(1f);

        while (!_health.IsDead)
        {
            _state = State.Attacking;

            bool isChargeAttack =
                (_fase == 1 && _attackIndex % 3 == 2) ||
                (_fase == 2 && _attackIndex % 2 == 1);

            if (isChargeAttack)
            {
                _chargeAttack.DoCharge();
                yield return new WaitUntil(() => !_chargeAttack.IsCharging);
            }
            else
            {
                FireCurrentFase();
                yield return new WaitForSeconds(_attackDuration);
            }

            _attackIndex++;
            _state = State.Cooldown;
            yield return new WaitForSeconds(_fase == 1 ? _cooldownFase1 : _cooldownFase2);
        }
    }

    private void FireCurrentFase()
    {
        switch (_fase)
        {
            case 1:
                if (_attackIndex % 2 == 0)
                    _crossPattern.FireCross();
                else
                    _randomPattern.FireRandom();
                break;

            case 2:
                _crossPattern.FireCross8();
                _randomPattern.FireRandom();
                break;
        }
    }

    private void HandleHealthChanged(float percent)
    {
        int nuevaFase = percent > 0.3f ? 1 : 2;

        if (nuevaFase != _fase)
        {
            _fase = nuevaFase;
            _attackIndex = 0;
            Debug.Log($"[MortarBoss] Fase {_fase} — HP: {percent:P0}");
        }
    }

    private void HandleDeath()
    {
        _state = State.Dead;
        StopAllCoroutines();
        Debug.Log("[MortarBoss] Muerto.");
    }

    private void OnGUI()
    {
#if UNITY_EDITOR
        GUI.Label(new UnityEngine.Rect(10, 10, 200, 20),
            $"Boss — Fase {_fase} | HP: {_health.HealthPercent:P0} | {_state}");
#endif
    }
}