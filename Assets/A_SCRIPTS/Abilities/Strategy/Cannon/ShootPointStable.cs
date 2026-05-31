using UnityEngine;
public class ShootPointStabilizer : MonoBehaviour
{
    [SerializeField] Transform _boat;
    public bool inv = false;

    void LateUpdate()
    {
        if (inv)
            transform.rotation = Quaternion.Euler(0f, _boat.eulerAngles.y + 180f, 0f);
        else
            transform.rotation = Quaternion.Euler(0f, _boat.eulerAngles.y, 0f);
    }
}