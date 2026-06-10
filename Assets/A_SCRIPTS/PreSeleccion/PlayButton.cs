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
    [SerializeField] private IslandSpawnManager _islandSpawner;
    [SerializeField] private PlayerUpgrades _playerUpgrades;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    public void OnClick()
    {
        if (PreselectionData.Ability == SelectedAbility.None)
        {
            Debug.LogWarning("[PlayButton] No se eligió ningún barco.");
            return;
        }

        if (PreselectionData.SelectedShipStats == null)
        {
            Debug.LogWarning("[PlayButton] No se eligió ningún barco.");
            return;
        }

        playerStats.SetBaseStats(PreselectionData.SelectedShipStats);
        _playerUpgrades.Setup(playerStats);

       

        StartGame();
    }

    public void StartGame()
    {
        _movement.SetMovementEnabled(true);
        _islandSpawner.StartSpawning();
        _enemySpawner.StartSpawning();
        canvas.SetActive(false);
        cameraPreSeleciion.SetActive(false);
        camaramMain.SetActive(true);
        canvasMain.SetActive(true);
    }
}