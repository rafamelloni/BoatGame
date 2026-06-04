using UnityEngine;

public class UIToggleAnimator : MonoBehaviour
{
    [Header("Objetos")]
    [SerializeField] private GameObject spriteA;
    [SerializeField] private GameObject spriteB;

    [Header("Tiempo")]
    [SerializeField] private float minSwitchTime = 0.15f;
    [SerializeField] private float maxSwitchTime = 0.4f;

    [Header("Inicio")]
    [SerializeField] private bool startWithA = true;

    private float timer;
    private float nextSwitchTime;
    private bool showingA;

    private void Start()
    {
        showingA = startWithA;

        spriteA.SetActive(showingA);
        spriteB.SetActive(!showingA);

        SetNextTime();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= nextSwitchTime)
        {
            timer = 0f;

            showingA = !showingA;

            spriteA.SetActive(showingA);
            spriteB.SetActive(!showingA);

            SetNextTime();
        }
    }

    private void SetNextTime()
    {
        nextSwitchTime = Random.Range(minSwitchTime, maxSwitchTime);
    }
}