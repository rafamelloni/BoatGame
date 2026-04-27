using System.Collections;
using UnityEngine;

public class SceneResetter : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RT_PlayerHealth _playerHealth;
    [SerializeField] private IslandSpawnManager _islandSpawnManager;
    [SerializeField] private AbilityUpgradeSystem _abilityUpgradeSystem;
    [SerializeField] private UpgradeStatsUI _upgradeStatsUI;
    [SerializeField] private EnemySpawner _enemySpawner;

    [Header("Player")]
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private Vector3 _playerStartPosition;
    [SerializeField] private Quaternion _playerStartRotation;


    [Header("Circle Vignette")]
    [SerializeField] private UnityEngine.UI.Image _vignetteImage;
    [SerializeField] private float _fadeInDuration = 0.8f;
    [SerializeField] private float _holdDuration = 0.5f;
    [SerializeField] private float _fadeOutDuration = 0.8f;

    private Material _vignetteMat;
    private Camera _mainCamera;

    private bool _isResetting = false;
    void Start()
    {
        _playerHealth.OnDeath += HandlePlayerDeath;
        // Guardá posición inicial automáticamente
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
        if (Input.GetKeyUp(KeyCode.L)) {
            HandlePlayerDeath();
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
        _isResetting = false;  // solo esto
    }

    private void ResetPlayer()
    {
        // Posición y rotación
        var rb = _playerTransform.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.MovePosition(_playerStartPosition);
        rb.MoveRotation(_playerStartRotation);

        // Stats
        _playerTransform.GetComponent<RT_PlayerStats>().ResetToBase();

        // Health
        _playerHealth.ResetHealth();
    }

    private IEnumerator PlayDeathVignette()
    {
        _vignetteImage.gameObject.SetActive(true);

        Vector3 screenPos = _mainCamera.WorldToViewportPoint(_playerTransform.position);
        _vignetteMat.SetVector("_Valoe", new Vector4(screenPos.x, 0.5f, 0, 0));

        yield return StartCoroutine(AnimateRadius(0.3f, 0f, _fadeInDuration));

        yield return new WaitForSeconds(_holdDuration);


        //RESET
        _islandSpawnManager.ResetIslands();
        _enemySpawner.DespawnAll();
        _abilityUpgradeSystem.ResetAllUpgrades();
        _upgradeStatsUI.ResetStats();
        ResetPlayer();

        screenPos = _mainCamera.WorldToViewportPoint(_playerTransform.position);
        _vignetteMat.SetVector("_Valoe", new Vector4(screenPos.x, 0.5f, 0, 0));

        yield return StartCoroutine(AnimateRadius(0f, 0.3f, _fadeOutDuration));

        _vignetteImage.gameObject.SetActive(false);
        _isResetting = false;
    }

    private IEnumerator AnimateRadius(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float radius = Mathf.Lerp(from, to, t / duration);
            _vignetteMat.SetFloat("_radius", radius);
            yield return null;
        }
        _vignetteMat.SetFloat("_radius", to);
    }
}