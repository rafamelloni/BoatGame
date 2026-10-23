using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{

    [SerializeField]GameObject _preseleccion;
    [SerializeField]GameObject MenuPrefab;

    [Header("Overworld")]
    [Tooltip("Escena del mapa de oceanos. Si esta vacia o no esta en Build Settings, Play va directo a la preseleccion como antes.")]
    [SerializeField] string _overworldScene = "Overworld";

    private void Start()
    {
        // Si venimos del Overworld (ya se eligio el oceano), salteamos el menu
        if (OverworldSession.ConsumeSkipMenu())
            OpenPreselection();
    }

    public void PlayButtonOnClick()
    {
        if (!string.IsNullOrEmpty(_overworldScene) && Application.CanStreamedLevelBeLoaded(_overworldScene))
        {
            SceneManager.LoadScene(_overworldScene);
            return;
        }
        OpenPreselection();
    }

    private void OpenPreselection()
    {
        _preseleccion.SetActive(true);
        gameObject.SetActive(false);
        MenuPrefab.SetActive(false);
    }
}
