using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Cloth))]
public class SailCloth : MonoBehaviour
{
    [Header("Referencias")]
    public Movement movement;
    public SphereCollider inflator;   // posicionala a mano, SIEMPRE queda ahí fija, solo cambia el radio
    public SphereCollider backstop;   // esfera grande fija, atrás, para que no se hunda para el otro lado

    [Header("Radio (panza)")]
    public float idleRadius = 3f;
    public float maxRadius = 20f;
    public float radiusLerpSpeed = 3f;

    [Header("Curva de inflado")]
    [Range(0.5f, 6f)]
    public float inflateCurveExponent = 2.5f; // 1 = lineal. Más alto = arranca lento y se dispara cerca del tope (más violento)

    [Header("Respiración en reposo")]
    public float idleNoiseStrength = 0.3f;
    public float idleNoiseSpeed = 1.5f;

    [Header("Viento / turbulencia (opcional)")]
    public Vector3 windDirectionLocal = Vector3.forward;
    public float windForceIdle = 0.5f;
    public float windForceMoving = 4f;
    public float windLerpSpeed = 3f;
    public float idleRandomAccel = 0.15f;
    public float movingRandomAccel = 2f;

    private Cloth _cloth;
    private float _currentRadius;
    private float _noiseSeed;

    private void Awake()
    {
        _cloth = GetComponent<Cloth>();
        _noiseSeed = Random.Range(0f, 100f);
        _currentRadius = idleRadius;

        if (inflator != null) inflator.isTrigger = true;
        if (backstop != null) backstop.isTrigger = true;

        var spheres = new List<ClothSphereColliderPair>();
        if (inflator != null) spheres.Add(new ClothSphereColliderPair(inflator));
        if (backstop != null) spheres.Add(new ClothSphereColliderPair(backstop));
        _cloth.sphereColliders = spheres.ToArray();
    }

    private void Update()
    {
        float speedRatio = movement != null ? movement.SpeedRatio : 0f;
        bool isMoving = speedRatio > 0.05f;

        // curva exponencial: a bajas velocidades casi no infla, y cerca del máximo se dispara
        float curvedRatio = Mathf.Pow(speedRatio, inflateCurveExponent);

        float targetRadius = Mathf.Lerp(idleRadius, maxRadius, curvedRatio);
        _currentRadius = Mathf.Lerp(_currentRadius, targetRadius, Time.deltaTime * radiusLerpSpeed);

        float finalRadius = _currentRadius;
        if (!isMoving)
        {
            float breathing = Mathf.Sin((Time.time + _noiseSeed) * idleNoiseSpeed) * idleNoiseStrength;
            finalRadius = Mathf.Max(0.1f, _currentRadius + breathing);
        }

        if (inflator != null)
            inflator.radius = finalRadius;

        Vector3 windDirWorld = transform.TransformDirection(windDirectionLocal.normalized);
        float targetWind = Mathf.Lerp(windForceIdle, windForceMoving, curvedRatio);
        _cloth.externalAcceleration = Vector3.Lerp(_cloth.externalAcceleration, windDirWorld * targetWind, Time.deltaTime * windLerpSpeed);

        float targetRandom = Mathf.Lerp(idleRandomAccel, movingRandomAccel, curvedRatio);
        _cloth.randomAcceleration = Vector3.one * targetRandom;
    }
}