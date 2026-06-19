using System.Collections;
using UnityEngine;

public class EnemyEmerge : MonoBehaviour
{
    [Header("Emerge")]
    [SerializeField] private float _startDepth = -5f;        // Y inicial bajo el agua
    [SerializeField] private float _targetHeight = 5.3f;     // Y final (altura del agua)
    [SerializeField] private float _emergeSpeed = 3f;        // velocidad de subida
    [SerializeField] private float _moveSpeed = 2f;          // velocidad hacia el player mientras sube
    [SerializeField] private float _tiltAngle = 45f;         // inclinación en X mientras emerge
    [SerializeField] private float _tiltSmoothSpeed = 5f;    // velocidad de enderezamiento

    private Transform _player;
    private Behaviour[] _behaviours;
    private bool _emerging = false;

    public void Emerge(Transform player)
    {
        if (_emerging) return;
        _player = player;

        // Posicionar bajo el agua
        Vector3 pos = transform.position;
        pos.y = _startDepth;
        transform.position = pos;

        // Recolectar y deshabilitar todos los behaviours de comportamiento
        _behaviours = CollectBehaviours();
        foreach (var b in _behaviours)
            b.enabled = false;

        _emerging = true;
        StartCoroutine(EmergeRoutine());
    }

    private IEnumerator EmergeRoutine()
    {
        while (transform.position.y < _targetHeight)
        {
            // Subir en Y
            Vector3 pos = transform.position;
            pos.y += _emergeSpeed * Time.deltaTime;
            pos.y = Mathf.Min(pos.y, _targetHeight);

            // Moverse hacia el player en XZ
            if (_player != null)
            {
                Vector3 dir = _player.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                {
                    dir.Normalize();
                    pos.x += dir.x * _moveSpeed * Time.deltaTime;
                    pos.z += dir.z * _moveSpeed * Time.deltaTime;
                }
            }

            transform.position = pos;

            // Calcular progreso para el tilt
            float progress = Mathf.InverseLerp(_startDepth, _targetHeight, transform.position.y);

            // Inclinación en X: de _tiltAngle a 0 mientras sube
            float currentTilt = Mathf.Lerp(_tiltAngle, 0f, progress);
            Vector3 euler = transform.eulerAngles;
            euler.x = currentTilt;
            transform.eulerAngles = euler;

            yield return null;
        }

        // Llegó arriba — enderezar y habilitar comportamiento
        Vector3 finalEuler = transform.eulerAngles;
        finalEuler.x = 0f;
        transform.eulerAngles = finalEuler;

        foreach (var b in _behaviours)
            b.enabled = true;

        _emerging = false;
        enabled = false; // se deshabilita a sí mismo
    }

    private Behaviour[] CollectBehaviours()
    {
        var list = new System.Collections.Generic.List<Behaviour>();

        // EnemyGroup + hijos
        var group = GetComponent<EnemyGroup>();
        if (group != null)
        {
            list.Add(group);
            foreach (var b in GetComponentsInChildren<BasicEnemy>())
                list.Add(b);
            foreach (var b in GetComponentsInChildren<BasicEnemyShoot>())
                list.Add(b);
        }

        // ShipEnemy
        var ship = GetComponent<ShipEnemy>();
        if (ship != null) list.Add(ship);

        // RafaEnemy
        var rafa = GetComponent<RafaEnemy>();
        if (rafa != null) list.Add(rafa);

        return list.ToArray();
    }
}