using UnityEngine;

public class DebugSetActive : MonoBehaviour
{
    private void OnDisable()
    {
        Debug.Log($"[DebugSetActive] {gameObject.name} desactivado\n{System.Environment.StackTrace}");
    }
}