using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UpgradeUI : MonoBehaviour
{
    [Header("Sistema")]
    [SerializeField] private UpgradeOfferSystem offerSystem;
    [SerializeField] private UpgradeSystem upgradeSystem;
    [SerializeField] private RT_PlayerUpgrades playerUpgrades;

    [Header("Mapa")]
    [SerializeField] private UpgradePathMap pathMap;


    [Header("Botones de paths")]
    [SerializeField] private Button[] pathButtons;
   // [SerializeField] private TextMeshProUGUI[] pathNames;

    [Header("Info del path seleccionado")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Confirmar")]
    [SerializeField] private Button confirmButton;

    [Header("Rebordes seleccionados")]
    [SerializeField] private GameObject[] selectedRebordes;

    private List<SO_UpgradePath> _currentOffer;
    private SO_UpgradePath _selectedPath;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirm);

        for (int i = 0; i < pathButtons.Length; i++)
        {
            int index = i;
            pathButtons[i].onClick.AddListener(() => OnPathSelected(index));
        }

        confirmButton.interactable = false;
    }

    private void OnEnable()
    {
        Show();
        pathMap.ResetMap();
    }

    public void Show()
    {
        _selectedPath = null;
        _currentOffer = offerSystem.GetOffer(3);

        for (int i = 0; i < pathButtons.Length; i++)
        {
            if (i < _currentOffer.Count)
            {
                pathButtons[i].gameObject.SetActive(true);
                pathButtons[i].GetComponent<Image>().sprite = _currentOffer[i].pathIcon;

                //pathNames[i].text = _currentOffer[i].pathName;
            }
            else
            {
                pathButtons[i].gameObject.SetActive(false);
            }
        }

        foreach (var r in selectedRebordes)
            r.SetActive(false);

        titleText.text = "";
        descriptionText.text = "";
        confirmButton.interactable = false;

        gameObject.SetActive(true);
    }

    private void OnPathSelected(int index)
    {
        if (index >= _currentOffer.Count) return;

        // Resetear todos al sprite normal
        for (int i = 0; i < _currentOffer.Count; i++)
            pathButtons[i].GetComponent<Image>().sprite = _currentOffer[i].pathIcon;

        // Seleccionado usa el sprite alternativo
        pathButtons[index].GetComponent<Image>().sprite = _currentOffer[index].pathIconSelected;

        // Resetear todos los rebordes
        for (int i = 0; i < selectedRebordes.Length; i++)
            selectedRebordes[i].SetActive(false);

        // Activar el seleccionado
        selectedRebordes[index].SetActive(true);

        _selectedPath = _currentOffer[index];
        SO_UpgradeStep nextStep = playerUpgrades.GetNextStep(_selectedPath);
        if (nextStep != null)
        {
            titleText.text = _selectedPath.pathName;
            descriptionText.text = nextStep.description;
        }

        pathMap.UpdateMap(_selectedPath);
        confirmButton.interactable = true;
    }

    private void OnConfirm()
    {
        if (_selectedPath == null) return;

        upgradeSystem.ApplyUpgrade(_selectedPath);
        Hide();
    }

    private void Hide()
    {
        Time.timeScale = 1f;
        _selectedPath = null;
        gameObject.SetActive(false);
    }
}