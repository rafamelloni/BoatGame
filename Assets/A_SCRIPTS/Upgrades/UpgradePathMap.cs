using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradePathMap : MonoBehaviour
{
    [Header("Nodos (en orden I, II, III, IV)")]
    [SerializeField] private TextMeshProUGUI[] nodeTexts;
    [SerializeField] private Image[] nodeCircles;

    [Header("Colores")]
    [SerializeField] private Color colorNotTaken;
    [SerializeField] private Color colorNext;
    [SerializeField] private Color colorCompleted;

    [Header("Referencias")]
    [SerializeField] private RT_PlayerUpgrades playerUpgrades;

    [Header("Tooltip Cofre")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipTitle;
    [SerializeField] private TextMeshProUGUI tooltipDescription;

    private SO_UpgradePath _currentPath;

    public void UpdateMap(SO_UpgradePath path)
    {
        _currentPath = path;
        int currentLevel = playerUpgrades.GetLevel(path);

        for (int i = 0; i < nodeTexts.Length; i++)
        {
            if (i < currentLevel)
            {
                nodeTexts[i].color = colorCompleted;
                nodeCircles[i].enabled = true;
            }
            else if (i == currentLevel)
            {
                nodeTexts[i].color = colorNext;
                nodeCircles[i].enabled = true;
            }
            else
            {
                nodeTexts[i].color = colorNotTaken;
                nodeCircles[i].enabled = false;
            }
        }
    }


    public void ShowTooltip()
    {
        if (_currentPath == null) return;

        SO_UpgradeStep tier4 = _currentPath.GetStep(3);
        if (tier4 == null) return;

        tooltipTitle.text = tier4.displayName;
        tooltipDescription.text = tier4.description;
        tooltipPanel.SetActive(true);
    }

    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }

    public void ResetMap()
    {
        foreach (var node in nodeTexts)
            node.color = colorNotTaken;

        foreach (var circle in nodeCircles)
            circle.enabled = false;
    }
}