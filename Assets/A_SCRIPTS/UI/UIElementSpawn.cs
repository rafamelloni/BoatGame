using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIElementSpawn : MonoBehaviour
{
    [Header("Pergamino")]
    [SerializeField] private Material _pergaminoMaterial;
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private float _duration = 0.5f;
    [SerializeField] private AnimationCurve _curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Elementos activar")]
    [SerializeField] private GameObject[] _dataTarjetasUI;

    private float _originalVal;
    private Vector2 _originalPos;
    private bool _secuenciaIniciada;
    private GameObject _tarjetaActiva;

    private void Awake()
    {
        _originalVal = _pergaminoMaterial.GetFloat("_valorScroll");
        _originalPos = _rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        IniciarSecuencia();
    }

    private void OnDisable()
    {
        _secuenciaIniciada = false;
        _pergaminoMaterial.SetFloat("_valorScroll", _originalVal);
        _rectTransform.anchoredPosition = _originalPos;

        if (_tarjetaActiva != null)
        {
            _tarjetaActiva.SetActive(false);
            _tarjetaActiva = null;
        }
    }

    public void IniciarSecuencia()
    {
        if (_secuenciaIniciada) return;
        _secuenciaIniciada = true;
        StartCoroutine(OpenPergamino());
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
        ActivarRandom();
    }

    private void ActivarRandom()
    {
        if (_dataTarjetasUI.Length == 0) return;

        int index = Random.Range(0, _dataTarjetasUI.Length);
        _tarjetaActiva = _dataTarjetasUI[index];
        _tarjetaActiva.SetActive(true);
    }

    public void MarcarSeleccionada()
    {
        if (_tarjetaActiva == null) return;
        _tarjetaActiva.SetActive(false);
        _tarjetaActiva = null;
    }
}