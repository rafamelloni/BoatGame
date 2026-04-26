using System.Collections;
using UnityEngine;

public class DashTelegraph : MonoBehaviour
{
    [SerializeField] private LineRenderer _lineA;
    [SerializeField] private LineRenderer _lineB;
    [SerializeField] private LineRenderer _lineAStatic;
    [SerializeField] private LineRenderer _lineBStatic;
    [SerializeField] private float _separation = 1f;
    [SerializeField] private float _height;
    [SerializeField] private Transform _startPoint;

    private void Awake()
    {
        Hide();
    }

    public void Show(Vector3 from, Vector3 to, float telegraphDuration)
    {
        _lineA.enabled = true;
        _lineB.enabled = true;
        _lineAStatic.enabled = true;
        _lineBStatic.enabled = true;
        StopAllCoroutines();
        Vector3 origin = _startPoint != null ? _startPoint.position : from;
        StartCoroutine(Animate(origin, to, telegraphDuration));
    }

    public void Hide()
    {
        if (_lineA != null) _lineA.enabled = false;
        if (_lineB != null) _lineB.enabled = false;
        if (_lineAStatic != null) _lineAStatic.enabled = false;
        if (_lineBStatic != null) _lineBStatic.enabled = false;
        StopAllCoroutines();
    }

    private IEnumerator Animate(Vector3 from, Vector3 to, float duration)
    {
        float distance = Vector3.Distance(from, to);
        float separation = Mathf.Min(_separation, distance * 0.1f);
        Debug.Log($"Telegraph separation: {separation}, distance: {distance}");

        Vector3 dir = (to - from).normalized;
        Vector3 perp = Vector3.Cross(dir, Vector3.up).normalized;
        SetLine(_lineAStatic, from + perp * separation, to + perp * separation);
        SetLine(_lineBStatic, from - perp * separation, to - perp * separation);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float offset = Mathf.Lerp(separation, 0f, t);
            SetLine(_lineA, from + perp * offset, to + perp * offset);
            SetLine(_lineB, from - perp * offset, to - perp * offset);
            yield return null;
        }
        _lineA.enabled = false;
        _lineB.enabled = false;
    }

    private void SetLine(LineRenderer lr, Vector3 start, Vector3 end)
    {
        start.y = _height;
        end.y = _height;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }
}