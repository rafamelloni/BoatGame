using System.Collections;
using UnityEngine;

public class UIElementSpawn : MonoBehaviour
{
    [Header("Pergamino")]
    [SerializeField] private Material _pergaminoMaterial;
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private float _duration = 0.5f;
    [SerializeField] private AnimationCurve _curve = AnimationCurve.EaseInOut(0, 0, 1, 1);


    [Header("Elementos activar")]
    [SerializeField]  private GameObject _dataTarjetaUI ;
    [SerializeField] private GameObject _dataTarjetaUI1 ;
    [SerializeField] private GameObject _dataTarjetaUI2 ;

    private float _originalVal;
    private Vector2 _originalPos;

    private bool secuenciaIniciada;

    private void Awake()
    {
        _originalVal = _pergaminoMaterial.GetFloat("_valorScroll");
        _originalPos = _rectTransform.anchoredPosition;
    }
    private IEnumerator OpenPergamino()
    {
        float startVal = _pergaminoMaterial.GetFloat("_valorScroll");
        float endVal = -0.64f;
        Vector2 startPos = _rectTransform.anchoredPosition;
        Vector2 endPos = new Vector2(startPos.x, -24.8f);

        float t = 0f;
        while (t < _duration)
        {
            t += Time.unscaledDeltaTime;
            float progress = _curve.Evaluate(t / _duration);
            _pergaminoMaterial.SetFloat("_valorScroll", Mathf.Lerp(startVal, endVal, progress));
            _rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, progress);
            yield return null;
        }

        _pergaminoMaterial.SetFloat("_valorScroll", endVal);
        _rectTransform.anchoredPosition = endPos;
        ActivarElementos();
    }

    void ActivarElementos()
    {
        if (_dataTarjetaUI != null) _dataTarjetaUI.SetActive(true);
        if (_dataTarjetaUI1 != null) _dataTarjetaUI1.SetActive(true);
        if (_dataTarjetaUI2 != null) _dataTarjetaUI2.SetActive(true);
    }

    private void OnDisable()
    {
        secuenciaIniciada = false;
        _pergaminoMaterial.SetFloat("_valorScroll", _originalVal);
        _rectTransform.anchoredPosition = _originalPos;

        if (_dataTarjetaUI != null) _dataTarjetaUI.SetActive(false);
        if (_dataTarjetaUI1 != null) _dataTarjetaUI1.SetActive(false);
        if (_dataTarjetaUI2 != null) _dataTarjetaUI2.SetActive(false);
    }
    private void OnEnable()
    {

        IniciarSecuencia();
    }
    public void IniciarSecuencia()
    {
        if (secuenciaIniciada) return;

        secuenciaIniciada = true;
        StartCoroutine(OpenPergamino());
    }





}