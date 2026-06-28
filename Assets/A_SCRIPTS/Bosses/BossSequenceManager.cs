using UnityEngine;

public class BossSequenceManager : MonoBehaviour
{
    [SerializeField] private GameObject[] _bosses;
    [SerializeField] private TimerBoss _timerBoss;
    [SerializeField] private EnemySpawner _enemySpawner;
    [SerializeField] private IslandSpawnManager _islandSpawner;
    [SerializeField] private BossArrivalText _bossArrivalText;

    private int _currentBossIndex = -1;

    public void ActivateNextBoss()
    {
        _currentBossIndex++;
        Debug.Log($"ActivateNextBoss llamado, index: {_currentBossIndex}");
        if (_currentBossIndex >= _bosses.Length)
        {
            Debug.Log("No hay mas bosses");
            return;
        }
        ActivateBossInternal(_currentBossIndex);
    }

    public void ActivateBoss(int index)
    {
        if (index < 0 || index >= _bosses.Length)
        {
            Debug.LogWarning($"[BossSequenceManager] Index {index} fuera de rango.");
            return;
        }

        // Limpiar el boss actual si hay uno activo
        if (_currentBossIndex >= 0 && _currentBossIndex < _bosses.Length)
            CleanupBoss(_currentBossIndex);

        _currentBossIndex = index;
        ActivateBossInternal(_currentBossIndex);
    }

    private void ActivateBossInternal(int index)
    {
        GameObject bossGO = _bosses[index];
        Debug.Log($"Activando boss: {bossGO.name}");

        var dashBoss = bossGO.GetComponent<DashBossController>();
        if (dashBoss != null) dashBoss.ResetBoss();

        var mortarBoss = bossGO.GetComponent<MortarBossController>();
        if (mortarBoss != null) mortarBoss.ResetBoss();

        bossGO.SetActive(true);
        _bossArrivalText?.Play();

        bossGO.GetComponent<BossSpawnPositioner>()?.PositionAndInit();

        EnemyHealth enemyHealth = bossGO.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.OnDeath += OnCurrentBossDied;
            return;
        }

        MortarBossHealth mortarHealth = bossGO.GetComponent<MortarBossHealth>();
        if (mortarHealth != null)
        {
            mortarHealth.OnDeath += OnCurrentBossDied;
            return;
        }

        Debug.LogWarning($"Boss {bossGO.name} no tiene EnemyHealth ni MortarBossHealth!");
    }

    private void OnCurrentBossDied()
    {
        Debug.Log($"Boss {_currentBossIndex} murio, resumiendo timer y spawner");
        CleanupBoss(_currentBossIndex);
        _enemySpawner?.ResumeSpawning();

        BasicEnemy.TriggerBossDefeated();
        ZombieEnemy.TriggerZombieBossDefeated();

        _islandSpawner?.StartSpawning();
        _timerBoss.ResumeTimer();
    }

    private void CleanupBoss(int index)
    {
        GameObject bossGO = _bosses[index];
        if (bossGO == null) return;

        EnemyHealth enemyHealth = bossGO.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
            enemyHealth.OnDeath -= OnCurrentBossDied;

        MortarBossHealth mortarHealth = bossGO.GetComponent<MortarBossHealth>();
        if (mortarHealth != null)
            mortarHealth.OnDeath -= OnCurrentBossDied;

        bossGO.GetComponent<BossSpawnPositioner>()?.Cleanup();
    }

    public void ResetAll()
    {
        if (_currentBossIndex >= 0 && _currentBossIndex < _bosses.Length)
            CleanupBoss(_currentBossIndex);

        foreach (GameObject bossGO in _bosses)
        {
            if (bossGO == null) continue;
            bossGO.GetComponent<BossSpawnPositioner>()?.Cleanup();
            var dashBoss = bossGO.GetComponent<DashBossController>();
            if (dashBoss != null) dashBoss.ResetBoss();
            var mortarBoss = bossGO.GetComponent<MortarBossController>();
            if (mortarBoss != null) mortarBoss.ResetBoss();
            bossGO.SetActive(false);
        }

        _currentBossIndex = -1;
    }
}