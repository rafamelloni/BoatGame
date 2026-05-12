using UnityEngine;

public class ShootPointStabilizer : MonoBehaviour
{
    [SerializeField] Transform _boat;

    private Vector3 _localOffsetFromBoat;

    void Start()
    {
        // Offset del shootpoint relativo al boat, en el espacio del boat sin tilt
        Quaternion boatYOnly = Quaternion.Euler(0f, _boat.eulerAngles.y, 0f);
        _localOffsetFromBoat = Quaternion.Inverse(boatYOnly) * (transform.position - _boat.position);
    }

    void LateUpdate()
    {
        // Reconstruye la posicion usando solo la Y del boat
        Quaternion boatYOnly = Quaternion.Euler(0f, _boat.eulerAngles.y, 0f);
        transform.position = _boat.position + boatYOnly * _localOffsetFromBoat;
        transform.rotation = boatYOnly;
    }
}