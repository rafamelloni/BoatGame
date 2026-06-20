using System;
using UnityEngine;

public class DashBossEnemy : Enemy
{
    [Header("DashBossEnemy")]
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDelay = 0.8f; // cooldown antes de dashear post-emerge

    private Vector3 _dashDir;
    private bool _dashing;

    public event Action<DashBossEnemy> OnDead;

    protected override void Awake()
    {
        base.Awake();
        _enemyHealth.OnDeath += () => OnDead?.Invoke(this);
    }

    public void Launch(Vector3 direction)
    {
        _dashDir = direction;
        _dashDir.y = 0f;
        _dashDir.Normalize();
        _dashing = false;
        _enemyHealth.Revive();
        StartCoroutine(DelayedDash());
    }

    private System.Collections.IEnumerator DelayedDash()
    {
        yield return new WaitForSeconds(dashDelay);

        _dashing = true;

        if (_dashDir.sqrMagnitude > 0.01f)
        {
            Quaternion rot = Quaternion.LookRotation(_dashDir);
            rot *= Quaternion.Euler(0f, -90f, 0f);
            transform.rotation = rot;
        }
    }

    private void Update()
    {
        if (!_dashing) return;
        transform.position += _dashDir * dashSpeed * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<RT_PlayerHealth>()?.TakeDamage(10f);
            ParticlePool.Instance.GetParticle(baseData.woodExplosionVFX, collision.contacts[0].point);
            Despawn();
        }
        else if (collision.gameObject.CompareTag("Island"))
        {
            ParticlePool.Instance.GetParticle(baseData.woodExplosionVFX, collision.contacts[0].point);
            Despawn();
        }
    }

    private void Despawn()
    {
        _dashing = false;
        _enemyHealth.TakeDamage(999f);
    }
}