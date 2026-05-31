using UnityEngine;
public class ShootPointStabilizer : MonoBehaviour
{
    [SerializeField] Transform _boat;
    private Vector3 _localOffsetFromBoat;
    public bool inv = false;
    void Start()
    {
        // Offset del shootpoint relativo al boat, en el espacio del boat sin tilt
        Quaternion boatYOnly = Quaternion.Euler(0f, _boat.eulerAngles.y, 0f);
        _localOffsetFromBoat = Quaternion.Inverse(boatYOnly) * (transform.position - _boat.position);
    }
    void LateUpdate()
    {
        // Reconstruye la posicion usando solo la Y del boat
        if (inv)
        {
            Quaternion boatYOnly = Quaternion.Euler(0f, _boat.eulerAngles.y +180f, 0f);
            transform.position = _boat.position + boatYOnly * _localOffsetFromBoat;
            transform.rotation = boatYOnly;
        }
        else
        {
            Quaternion boatYOnly = Quaternion.Euler(0f, _boat.eulerAngles.y, 0f);
            transform.position = _boat.position + boatYOnly * _localOffsetFromBoat;
            transform.rotation = boatYOnly;
        }

    }
}