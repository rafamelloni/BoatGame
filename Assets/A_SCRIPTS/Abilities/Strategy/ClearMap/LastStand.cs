using UnityEngine;
using System.Collections;

public class LastStand : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private float healthThreshold = 0.30f;
    [SerializeField] private float cooldown = 45f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("VFX (opcional)")]
    [SerializeField] private ParticleSystem activationVFX;

    private bool _isUnlocked = false;
    private bool _onCooldown = false;
    private RT_PlayerHealth _playerHealth;
    private RT_PlayerStats _stats;

    private void Awake()
    {
        _playerHealth = GetComponent<RT_PlayerHealth>();
        _stats = GetComponent<RT_PlayerStats>();
    }

    private void OnEnable()
    {
        if (_playerHealth != null)
            _playerHealth.OnDamage += CheckThreshold;
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
            _playerHealth.OnDamage -= CheckThreshold;
    }

    public void Unlock() => _isUnlocked = true;

    public void DebugActivate() => Activate();

    private void CheckThreshold()
    {
        if (!_isUnlocked || _onCooldown) return;

        float ratio = _stats.currentHealth / _stats.maxHealth;
        if (ratio <= healthThreshold)
            Activate();
    }

    private void Activate()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, 999f, enemyLayer);
        foreach (var e in enemies)
        {
            EnemyHealth health = e.GetComponent<EnemyHealth>();
            if (health != null)
                health.TakeDamage(999f);
        }

        //activationVFX?.Play();
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        _onCooldown = true;
        yield return new WaitForSeconds(cooldown);
        _onCooldown = false;
    }
}