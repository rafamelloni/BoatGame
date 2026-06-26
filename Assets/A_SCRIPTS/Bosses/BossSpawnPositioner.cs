using UnityEngine;

/// Posiciona el boss lejos del player y fuera de la vista de cámara.
/// Inicializa el indicador en pantalla.
/// Agregalo al GO del boss junto con BossIndicator.
public class BossSpawnPositioner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _player;
    [SerializeField] private Camera _camera;
    [SerializeField] private Transform _areaCenter;
    [SerializeField] private RectTransform _indicatorPrefab;
    [SerializeField] private Canvas _uiCanvas;

    [Header("Spawn Config")]
    [SerializeField] private Vector2 _areaSize = new Vector2(200f, 200f);
    [SerializeField] private float _spawnY = 5.3f;
    [SerializeField] private float _minDistanceFromPlayer = 60f;
    [SerializeField] private int _maxAttempts = 30;

    private BossIndicator _bossIndicator;

    public void PositionAndInit()
    {
        PositionBoss();
        InitIndicator();
    }

    public void Cleanup()
    {
        _bossIndicator?.Cleanup();
    }

    private void PositionBoss()
    {
        Vector3? pos = FindValidPosition();
        if (pos != null)
            transform.position = pos.Value;
        else
            Debug.LogWarning("[BossSpawnPositioner] No se encontró posición válida para el boss.");
    }

    private void InitIndicator()
    {
        _bossIndicator = GetComponent<BossIndicator>();
        if (_bossIndicator == null || _indicatorPrefab == null || _uiCanvas == null) return;

        RectTransform indicator = Instantiate(_indicatorPrefab, _uiCanvas.transform);
        _bossIndicator.Init(indicator);
    }

    private Vector3? FindValidPosition()
    {
        if (_areaCenter == null || _player == null || _camera == null) return null;

        float halfX = _areaSize.x * 0.5f;
        float halfZ = _areaSize.y * 0.5f;

        for (int i = 0; i < _maxAttempts; i++)
        {
            float x = Random.Range(_areaCenter.position.x - halfX, _areaCenter.position.x + halfX);
            float z = Random.Range(_areaCenter.position.z - halfZ, _areaCenter.position.z + halfZ);
            Vector3 candidate = new Vector3(x, _spawnY, z);

            float dist = Vector2.Distance(
                new Vector2(candidate.x, candidate.z),
                new Vector2(_player.position.x, _player.position.z)
            );
            if (dist < _minDistanceFromPlayer) continue;

            Vector3 vp = _camera.WorldToViewportPoint(candidate);
            bool isVisible = vp.z > 0 && vp.x > 0 && vp.x < 1 && vp.y > 0 && vp.y < 1;
            if (isVisible) continue;

            return candidate;
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        if (_areaCenter == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            new Vector3(_areaCenter.position.x, _spawnY, _areaCenter.position.z),
            new Vector3(_areaSize.x, 1f, _areaSize.y)
        );
        if (_player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_player.position, _minDistanceFromPlayer);
        }
    }
}