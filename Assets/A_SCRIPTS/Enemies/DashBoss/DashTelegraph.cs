using System.Collections;
using UnityEngine;

public class DashTelegraph : MonoBehaviour
{
    [SerializeField] private LineRenderer _lineA;
    [SerializeField] private LineRenderer _lineB;
    [SerializeField] private float _separation = 1f; // distancia inicial entre las dos líneas
    [SerializeField] private float _height; // distancia inicial entre las dos líneas
    [SerializeField] private Transform _startPoint; // distancia inicial entre las dos líneas

    private void Awake()
    {
        Hide();
    }

    public void Show(Vector3 from, Vector3 to, float telegraphDuration)
    {
        _lineA.enabled = true;
        _lineB.enabled = true;
        StopAllCoroutines();
        Vector3 origin = _startPoint != null ? _startPoint.position : from;
        StartCoroutine(Animate(origin, to, telegraphDuration));
    }

    public void Hide()
    {
        if (_lineA != null) _lineA.enabled = false;
        if (_lineB != null) _lineB.enabled = false;
        StopAllCoroutines();
    }

    private IEnumerator Animate(Vector3 from, Vector3 to, float duration)
    {
        Vector3 dir = (to - from).normalized;
        Vector3 perp = Vector3.Cross(dir, Vector3.up).normalized;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float offset = Mathf.Lerp(_separation, 0f, t);

            SetLine(_lineA, from + perp * offset, to + perp * offset);
            SetLine(_lineB, from - perp * offset, to - perp * offset);

            yield return null;
        }
    }

    private void SetLine(LineRenderer lr, Vector3 start, Vector3 end)
    {
        start.y = _height;
        end.y = _height;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }
}