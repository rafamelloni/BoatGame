using UnityEngine;

public class BossSequenceManager : MonoBehaviour
{
    [SerializeField] private GameObject[] _bosses;
    [SerializeField] private TimerBoss _timerBoss;
    [SerializeField] private EnemySpawner _enemySpawner;

    private int _currentBossIndex = -1;

    public void ActivateNextBoss()
    {
        _currentBossIndex++;

        if (_currentBossIndex >= _bosses.Length)
        {
            Debug.Log("No hay mas bosses");
            return;
        }

        GameObject bossGO = _bosses[_currentBossIndex];
        bossGO.SetActive(true);

        EnemyHealth enemyHealth = bossGO.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.OnDeath += OnCurrentBossDied;
            return;
        }

        MortarBossHealth mortarHealth = bossGO.GetComponent<MortarBossHealth>();
        if (mortarHealth != null)
            mortarHealth.OnDeath += OnCurrentBossDied;
    }

    private void OnCurrentBossDied()
    {
        GameObject bossGO = _bosses[_currentBossIndex];

        EnemyHealth enemyHealth = bossGO.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
            enemyHealth.OnDeath -= OnCurrentBossDied;

        MortarBossHealth mortarHealth = bossGO.GetComponent<MortarBossHealth>();
        if (mortarHealth != null)
            mortarHealth.OnDeath -= OnCurrentBossDied;

        _enemySpawner?.ResumeSpawning();
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

                bossGO.SetActive(false);
            }
        }

        _currentBossIndex = -1;
    }
}