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

        GameObject bossGO = _bosses[_currentBossIndex];
        Debug.Log($"Activando boss: {bossGO.name}");
        bossGO.SetActive(true);
        _bossArrivalText?.Play();

        // Posicionar e inicializar indicador
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
            Debug.Log("MortarBossHealth encontrado, suscribiendo OnDeath");
            mortarHealth.OnDeath += OnCurrentBossDied;
        }
        else
        {
            Debug.LogWarning($"Boss {bossGO.name} no tiene EnemyHealth ni MortarBossHealth!");
        }
    }

    private void OnCurrentBossDied()
    {
        Debug.Log($"Boss {_currentBossIndex} murio, resumiendo timer y spawner");

        GameObject bossGO = _bosses[_currentBossIndex];

        EnemyHealth enemyHealth = bossGO.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
            enemyHealth.OnDeath -= OnCurrentBossDied;

        MortarBossHealth mortarHealth = bossGO.GetComponent<MortarBossHealth>();
        if (mortarHealth != null)
            mortarHealth.OnDeath -= OnCurrentBossDied;

        // Limpiar indicador
        bossGO.GetComponent<BossSpawnPositioner>()?.Cleanup();

        _enemySpawner?.ResumeSpawning();
        _islandSpawner?.StartSpawning();
        _timerBoss.ResumeTimer();
    }

    public void ResetAll()
    {
        if (_currentBossIndex >= 0 && _currentBossIndex < _bosses.Length)
        {
            GameObject bossGO = _bosses[_currentBossIndex];
            if (bossGO != null)
            {
                EnemyHealth enemyHealth = bossGO.GetComponent<EnemyHealth>();
                if (enemyHealth != null) enemyHealth.OnDeath -= OnCurrentBossDied;

                MortarBossHealth mortarHealth = bossGO.GetComponent<MortarBossHealth>();
                if (mortarHealth != null) mortarHealth.OnDeath -= OnCurrentBossDied;
            }
        }

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