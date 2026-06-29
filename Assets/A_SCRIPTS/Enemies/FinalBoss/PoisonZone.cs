using UnityEngine;

public class PoisonZone : MonoBehaviour
{
    [SerializeField] float duration = 3f;
    float age;

    void Update()
    {
        age += Time.deltaTime;
        if (age >= duration)
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerPoison>(out var poison))
            poison.EnterZone();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<PlayerPoison>(out var poison))
            poison.ExitZone();
    }
}