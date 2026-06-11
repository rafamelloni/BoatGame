using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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

    [Header("Info del path seleccionado")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Confirmar")]
    [SerializeField] private Button confirmButton;

    [Header("Rebordes seleccionados")]
    [SerializeField] private GameObject[] selectedRebordes;

    [Header("Pergamino")]
    [SerializeField] private PergaminoSlideByVisualBar pergamino;

    private List<SO_UpgradePath> _currentOffer;
    private SO_UpgradePath _selectedPath;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirm);

        for (int i = 0; i < pathButtons.Length; i++)
        {
            int index = i;
            pathButtons[i].onClick.AddListener(() => OnPathSelected(index));

            var trigger = pathButtons[i].gameObject.AddComponent<EventTrigger>();

            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => selectedRebordes[index].SetActive(true));
            trigger.triggers.Add(enterEntry);

            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ => selectedRebordes[index].SetActive(_selectedPath == _currentOffer[index]));
            trigger.triggers.Add(exitEntry);
        }

        confirmButton.interactable = false;
    }

    private void OnEnable()
    {
        RefreshOffer();
        pathMap.ResetMap();
    }

    private void RefreshOffer()
    {
        _selectedPath = null;
        _currentOffer = offerSystem.GetOffer(3);

        for (int i = 0; i < pathButtons.Length; i++)
        {
            if (i < _currentOffer.Count)
            {
                pathButtons[i].gameObject.SetActive(true);
                pathButtons[i].GetComponent<Image>().sprite = _currentOffer[i].pathIconGrey;
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
    }

    private void OnPathSelected(int index)
    {
        if (index >= _currentOffer.Count) return;

        for (int i = 0; i < _currentOffer.Count; i++)
            pathButtons[i].GetComponent<Image>().sprite = _currentOffer[i].pathIconGrey;

        pathButtons[index].GetComponent<Image>().sprite = _currentOffer[index].pathIconSelected;

        for (int i = 0; i < selectedRebordes.Length; i++)
            selectedRebordes[i].SetActive(i == index);

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
        confirmButton.interactable = false;
        pergamino.ClosePergamino(() => RefreshOffer());
    }
}