using UnityEngine;

public class MenuManager : MonoBehaviour
{

    [SerializeField]GameObject _preseleccion;
    [SerializeField]GameObject MenuPrefab;
    public void PlayButtonOnClick()
    {
        _preseleccion.SetActive(true);
        gameObject.SetActive(false);
        MenuPrefab.SetActive(false);
    }
}
