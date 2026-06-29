using System.Collections;
using UnityEngine;

public class FinalBossController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform _player;
    [SerializeField] private FinalBossDash _dash;
    [SerializeField] private BossSpinAttack _spinAttack;
    [SerializeField] private BossCurtainAttack _curtainAttack;
    [SerializeField] private BossSpawnPattern _spawnPattern;
    [SerializeField] private BossOrbitAttack _orbitAttack;
    [SerializeField] private MortarBossHealth _health;
    [SerializeField] private GameObject _cannons;
    [SerializeField] private GameObject finalcanvas;

    [Header("Cooldowns")]
    [SerializeField] private float _cooldownMax = 3f;
    [SerializeField] private float _cooldownMin = 1f;

    [Header("Orbita Base")]
    [SerializeField] private float _baseOrbitRadius = 14f;
    [SerializeField] private float _baseOrbitSpeed = 15f;

    private float _orbitAngle = 0f;
    private bool _isBusy = false;
    private int _attackIndex = 0;

    private void OnEnable()
    {
        _attackIndex = 0;
        _isBusy = false;
        _health.OnDeath += HandleDeath;

        Vector3 dir = transform.position - _player.position;
        dir.y = 0f;
        _orbitAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

        StartCoroutine(BossLoop());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _isBusy = false;
        if (_health != null) _health.OnDeath -= HandleDeath;
    }

    private void Update()
    {
        if (!_isBusy)
            DoBaseOrbit();
    }

    private void DoBaseOrbit()
    {
        Vector3 dir = (_player.position - transform.position);
        dir.y = 0f;

        float dist = dir.magnitude;
        if (dist > _baseOrbitRadius)
        {
            Vector3 targetPos = transform.position + dir.normalized * _baseOrbitSpeed * Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, targetPos, 0.1f);
        }

        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 2f * Time.deltaTime);
        }
    }

    private IEnumerator BossLoop()
    {
        yield return new WaitForSeconds(2f);

        while (!_health.IsDead)
        {
            yield return new WaitForSeconds(GetCooldown());

            _isBusy = true;
            yield return StartCoroutine(ExecuteNextAttack());
            _isBusy = false;

            _attackIndex++;
        }
    }

    private IEnumerator ExecuteNextAttack()
    {
        int attack = _attackIndex % 7;
        Debug.Log($"[FinalBoss] Ataque {attack} START");


        switch (attack)
        {
            case 0:
                _orbitAttack.DoOrbitAttack();
                yield return new WaitUntil(() => !_orbitAttack.IsOrbiting);
                break;
            case 1:
                _dash.DoDash();
                yield return new WaitUntil(() => !_dash.IsDashing);
                break;
            case 2:
                _spinAttack.DoSpinAttack();
                yield return new WaitUntil(() => !_spinAttack.IsSpinning);
                break;
            case 3:
                _spawnPattern.ExecuteCercoProgresivo();
                yield return new WaitForSeconds(5f);
                break;
            case 4:
                _curtainAttack.DoSpinGapAttack();
                yield return new WaitUntil(() => !_curtainAttack.IsSpinning);
                break;
            case 5:
                _spawnPattern.ExecuteLineaFuegoGroups();
                yield return new WaitForSeconds(4f);
                break;
            case 6:
                _spawnPattern.ExecutePattern();
                yield return new WaitForSeconds(4f);
                break;
        }
        Debug.Log($"[FinalBoss] Ataque {attack} END");

    }

    private float GetCooldown()
    {
        float hp = _health.HealthPercent;
        return Mathf.Lerp(_cooldownMin, _cooldownMax, hp);
    }

    private void HandleDeath()
    {
        StopAllCoroutines();
        Debug.Log("[FinalBoss] Muerto.");
    }

    public void EndCanvas()
    {
        finalcanvas.SetActive(true);
        Time.timeScale = 0f;
    }
}