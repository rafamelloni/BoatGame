using UnityEngine;
using UnityEngine.UI;

public class PlayButton : MonoBehaviour
{
    [SerializeField] private AbilityController abilityController;
    [SerializeField] private RT_PlayerStats playerStats;
    [SerializeField] private GameObject canvas;
    [SerializeField] private GameObject canvasMain;
    [SerializeField] private GameObject cameraPreSeleciion;
    [SerializeField] private GameObject camaramMain;
    [SerializeField] private Movement _movement;
    [SerializeField] private EnemySpawner _enemySpawner;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    public void OnClick()
    {
        if (PreselectionData.Ability == SelectedAbility.None)
        {
            Debug.LogWarning("[PlayButton] No se eligió ninguna habilidad.");
            return;
        }

        if (PreselectionData.SelectedShipStats == null)
        {
            Debug.LogWarning("[PlayButton] No se eligió ningún barco.");
            return;
        }

        // Aplicar stats del barco elegido
        playerStats.SetBaseStats(PreselectionData.SelectedShipStats);

        switch (PreselectionData.Ability)
        {
            case SelectedAbility.Cannon:
                abilityController.CannonAveilable();
                break;
            case SelectedAbility.Mortar:
                abilityController.MortarAveilable();
                break;
        }

        StartGame();
    }

    public void StartGame()
    {
        _movement.SetMovementEnabled(true);
        _enemySpawner.ResumeSpawning();
        canvas.SetActive(false);
        cameraPreSeleciion.SetActive(false);
        camaramMain.SetActive(true);
        canvasMain.SetActive(true);
    }
}