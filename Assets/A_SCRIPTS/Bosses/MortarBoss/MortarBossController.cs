using System;
using System.Collections;
using UnityEngine;

public class MortarBossController : MonoBehaviour
{
    [Header("Patrones")]
    [SerializeField] private BossCrossPattern _crossPattern;
    [SerializeField] private BossRandomPattern _randomPattern;
    [SerializeField] private BossChargeAttack _chargeAttack;

    [Header("Cooldowns por fase")]
    [SerializeField] private float _cooldownFase1 = 2.5f;
    [SerializeField] private float _cooldownFase2 = 1.5f;

    [Header("Duración del ataque (antes de cooldown)")]
    [SerializeField] private float _attackDuration = 1.5f;

    [Header("UI")]
    [SerializeField] private GameObject _UIShipUpgrade;

    private MortarBossHealth _health;
    private BossMovement _movement;

    private enum State { Idle, Attacking, Cooldown, Dead }
    private State _state = State.Idle;

    private int _fase = 1;
    private int _attackIndex = 0;

    private void Awake()
    {
        _health = GetComponent<MortarBossHealth>();
        _movement = GetComponent<BossMovement>();
    }

    private void OnEnable()
    {
        if (_health == null) return;

        _health.OnHealthChanged -= HandleHealthChanged;
        _health.OnDeath -= HandleDeath;
        _health.OnHealthChanged += HandleHealthChanged;
        _health.OnDeath += HandleDeath;

        if (_movement != null) _movement.Paused = false;
        StartCoroutine(BossLoop());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (_movement != null) _movement.Paused = true;
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

        var activeBullets = FindObjectsByType<BarrelExplosion>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var bullet in activeBullets)
            bullet.ForceReturn();

        _UIShipUpgrade.SetActive(true);
    }

    public void ResetBoss()
    {
        StopAllCoroutines();

        _state = State.Idle;
        _fase = 1;
        _attackIndex = 0;

        _chargeAttack.ResetBoss();
        if (_movement != null) _movement.Paused = true;

        _UIShipUpgrade.SetActive(false);
        if (_health == null) return;

        _health.ResetHealth();
    }
}