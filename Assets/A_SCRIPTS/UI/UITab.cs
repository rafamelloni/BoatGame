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
    bool isAnimating;

    void Start()
    {
        containerStartPos = container.localPosition;
        containerUpPos = containerStartPos + new Vector3(0f, moveAmount, 0f);
        //panel.SetActive(false);
    }

    void Update()
    {
        if (isAnimating) return;

        if (Input.GetKeyDown(KeyCode.Tab) && !isAnimating)
            StartCoroutine(OpenUI());

        if (Input.GetKeyUp(KeyCode.Tab) && !isAnimating)
            StartCoroutine(CloseUI());
    }

    IEnumerator OpenUI()
    {
        isAnimating = true;
        panel.SetActive(true);
        yield return MoveContainer(containerStartPos, containerUpPos);
        isAnimating = false;
    }

    IEnumerator CloseUI()
    {
        isAnimating = true;
        yield return MoveContainer(containerUpPos, containerStartPos);
        //panel.SetActive(false);
        isAnimating = false;
    }

    IEnumerator MoveContainer(Vector3 from, Vector3 to)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            container.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        container.localPosition = to;
    }
}