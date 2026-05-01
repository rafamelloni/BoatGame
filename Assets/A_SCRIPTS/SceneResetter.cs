using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

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
    [SerializeField] private float _zoomDuration = 0.4f;
    [SerializeField] private float _particleDelay = 0.2f;

    [SerializeField] private Movement _movement;

    private Material _vignetteMat;
    private Camera _mainCamera;
    private bool _isResetting = false;

    void Start()
    {
        _playerHealth.OnDeath += HandlePlayerDeath;
        _playerStartPosition = _playerTransform.position;
        _playerStartRotation = _playerTransform.rotation;
        _mainCamera = Camera.main;
        _vignetteMat = _vignetteImage.material;
        _vignetteImage.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        _playerHealth.OnDeath -= HandlePlayerDeath;
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.L))
            HandlePlayerDeath();

        if (Input.GetKeyUp(KeyCode.B))
        {
            BOSS.SetActive(true);
        }
           
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
        //player
        _movement.SetMovementEnabled(true);
        var rb = _playerTransform.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.MovePosition(_playerStartPosition);
        rb.MoveRotation(_playerStartRotation);
        _playerTransform.GetComponent<RT_PlayerStats>().ResetToBase();
        _playerHealth.ResetHealth();

        //Abilityes-Stats
        _islandSpawnManager.ResetIslands();
        _enemySpawner.DespawnAllAndReset();
        _abilityUpgradeSystem.ResetAllUpgradesStats();
        _upgradeStatsUI.ResetStats();
        _abilityController.ResetAbilities();
        _timerBoss.ResetTimer();

        //boss
        BOSS.SetActive(false);
    }

    private IEnumerator PlayDeathVignette()
    {
        _movement.SetMovementEnabled(false);
        _vignetteImage.gameObject.SetActive(true);
        //SetVignetteCenter();
        // 1. Fade in vignette + zoom in simultáneos
        _Canvas.SetActive(false);
        StartCoroutine(LerpFOV(_mainCamera.fieldOfView, _zoomFOV, _fadeInDuration));
        StartCoroutine(LerpTimeScale(_slowMotionScale, _slowMoFadeInDuration));
        StartCoroutine(PlayParticlesStaggered());
        yield return StartCoroutine(AnimateRadiusUnscaled(0.3f, _vignetteClosedRadius, _fadeInDuration));

        // 2. Zoom in + slow mo + partículas simultáneos
     
       

        yield return new WaitForSecondsRealtime(_holdDuration);

        // 3. Cerrar vignette del todo
        //SetVignetteCenter();
        yield return StartCoroutine(AnimateRadiusUnscaled(_vignetteClosedRadius, 0f, _fadeOutDuration));

        // 4. Reset todo
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        _mainCamera.fieldOfView = 60f;

       
        ResetAll();

        foreach (var ps in _deathParticles)
            if (ps != null) ps.Stop();

        // 5. Abrir vignette DESDE la posición nueva del player
        //SetVignetteCenter();
        yield return new WaitForSecondsRealtime(_holdAfterResetDuration);
        _MESH.SetActive(true);
        _Canvas.SetActive(true);
        yield return StartCoroutine(AnimateRadiusUnscaled(0f, 0.3f, _fadeOutDuration));
       
        _vignetteImage.gameObject.SetActive(false);
        _isResetting = false;
        
        
        

    }

    // Interpola Time.timeScale en tiempo real
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

    // AnimateRadius en tiempo real para que no le afecte el slow mo
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

            if(i == 2)
            {
                _MESH.SetActive(false);
            }
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
        _mainCamera.fieldOfView = to;
    }
}