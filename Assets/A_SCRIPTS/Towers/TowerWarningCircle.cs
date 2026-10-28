using UnityEngine;

// Círculo de aviso en el agua donde va a caer la bala del mortero.
// Tiene un borde fijo y un relleno que crece de 0 al radio completo mientras la bala vuela.
// Si la torre no tiene prefab de aviso, arma uno solo con dos discos.
public class TowerWarningCircle : MonoBehaviour
{
    [Tooltip("Disco del tamaño final (borde / fondo)")]
    [SerializeField] private Transform _outer;
    [Tooltip("Disco que se va llenando")]
    [SerializeField] private Transform _fill;
    [Tooltip("Altura sobre el agua para que no parpadee con la superficie")]
    [SerializeField] private float _heightOffset = 0.05f;

    private float _radius = 1f;

    public bool IsFree => !gameObject.activeSelf;

    public void Setup(Transform outer, Transform fill)
    {
        _outer = outer;
        _fill = fill;
    }

    public void Show(Vector3 position, float radius)
    {
        _radius = radius;
        transform.position = position + Vector3.up * _heightOffset;
        transform.rotation = Quaternion.identity;
        if (_outer != null) _outer.localScale = new Vector3(radius * 2f, _outer.localScale.y, radius * 2f);
        SetProgress(0f);
        gameObject.SetActive(true);
    }

    public void SetProgress(float k)
    {
        if (_fill == null) return;
        float r = _radius * 2f * Mathf.Clamp01(k);
        _fill.localScale = new Vector3(r, _fill.localScale.y, r);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
