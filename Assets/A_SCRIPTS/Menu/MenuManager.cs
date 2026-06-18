using UnityEngine;

public class MenuManager : MonoBehaviour
{

    [SerializeField]GameObject _preseleccion;
    public void PlayButtonOnClick()
    {
        _preseleccion.SetActive(true);
        gameObject.SetActive(false);
    }
}
