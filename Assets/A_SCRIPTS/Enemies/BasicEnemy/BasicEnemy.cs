using System;
using UnityEngine;

public class BasicEnemy : Enemy
{
    [Header("BasicEnemy Class Temporal")]
    [SerializeField] private Transform leader;
    [SerializeField] private Transform player;
    [SerializeField] private float speed = 3f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private ParticleSystem _trailP;

    [Header("Obstacle Avoidance")]
    [SerializeField] private float obstacleRadius = 15f;
    [SerializeField] private float obstacleForce = 3f;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Separation")]
    [SerializeField] private float separationRadius = 4f;
    [SerializeField] private float separationForce = 2f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Boss Upgrade")]
    [SerializeField] private Renderer _meshRenderer;
    [SerializeField] private GameObject[] _localModels; // hijos del prefab, uno por tier

    public static event Action OnBossDefeated;
    private static int _tier = 0;

    public Vector3 OriginalLocalPosition { get; private set; }
    public Quaternion OriginalLocalRotation { get; private set; }

    private Vector3 formationOffset;
    private bool _stopped;
    private FakeWaveMovement _wave;

    private void Awake()
    {
        base.Awake();
        _wave = GetComponent<FakeWaveMovement>();
        OriginalLocalPosition = transform.localPosition;
        OriginalLocalRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        OnBossDefeated += ApplyTier;
        if (_tier > 0)
            ApplyTier();
    }

    private void OnDisable()
    {
        OnBossDefeated -= ApplyTier;
    }

    private void ApplyTier()
    {
        ApplyUpgradedMaterial();
        ApplyUpgradedModel();
    }

    private void ApplyUpgradedMaterial()
    {
        if (_meshRenderer == null || baseData?.tieredMaterials == null) return;
        int idx = _tier - 1;
        if (idx >= 0 && idx < baseData.tieredMaterials.Length)
            _meshRenderer.material = baseData.tieredMaterials[idx];
    }

    private void ApplyUpgradedModel()
    {
        if (_localModels == null || _localModels.Length == 0) return;
        int idx = _tier - 1;
        for (int i = 0; i < _localModels.Length; i++)
        {
            if (_localModels[i] != null)
                _localModels[i].SetActive(i == idx);
        }

        // Actualizar el renderer al modelo activo
        if (idx >= 0 && idx < _localModels.Length && _localModels[idx] != null)
        {
            Renderer r = _localModels[idx].GetComponentInChildren<Renderer>();
            if (r != null) _meshRenderer = r;
        }
    }

    public static void TriggerBossDefeated()
    {
        _tier++;
        OnBossDefeated?.Invoke();
    }

    public static void ResetTier()
    {
        _tier = 0;
    }

    // ... resto igual
    public void SetPlayer(Transform player) { /* sin cambios */ this.player = player; if (leader != null) formationOffset = transform.position - leader.position; }
    public void SetStopped(bool stopped) { _stopped = stopped; }
    private void ApplyYaw(Quaternion targetRot) { Quaternion smoothed = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime); if (_wave != null) _wave.SetYaw(smoothed.eulerAngles.y); else transform.rotation = smoothed; }

    private void Update()
    {
        if (player == null) return;
        Vector3 targetPos = (leader != null && leader.gameObject.activeInHierarchy) ? leader.position + formationOffset : player.position;
        Vector3 dir = targetPos - transform.position; dir.y = 0f;
        if (!_stopped && dir.sqrMagnitude > 0.01f)
        {
            if (_trailP != null) _trailP.Play();
            Vector3 avoidance = Vector3.zero;
            Collider[] obstacles = Physics.OverlapSphere(transform.position, obstacleRadius, obstacleLayer);
            foreach (var col in obstacles) { Vector3 away = transform.position - col.ClosestPoint(transform.position); away.y = 0f; float dist = away.magnitude; if (dist > 0.001f) avoidance += away.normalized / dist; }
            Vector3 separation = Vector3.zero;
            Collider[] neighbors = Physics.OverlapSphere(transform.position, separationRadius, enemyLayer);
            foreach (var col in neighbors) { if (col.gameObject == gameObject) continue; Vector3 away = transform.position - col.transform.position; away.y = 0f; float dist = away.magnitude; if (dist > 0.001f) separation += away.normalized / dist; }
            Vector3 move = dir.normalized + avoidance * obstacleForce + separation * separationForce; move.y = 0f;
            transform.position += move.normalized * speed * Time.deltaTime;
            Vector3 rotDir = player.position - transform.position; rotDir.y = 0f;
            if (rotDir.sqrMagnitude > 0.01f) { Quaternion targetRot = Quaternion.LookRotation(rotDir) * Quaternion.Euler(0f, -90f, 0f); ApplyYaw(targetRot); }
        }
        else if (_stopped)
        {
            if (_trailP != null) _trailP.Stop();
            Vector3 rotDir = player.position - transform.position; rotDir.y = 0f;
            if (rotDir.sqrMagnitude > 0.01f) { Quaternion targetRot = Quaternion.LookRotation(rotDir) * Quaternion.Euler(0f, -90f, 0f); ApplyYaw(targetRot); }
        }
    }
}