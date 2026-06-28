using System;
using UnityEngine;

public class ZombieEnemy : Enemy
{
    [Header("ZombieEnemy")]
    [SerializeField] private float _speed = 4f;
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _rotationSpeed = 5f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private float _obstacleRadius = 15f;
    [SerializeField] private float _obstacleForce = 3f;
    [SerializeField] private LayerMask _obstacleLayer;

    [Header("Separation")]
    [SerializeField] private float _separationRadius = 4f;
    [SerializeField] private float _separationForce = 2f;
    [SerializeField] private LayerMask _enemyLayer;

    [Header("Boss Upgrade")]
    [SerializeField] private Renderer _meshRenderer;
    [SerializeField] private GameObject[] _localModels;

    public static event Action OnZombieBossDefeated;
    public static int _zombieTier = 0;

    private Transform _player;
    private bool _stopped;
    private FakeWaveMovement _wave;

    public Vector3 OriginalLocalPosition { get; private set; }
    public Quaternion OriginalLocalRotation { get; private set; }

    public event Action<ZombieEnemy> OnDead;

    protected override void Awake()
    {
        base.Awake();
        _wave = GetComponent<FakeWaveMovement>();
        OriginalLocalPosition = transform.localPosition;
        OriginalLocalRotation = transform.localRotation;
        _enemyHealth.OnDeath += () => OnDead?.Invoke(this);
    }

    private void OnEnable()
    {
        OnZombieBossDefeated -= ApplyTier;
        OnZombieBossDefeated += ApplyTier;
        ApplyUpgradedModel();
    }

    private void OnDisable()
    {
        OnZombieBossDefeated -= ApplyTier;
    }

    public static void TriggerZombieBossDefeated()
    {
        Debug.Log($"[ZombieEnemy] TriggerZombieBossDefeated llamado, tier actual: {_zombieTier}");
        if (_zombieTier >= 2) return;
        _zombieTier++;
        OnZombieBossDefeated?.Invoke();
        Debug.Log($"[ZombieEnemy] Nuevo tier: {_zombieTier}");
    }

    public static void ResetZombieTier()
    {
        _zombieTier = 0;
    }

    private void ApplyTier()
    {
        ApplyUpgradedModel();
    }

    private void ApplyUpgradedModel()
    {
        if (_localModels == null || _localModels.Length == 0) return;
        int clampedTier = Mathf.Clamp(_zombieTier, 0, _localModels.Length - 1);
        for (int i = 0; i < _localModels.Length; i++)
            if (_localModels[i] != null)
                _localModels[i].SetActive(i == clampedTier);

        if (_localModels[clampedTier] != null)
        {
            Renderer r = _localModels[clampedTier].GetComponentInChildren<Renderer>();
            if (r != null) _meshRenderer = r;
        }
    }

    public void ForceApplyModel()
    {
        ApplyUpgradedModel();
    }

    public void SetPlayer(Transform player)
    {
        _player = player;
    }

    public void SetStopped(bool stopped)
    {
        _stopped = stopped;
    }

    private void ApplyYaw(Quaternion targetRot)
    {
        Quaternion smoothed = Quaternion.Slerp(transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
        if (_wave != null)
            _wave.SetYaw(smoothed.eulerAngles.y);
        else
            transform.rotation = smoothed;
    }

    private void Update()
    {
        if (_player == null) return;

        Vector3 dir = _player.position - transform.position;
        dir.y = 0f;

        if (!_stopped && dir.sqrMagnitude > 0.01f)
        {
            Vector3 avoidance = Vector3.zero;
            Collider[] obstacles = Physics.OverlapSphere(transform.position, _obstacleRadius, _obstacleLayer);
            foreach (var col in obstacles)
            {
                Vector3 away = transform.position - col.ClosestPoint(transform.position);
                away.y = 0f;
                float dist = away.magnitude;
                if (dist > 0.001f)
                    avoidance += away.normalized / dist;
            }

            Vector3 separation = Vector3.zero;
            Collider[] neighbors = Physics.OverlapSphere(transform.position, _separationRadius, _enemyLayer);
            foreach (var col in neighbors)
            {
                if (col.gameObject == gameObject) continue;
                Vector3 away = transform.position - col.transform.position;
                away.y = 0f;
                float dist = away.magnitude;
                if (dist > 0.001f)
                    separation += away.normalized / dist;
            }

            Vector3 move = dir.normalized + avoidance * _obstacleForce + separation * _separationForce;
            move.y = 0f;
            transform.position += move.normalized * _speed * Time.deltaTime;

            Vector3 rotDir = _player.position - transform.position;
            rotDir.y = 0f;
            if (rotDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(rotDir) * Quaternion.Euler(0f, -90f, 0f);
                ApplyYaw(targetRot);
            }
        }
        else if (_stopped)
        {
            Vector3 rotDir = _player.position - transform.position;
            rotDir.y = 0f;
            if (rotDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(rotDir) * Quaternion.Euler(0f, -90f, 0f);
                ApplyYaw(targetRot);
            }
        }
    }

    private void OnCollisionEnter(Collision col)
    {
        if (!col.gameObject.CompareTag("Player")) return;
        col.gameObject.GetComponent<RT_PlayerHealth>()?.TakeDamage(_damage);
    }
}