using System;
using UnityEngine;

public class ZombieGroup : MonoBehaviour
{
    [SerializeField] private EnemyHealth[] _members;
    private int _aliveCount;

    public event Action<ZombieGroup> OnGroupDead;
    public event Action<ZombieGroup> OnGroupAlmostDead;

    private Transform _player;
    private ZombieEnemy[] _zombies;

    public void Init(Transform player, BulletFactory enemyBullet)
    {
        _player = player;
        _aliveCount = _members.Length;

        foreach (var member in _members)
        {
            member.Revive();
            member.OnDeath += OnMemberDied;
        }

        _zombies = GetComponentsInChildren<ZombieEnemy>(true);
        Physics.SyncTransforms();

        foreach (var zombie in _zombies)
            zombie.SetPlayer(player);
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

            var zombie = member.GetComponent<ZombieEnemy>();
            if (zombie != null)
            {
                member.transform.localPosition = zombie.OriginalLocalPosition;
                member.transform.localRotation = zombie.OriginalLocalRotation;
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