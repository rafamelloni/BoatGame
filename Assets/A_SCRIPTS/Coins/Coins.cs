using UnityEngine;

public class Coins : MonoBehaviour
{
   
    [Header("Value")]
    [SerializeField] private int value = 1;

    [Header("Drop")]
    [SerializeField] private float scatterRadius = 1.5f;
    [SerializeField] private float dropDuration = 0.25f;
    [SerializeField] private float dropArcHeight = 0.5f;
    [SerializeField] private float hoverHeight = 1.2f;

    [Header("Idle")]
    [SerializeField] private float floatAmplitude = 0.15f;
    [SerializeField] private float floatFrequency = 2f;
    [SerializeField] private float rotationSpeed = 90f;

    private Vector3 startPos;
    private Vector3 targetPos;

    private float dropTimer;
    private float baseY;
    public float floatOffset;

    private bool isDropping;

    public int Value => value;

    public void Init(Transform spawnPoint)
    {
        startPos = spawnPoint.position;

        Vector2 random = Random.insideUnitCircle * scatterRadius;
        targetPos = startPos + new Vector3(random.x, 0f, random.y);
        targetPos.y = startPos.y + hoverHeight;

        dropTimer = 0f;
        floatOffset = Random.Range(0f, 10f);
        isDropping = true;

        transform.position = startPos;
    }

    private void Update()
    {
        if (isDropping)
        {
            UpdateDrop();
        }
        else
        {
            UpdateIdle();
        }
    }

    private void UpdateDrop()
    {
        dropTimer += Time.deltaTime;
        float t = Mathf.Clamp01(dropTimer / dropDuration);

        Vector3 pos = Vector3.Lerp(startPos, targetPos, t);
        pos.y += Mathf.Sin(t * Mathf.PI) * dropArcHeight;

        transform.position = pos;

        if (t >= 1f)
        {
            isDropping = false;
            baseY = transform.position.y;
        }
    }

    private void UpdateIdle()
    {
        Vector3 pos = transform.position;
        pos.y = baseY + Mathf.Sin((Time.time + floatOffset) * floatFrequency) * floatAmplitude;
        transform.position = pos;

        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
    }

    public int Collect()
    {
        gameObject.SetActive(false);
        return value;
    }
}

