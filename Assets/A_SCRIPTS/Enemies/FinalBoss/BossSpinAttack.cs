using System.Collections;
using UnityEngine;

public class BossSpinAttack : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private Transform _mesh; // el mesh del barco que gira

    [Header("Spin")]
    [SerializeField] private float _spinSpeed = 360f;       // grados por segundo
    [SerializeField] private float _spinDuration = 4f;      // cuanto dura el ataque

    [Header("Disparo")]
    [SerializeField] private float _fireRate = 0.08f;       // segundos entre balas
    [SerializeField] private float _bulletSpawnOffset = 2f; // distancia del centro al spawn
    [SerializeField] private float _bulletYOffset = 1f;

    // El barco tiene dos puntos de disparo opuestos (espiral doble)
    // No necesita transforms — los calcula desde el angulo actual

    private float _currentAngle = 0f;
    private bool _isSpinning = false;

    public bool IsSpinning => _isSpinning;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
            DoSpinAttack();
    }
    public void DoSpinAttack()
    {
        if (_isSpinning) return;
        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        _isSpinning = true;
        float elapsed = 0f;
        float nextFireTime = 0f;

        while (elapsed < _spinDuration)
        {
            float delta = Time.deltaTime;
            elapsed += delta;

            // Girar el mesh
            _currentAngle += _spinSpeed * delta;
            if (_mesh != null)
                _mesh.localRotation = Quaternion.Euler(0f, _currentAngle, 0f);

            // Disparar
            if (elapsed >= nextFireTime)
            {
                FireSpiral(_currentAngle);
                FireSpiral(_currentAngle + 180f); // segundo punto opuesto
                nextFireTime = elapsed + _fireRate;
            }

            yield return null;
        }

        // Resetear rotacion del mesh suavemente
        if (_mesh != null)
            _mesh.localRotation = Quaternion.identity;

        _isSpinning = false;
    }

    private void FireSpiral(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        Vector3 spawnPos = transform.position + dir * _bulletSpawnOffset;
        spawnPos.y = transform.position.y + _bulletYOffset;

        var go = Instantiate(_bulletPrefab, spawnPos, Quaternion.identity);
        go.GetComponent<BossBullet>()?.Launch(dir);
    }
}