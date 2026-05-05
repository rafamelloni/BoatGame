using UnityEngine;
using UnityEngine.UI;

public class PlayButton : MonoBehaviour
{
    [SerializeField] private AbilityController abilityController;
    [SerializeField] private GameObject canvas;
    [SerializeField] private GameObject canvasMain;
    [SerializeField] private GameObject cameraPreSeleciion;
    [SerializeField] private GameObject camaramMain;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    public void OnClick()
    {
        switch (PreselectionData.Ability)
        {
            case SelectedAbility.Cannon:
                abilityController.CannonAveilable();
                StartGame();
                break;

            case SelectedAbility.Mortar:
                abilityController.MortarAveilable();
                StartGame();
                break;

            case SelectedAbility.None:
                Debug.LogWarning("[PlayButton] No se eligió ninguna habilidad.");
                return;
        }

        //PreselectionData.Reset();
        // Acá cargás la escena de juego o activás lo que necesites
        // SceneManager.LoadScene("GameScene");
    }


    public void StartGame()
    {
        canvas.SetActive(false);
        cameraPreSeleciion.SetActive(false);
        camaramMain.SetActive(true);
        canvasMain.SetActive(true);
    }
}