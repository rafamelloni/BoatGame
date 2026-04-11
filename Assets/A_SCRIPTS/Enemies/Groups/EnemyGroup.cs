using System;
using UnityEngine;

public class EnemyGroup : MonoBehaviour
{
    [SerializeField] private EnemyHealth[] _members;
    private int _aliveCount;
    public event Action<EnemyGroup> OnGroupDead;

    public void Init(Transform player, BulletFactory enemyBullet)
    {
        _aliveCount = _members.Length;

        foreach (var member in _members)
        {
            member.ResetHealth();
            member.OnDeath += OnMemberDied;
        }

        foreach (var enemy in GetComponentsInChildren<BasicEnemy>())
            enemy.SetPlayer(player);
        foreach (var shooter in GetComponentsInChildren<BasicEnemyShoot>())
            shooter.SetTarget(player, enemyBullet);
    }

    public void Cleanup()
    {
        if (_members == null) return;
        foreach (var member in _members)
        {
            if (member == null) continue;
            member.OnDeath -= OnMemberDied;
        }
    }

    private void OnMemberDied()
    {
        _aliveCount--;
        if (_aliveCount <= 0)
            OnGroupDead?.Invoke(this);
    }
}