using UnityEngine;

// Menú dentro de la escena del juego. El menú principal ahora vive en la escena MainMenu,
// así que acá se saltea y se abre directo la preselección de barco.
public class MenuManager : MonoBehaviour
{
    [SerializeField] GameObject _preseleccion;
    [SerializeField] GameObject MenuPrefab;

    [Tooltip("Abrir la preselección apenas arranca la escena (el menú principal está en MainMenu)")]
    [SerializeField] bool _skipMenuOnStart = true;

    private void Start()
    {
        if (_skipMenuOnStart) OpenPreselection();
    }

    public void PlayButtonOnClick()
    {
        OpenPreselection();
    }

    private void OpenPreselection()
    {
        _preseleccion.SetActive(true);
        gameObject.SetActive(false);
        MenuPrefab.SetActive(false);
    }
}
