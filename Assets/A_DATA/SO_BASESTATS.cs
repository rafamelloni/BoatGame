using UnityEngine;

[CreateAssetMenu(fileName = "SO_BASESTATS", menuName = "Scriptable Objects/SO_BASESTATS")]
public class SO_BASESTATS : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float turnSpeed = 90f;

    [Header("Inertia")]
    public float acceleration = 5f;
    public float deceleration = 4f;

    [Header("Sprint")]
    public float sprintMultiplier = 1.6f;
}
