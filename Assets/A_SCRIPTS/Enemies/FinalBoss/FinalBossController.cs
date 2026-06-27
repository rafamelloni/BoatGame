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

    [Header("Cooldowns")]
    [SerializeField] private float _cooldownMax = 3f;    // cooldown al 100% HP
    [SerializeField] private float _cooldownMin = 1f;    // cooldown al 0% HP

    [Header("Orbita Base")]
    [SerializeField] private float _baseOrbitRadius = 14f;
    [SerializeField] private float _baseOrbitSpeed = 15f; // grados por segundo

    private float _orbitAngle = 0f;
    private bool _isBusy = false;
    private int _attackIndex = 0;

    private void Start()
    {
        _health.OnDeath += HandleDeath;

        // Calcular angulo inicial desde posicion actual
        Vector3 dir = transform.position - _player.position;
        dir.y = 0f;
        _orbitAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

        StartCoroutine(BossLoop());
    }

    private void OnDestroy()
    {
        if (_health != null)
            _health.OnDeath -= HandleDeath;
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
        yield return new WaitForSeconds(2f); // intro delay

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

        switch (attack)
        {
            case 0:
                // Orbita agresiva
                _orbitAttack.DoOrbitAttack();
                yield return new WaitUntil(() => !_orbitAttack.IsOrbiting);
                break;

            case 1:
                // Dash agresivo
                _dash.DoDash();
                yield return new WaitUntil(() => !_dash.IsDashing);
                break;

            case 2:
                // Espiral doble
                _spinAttack.DoSpinAttack();
                yield return new WaitUntil(() => !_spinAttack.IsSpinning);
                break;

            case 3:
                // Cerco progresivo
                _spawnPattern.ExecuteCercoProgresivo();
                yield return new WaitForSeconds(5f);
                break;

            case 4:
                // Espiral con gap
                _curtainAttack.DoSpinGapAttack();
                yield return new WaitUntil(() => !_curtainAttack.IsSpinning);
                break;

            case 5:
                // Linea de fuego
                _spawnPattern.ExecuteLineaFuegoGroups();
                yield return new WaitForSeconds(4f);
                break;

            

            case 6:
                _spawnPattern.ExecutePattern();
                yield return new WaitForSeconds(4f);
                break;
        }
    }

    private float GetCooldown()
    {
        // Cooldown se reduce a medida que pierde vida
        float hp = _health.HealthPercent;
        return Mathf.Lerp(_cooldownMin, _cooldownMax, hp);
    }

    private void HandleDeath()
    {
        StopAllCoroutines();
        Debug.Log("[FinalBoss] Muerto.");
    }
}