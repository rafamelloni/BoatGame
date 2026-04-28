using UnityEngine;
using System.Collections;

public class TabUI : MonoBehaviour
{
    [SerializeField] Transform container;
    [SerializeField] GameObject panel;
    [SerializeField] float moveAmount = 1f;
    [SerializeField] float duration = 0.2f;

    Vector3 containerStartPos;
    Vector3 containerUpPos;
    bool isOpen = false;
    Coroutine currentRoutine;

    void Start()
    {
        containerStartPos = container.localPosition;
        containerUpPos = containerStartPos + new Vector3(0f, moveAmount, 0f);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            Animate(true);
        if (Input.GetKeyUp(KeyCode.Tab))
            Animate(false);
    }

    void Animate(bool open)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(MoveContainer(
            container.localPosition,
            open ? containerUpPos : containerStartPos,
            open
        ));
    }

    IEnumerator MoveContainer(Vector3 from, Vector3 to, bool open)
    {
        if (open) panel.SetActive(true);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            container.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        container.localPosition = to;
        currentRoutine = null;
    }
}