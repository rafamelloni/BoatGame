using UnityEngine;
using UnityEngine.UI;

public class BarraAnimacion : MonoBehaviour
{
    [Header("Barra de vida original")]
    [SerializeField] private Image barraVidaReferencia;

    [Header("Sprites animados")]
    [SerializeField] private GameObject spriteA;
    [SerializeField] private GameObject spriteB;

    [Header("Tiempo")]
    [SerializeField] private float minSwitchTime = 0.15f;
    [SerializeField] private float maxSwitchTime = 0.4f;

    [Header("Inicio")]
    [SerializeField] private bool startWithA = true;

    private Image imageA;
    private Image imageB;

    private float timer;
    private float nextSwitchTime;
    private bool showingA;

    private void Awake()
    {
        if (spriteA != null)
            imageA = spriteA.GetComponent<Image>();

        if (spriteB != null)
            imageB = spriteB.GetComponent<Image>();
    }

    private void Start()
    {
        showingA = startWithA;

        if (spriteA != null)
            spriteA.SetActive(showingA);

        if (spriteB != null)
            spriteB.SetActive(!showingA);

        SetNextTime();
        ActualizarFillAmount();
    }

    private void Update()
    {
        ActualizarFillAmount();

        timer += Time.deltaTime;

        if (timer >= nextSwitchTime)
        {
            timer = 0f;

            showingA = !showingA;

            if (spriteA != null)
                spriteA.SetActive(showingA);

            if (spriteB != null)
                spriteB.SetActive(!showingA);

            SetNextTime();
        }
    }

    private void ActualizarFillAmount()
    {
        if (barraVidaReferencia == null)
            return;

        float fill = barraVidaReferencia.fillAmount;

        if (imageA != null)
            imageA.fillAmount = fill;

        if (imageB != null)
            imageB.fillAmount = fill;
    }

    private void SetNextTime()
    {
        nextSwitchTime = Random.Range(minSwitchTime, maxSwitchTime);
    }
}