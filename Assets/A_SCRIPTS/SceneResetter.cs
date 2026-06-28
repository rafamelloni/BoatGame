using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneResetter : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RT_PlayerHealth _playerHealth;
    [SerializeField] private IslandSpawnManager _islandSpawnManager;
    [SerializeField] private AbilityUpgradeSystem _abilityUpgradeSystem;
    [SerializeField] private UpgradeStatsUI _upgradeStatsUI;
    [SerializeField] private EnemySpawner _enemySpawner;
    [SerializeField] private AbilityController _abilityController;
    [SerializeField] private TimerBoss _timerBoss;
    [SerializeField] private GameObject _Canvas;
    [SerializeField] private GameObject _MESH;
    [SerializeField] private GameObject _cameraPreSeleccion;
    [SerializeField] private GameObject _canvasPreSeleccion;
    [SerializeField] private UpgradeSystem _upgradeSystemRougelike;
    [SerializeField] private PlayerUpgrades _upgradeSystemIslands;
    [SerializeField] private RT_PlayerUpgrades _playerUpgrades;
    [SerializeField] private CoinSpawner _coinSpawner;
    [SerializeField] private CoinBarUI _coinBarUI;
    [SerializeField] private BossSequenceManager _bossSequenceManager;
    [SerializeField] private UpgradeShipBOSS _bossShip;
    [SerializeField] private LifeBoxSpawner _lifeBoxSpawner;
    [SerializeField] private CoinManager _coinManager;

    [Header("Player")]
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private Vector3 _playerStartPosition;
    [SerializeField] private Quaternion _playerStartRotation;

    [Header("BOISS TEST")]
    [SerializeField] private GameObject BOSS;

    [Header("Circle Vignette")]
    [SerializeField] private UnityEngine.UI.Image _vignetteImage;
    [SerializeField] private float _fadeInDuration = 0.8f;
    [SerializeField] private float _holdDuration = 2f;
    [SerializeField] private float _fadeOutDuration = 0.8f;
    [SerializeField] private float _holdAfterResetDuration = 0.5f;
    [SerializeField] private float _vignetteClosedRadius = 0.05f;

    [Header("Death FX")]
    [SerializeField] private ParticleSystem[] _deathParticles;
    [SerializeField] private float _slowMotionScale = 0.15f;
    [SerializeField] private float _slowMoFadeInDuration = 0.3f;
    [SerializeField] private float _zoomFOV = 40f;
    [SerializeField] private float _particleDelay = 0.2f;

    [SerializeField] private Movement _movement;

    private Material _vignetteMat;
    public Camera _mainCamera;
    private bool _isResetting = false;

    void Start()
    {
        _playerHealth.OnDeath += HandlePlayerDeath;
        _playerStartPosition = _playerTransform.position;
        _playerStartRotation = _playerTransform.rotation;
        _vignetteMat = _vignetteImage.material;
        _vignetteImage.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        _playerHealth.OnDeath -= HandlePlayerDeath;
    }


    private void HandlePlayerDeath()
    {
        if (_isResetting) return;
        _isResetting = true;
        StartCoroutine(ResetRoutine());
    }

    private IEnumerator ResetRoutine()
    {
        yield return StartCoroutine(PlayDeathVignette());
    }

    private void ResetAll()
    {
        // Player
        var rb = _playerTransform.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.MovePosition(_playerStartPosition);
        rb.MoveRotation(_playerStartRotation);
        _playerTransform.GetComponent<RT_PlayerStats>().ResetToBase();
        _playerHealth.ResetHealth();
        _bossShip.ResetShip();

        // Abilities
        _abilityController.ResetAbilities();
        _upgradeSystemRougelike.ResetAll();
        _upgradeSystemIslands.ResetAll();

        // UI
        _abilityUpgradeSystem.ResetAllUpgradesStats();
        _upgradeStatsUI.ResetStats();
        _coinBarUI.ResetBar();
        _coinManager.ResetCoins();

        // World - ResetTier ANTES de DespawnAll
        
        _islandSpawnManager.ResetIslands();
        _enemySpawner.DespawnAll();
        _timerBoss.ResetTimer();
        _coinSpawner.DespawnAll();
        _bossSequenceManager.ResetAll();
        _lifeBoxSpawner.ClearAllCrates();

        // Preselección
        PreselectionData.Reset();
        ShipPreselectionManager.Instance.Reset();

        // Boss
        BOSS.SetActive(false);
    }


    private IEnumerator PlayDeathVignette()
    {
        _movement.SetMovementEnabled(false);

        // ✅ Despawnear enemigos inmediatamente al morir
        BasicEnemy.ResetTier();
        ZombieEnemy.ResetZombieTier();
        _enemySpawner.DespawnAll();
        _coinSpawner.DespawnAll();

        _vignetteImage.gameObject.SetActive(true);
        _Canvas.SetActive(false);

        StartCoroutine(LerpFOV(_mainCamera.fieldOfView, _zoomFOV, _fadeInDuration));
        StartCoroutine(LerpTimeScale(_slowMotionScale, _slowMoFadeInDuration));
        StartCoroutine(PlayParticlesStaggered());
        yield return StartCoroutine(AnimateRadiusUnscaled(0.3f, _vignetteClosedRadius, _fadeInDuration));

        yield return new WaitForSecondsRealtime(_holdDuration);

        yield return StartCoroutine(AnimateRadiusUnscaled(_vignetteClosedRadius, 0f, _fadeOutDuration));

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        ResetAll();

        // ✅ Asegurarse que el movimiento sigue deshabilitado post-reset
        _movement.SetMovementEnabled(false);

        foreach (var ps in _deathParticles)
            if (ps != null) ps.Stop();

        yield return new WaitForSecondsRealtime(_holdAfterResetDuration);

        _MESH.SetActive(true);
        yield return StartCoroutine(AnimateRadiusUnscaled(0f, 0.3f, _fadeOutDuration));

        StartCoroutine(LerpFOV(_mainCamera.fieldOfView, 50, 0.1f));

        _vignetteImage.gameObject.SetActive(false);
        _Canvas.SetActive(false);
        _mainCamera.gameObject.SetActive(false);

        _cameraPreSeleccion.SetActive(true);
        _canvasPreSeleccion.SetActive(true);

        _isResetting = false;
    }

    private IEnumerator LerpTimeScale(float targetScale, float realDuration)
    {
        float startScale = Time.timeScale;
        float elapsed = 0f;
        while (elapsed < realDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / realDuration;
            Time.timeScale = Mathf.Lerp(startScale, targetScale, t);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return null;
        }
        Time.timeScale = targetScale;
        Time.fixedDeltaTime = 0.02f * targetScale;
    }

    private IEnumerator AnimateRadiusUnscaled(float from, float to, float realDuration)
    {
        float elapsed = 0f;
        while (elapsed < realDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float radius = Mathf.Lerp(from, to, elapsed / realDuration);
            _vignetteMat.SetFloat("_radius", radius);
            yield return null;
        }
        _vignetteMat.SetFloat("_radius", to);
    }

    private IEnumerator PlayParticlesStaggered()
    {
        for (int i = 0; i < _deathParticles.Length; i++)
        {
            if (_deathParticles[i] == null) continue;
            _deathParticles[i].Play();
            if (i == 2)
                _MESH.SetActive(false);
            yield return new WaitForSecondsRealtime(_particleDelay);
        }
    }

    private IEnumerator LerpFOV(float from, float to, float realDuration)
    {
        float elapsed = 0f;
        while (elapsed < realDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _mainCamera.fieldOfView = Mathf.Lerp(from, to, elapsed / realDuration);
            yield return null;
        }
    }
}