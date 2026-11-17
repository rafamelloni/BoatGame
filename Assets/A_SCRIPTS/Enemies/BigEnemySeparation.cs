using System.Collections.Generic;
using UnityEngine;

// Separación entre enemigos grandes (Mortar, Ship, Spike, Heavy).
// Ponelo en el prefab de cada uno. Todos los que lo tengan se empujan entre sí
// cuando se pisan, sin tocar la IA de cada enemigo: el empuje se suma a su movimiento.
//  - Solo empuja en X/Z (no toca la altura).
//  - La distancia mínima entre dos es la suma de sus radios (cada prefab tiene el suyo).
//  - Más cerca = empuje más fuerte. Los dos se corren, cada uno para su lado.
//  - Mientras un enemigo sale del agua (EnemyEmerge) no empuja ni lo empujan.
// Corre después de la IA (DefaultExecutionOrder) para corregir donde quedó ese frame.
[DefaultExecutionOrder(100)]
public class BigEnemySeparation : MonoBehaviour
{
    [Tooltip("Radio de este enemigo en metros. La distancia mínima entre dos = suma de sus radios")]
    [SerializeField, Min(0.1f)] private float _radius = 6f;
    [Tooltip("Velocidad de empuje (m/s) cuando están completamente encimados")]
    [SerializeField, Min(0f)] private float _strength = 10f;
    [Tooltip("Tope de velocidad del empuje, por si se juntan varios")]
    [SerializeField, Min(0f)] private float _maxPushSpeed = 12f;

    private static readonly List<BigEnemySeparation> _all = new();

    private EnemyEmerge _emerge;

    public float Radius => _radius;
    private bool IsActive => _emerge == null || !_emerge.IsEmerging;

    private void Awake()
    {
        _emerge = GetComponent<EnemyEmerge>();
    }

    private void OnEnable()
    {
        if (!_all.Contains(this)) _all.Add(this);
    }

    private void OnDisable()
    {
        _all.Remove(this);
    }

    private void Update()
    {
        if (!IsActive) return;

        Vector3 pos = transform.position;
        Vector3 push = Vector3.zero;

        for (int i = 0; i < _all.Count; i++)
        {
            var other = _all[i];
            if (other == this || other == null || !other.IsActive) continue;

            Vector3 away = pos - other.transform.position;
            away.y = 0f;
            float minDist = _radius + other._radius;
            float sqr = away.sqrMagnitude;
            if (sqr >= minDist * minDist) continue;

            float dist = Mathf.Sqrt(sqr);
            Vector3 dir;
            if (dist > 0.001f) dir = away / dist;
            else
            {
                // justo uno arriba del otro: cada uno sale para un lado distinto
                float angle = (GetInstanceID() * 137.5f) % 360f;
                dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            }

            float overlap = 1f - dist / minDist; // 0 = recién se tocan, 1 = encimados
            push += dir * overlap;
        }

        if (push.sqrMagnitude < 0.000001f) return;

        Vector3 velocity = Vector3.ClampMagnitude(push * _strength, _maxPushSpeed);
        transform.position += velocity * Time.deltaTime;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
