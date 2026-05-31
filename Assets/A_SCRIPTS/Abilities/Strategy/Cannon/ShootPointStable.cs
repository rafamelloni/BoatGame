using UnityEngine;
public class ShootPointStabilizer : MonoBehaviour
{
    [SerializeField] private Transform _boat;
    [SerializeField] private Transform _cannon;
    public bool inv = false;

    private Vector3 _localOffsetFromBoat;

    void Start()
    {
        Quaternion boatYOnly = Quaternion.Euler(0f, _boat.eulerAngles.y, 0f);
        _localOffsetFromBoat = Quaternion.Inverse(boatYOnly) * (transform.position - _boat.position);
    }

    void LateUpdate()
    {
        Quaternion boatYOnly = Quaternion.Euler(0f, _boat.eulerAngles.y, 0f);
        transform.position = _boat.position + boatYOnly * _localOffsetFromBoat;

        if (inv)
            transform.rotation = Quaternion.Euler(0f, _cannon.eulerAngles.y + 180f, 0f);
        else
            transform.rotation = Quaternion.Euler(0f, _cannon.eulerAngles.y, 0f);
    }
}