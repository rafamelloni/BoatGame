using System;
using UnityEngine;

public class EnemyGroup : MonoBehaviour
{
    [SerializeField] private EnemyHealth[] _members;
    private int _aliveCount;

    public event Action<EnemyGroup> OnGroupDead;
    public event Action<EnemyGroup> OnGroupAlmostDead;

    [Header("Movement")]
    [SerializeField] private float stopDistance = 12f;

    [Header("Shoot")]
    [SerializeField] private float fireRate = 2f;
    private float _nextFireTime;

    private Transform _player;
    private BasicEnemy[] _enemies;
    private BasicEnemyShoot[] _shooters;

    public void Init(Transform player, BulletFactory enemyBullet)
    {
        _player = player;
        _aliveCount = _members.Length;

        foreach (var member in _members)
        {
            member.Revive();
            member.OnDeath += OnMemberDied;
        }

        _enemies = GetComponentsInChildren<BasicEnemy>();
        _shooters = GetComponentsInChildren<BasicEnemyShoot>();

        Physics.SyncTransforms();

        foreach (var enemy in _enemies)
            enemy.SetPlayer(player);
        foreach (var shooter in _shooters)
            shooter.SetTarget(player, enemyBullet);
    }

    private void Update()
    {
        if (_player == null) return;

        Transform reference = GetAliveMember();
        if (reference == null) return;

        Vector3 toPlayer = _player.position - reference.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;
        bool shouldStop = dist <= stopDistance;

        foreach (var enemy in _enemies)
            enemy.SetStopped(shouldStop);
        foreach (var shooter in _shooters)
            shooter.SetStopped(shouldStop);

        if (Time.time >= _nextFireTime)
        {
            foreach (var shooter in _shooters)
            {
                if (shooter.gameObject.activeInHierarchy)
                    shooter.ForceShoot();
            }
            _nextFireTime = Time.time + fireRate;
        }
    }

    private Transform GetAliveMember()
    {
        foreach (var member in _members)
        {
            if (member != null && member.gameObject.activeInHierarchy)
                return member.transform;
        }
        return null;
    }

    public void Cleanup()
    {
        if (_members == null) return;
        foreach (var member in _members)
        {
            if (member == null) continue;
            member.OnDeath -= OnMemberDied;
            member.Revive();

            // Resetear posición y rotación local al estado original del prefab
            var basicEnemy = member.GetComponent<BasicEnemy>();
            if (basicEnemy != null)
            {
                basicEnemy.ForceApplyModel();
                member.transform.localPosition = basicEnemy.OriginalLocalPosition;
                member.transform.localRotation = basicEnemy.OriginalLocalRotation;
            }
        }
        _aliveCount = 0;
    }

    private void OnMemberDied()
    {
        _aliveCount--;
        if (_aliveCount == 1)
            OnGroupAlmostDead?.Invoke(this);
        if (_aliveCount <= 0)
            OnGroupDead?.Invoke(this);
    }
}