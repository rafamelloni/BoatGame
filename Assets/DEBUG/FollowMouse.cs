using UnityEngine;

public class FollowMouse : MonoBehaviour
{
    public Vector3 offset;
    void Update()
    {
        transform.position  = Input.mousePosition + offset;
    }
}