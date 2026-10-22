using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Muestra la munición del cañón. Usá cualquiera de las dos opciones (o las dos):
//  - _label: un TextMeshPro que muestra "2/2"
//  - _pips: imágenes puestas a mano (una por bala). Se prenden tantas como el máximo
//    y se pintan llenas/vacías según las balas que quedan. Poné tantas como el máximo
//    que pueda llegar a tener (ej. 6) y las que sobran quedan apagadas.
public class CannonAmmoUI : MonoBehaviour
{
    [SerializeField] private AbilityController _abilityController;

    [Header("Texto (opcional)")]
    [SerializeField] private TMP_Text _label;

    [Header("Íconos por bala (opcional)")]
    [SerializeField] private Image[] _pips;
    [SerializeField] private Color _fullColor = Color.white;
    [SerializeField] private Color _emptyColor = new Color(1f, 1f, 1f, 0.25f);

    private CannonStrategy _cannon;

    private void Start()
    {
        // CannonStrategy se crea en el Awake de AbilityController
        _cannon = _abilityController != null ? _abilityController.CannonAbility : null;
        if (_cannon == null) return;
        _cannon.OnAmmoChanged += Refresh;
        Refresh(_cannon.CurrentAmmo, _cannon.MaxAmmo);
    }

    private void OnDestroy()
    {
        if (_cannon != null) _cannon.OnAmmoChanged -= Refresh;
    }

    private void Refresh(int current, int max)
    {
        if (_label != null) _label.text = $"{current}/{max}";

        if (_pips == null) return;
        for (int i = 0; i < _pips.Length; i++)
        {
            if (_pips[i] == null) continue;
            bool exists = i < max;
            _pips[i].gameObject.SetActive(exists);
            if (exists) _pips[i].color = i < current ? _fullColor : _emptyColor;
        }
    }
}
