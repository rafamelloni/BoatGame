using System;
using UnityEngine;

public class MolotovProjectile : MonoBehaviour
{
    private float _arcHeight;
    private float _speed;
    private float _damage;
    private float _explosionRadius;
    private float _fireDuration;
    private float _fireDamagePerSecond;
    private float _fireRadius;
    private GameObject _explosionVFX;
    private GameObject _fireZonePrefab;
    private LayerMask _damageLayers;

    private Vector3 _startPos;
    private Vector3 _targetPos;
    private float _progress = 0f;
    private bool _launched = false;
    private float _spinAngle = 0f;

    public void Launch(Vector3 from, Vector3 to, RT_MolotovData data, Collider shipCollider)
    {
        Physics.IgnoreCollision(GetComponent<Collider>(), shipCollider, true);
        _startPos = from;
        _targetPos = to;
        _arcHeight = data.arcHeight;
        _speed = data.projectileSpeed;
        _damage = data.damage;
        _explosionRadius = data.explosionRadius;
        _fireDuration = data.fireDuration;
        _fireDamagePerSecond = data.fireDamagePerSecond;
        _fireRadius = data.fireRadius;
        _explosionVFX = data.explosionVFX;
        _fireZonePrefab = data.fireZonePrefab;
        _damageLayers = data.damageLayers;
        _progress = 0f;
        _launched = true;
    }

    private void Update()
    {
        if (!_launched) return;

        _progress += Time.deltaTime * _speed / Vector3.Distance(_startPos, _targetPos);
        _progress = Mathf.Clamp01(_progress);

        Vector3 flatPos = Vector3.Lerp(_startPos, _targetPos, _progress);
        float arc = _arcHeight * Mathf.Sin(Mathf.PI * _progress);
        transform.position = new Vector3(flatPos.x, flatPos.y + arc, flatPos.z);

        if (_progress < 1f)
        {
            Vector3 nextFlat = Vector3.Lerp(_startPos, _targetPos, _progress + 0.01f);
            float nextArc = _arcHeight * Mathf.Sin(Mathf.PI * (_progress + 0.01f));
            Vector3 nextPos = new Vector3(nextFlat.x, nextFlat.y + nextArc, nextFlat.z);
            Quaternion lookRot = Quaternion.LookRotation(nextPos - transform.position);
            _spinAngle += 360f * Time.deltaTime;
            Quaternion spinRot = Quaternion.AngleAxis(_spinAngle, Vector3.right);
            transform.rotation = lookRot * spinRot;
        }

        if (_progress >= 1f)
            Impact();
    }

    private void Impact()
    {
        _launched = false;

        Collider[] hits = Physics.OverlapSphere(_targetPos, _explosionRadius, _damageLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(_damage);
        }

        if (_explosionVFX != null)
            Instantiate(_explosionVFX, _targetPos, Quaternion.identity);

        FireZone fireZone = Instantiate(_fireZonePrefab, _targetPos, Quaternion.identity).GetComponent<FireZone>();
        if (fireZone != null)
            fireZone.Init(_fireDuration, _fireDamagePerSecond, _fireRadius, _damageLayers);

        Destroy(gameObject);
    }
}