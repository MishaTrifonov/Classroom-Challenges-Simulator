using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TeacherBagUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject bagPanel;

    [Header("Refs")]
    public ClassroomManager classroomManager;

    [Header("Animation")]
    public CanvasGroup bagGroup;
    public RectTransform bagRect;
    public float animDuration = 0.18f;
    public Vector3 openScale = Vector3.one;
    public Vector3 closedScale = new Vector3(0.9f, 0.9f, 1f);

    private bool isOpen;
    private Coroutine animCo;

    private void Awake()
    {
        if (bagGroup == null) bagGroup = bagPanel.GetComponent<CanvasGroup>();
        if (bagRect == null) bagRect = bagPanel.GetComponent<RectTransform>();

        SetInstant(false); // start closed
    }

    public void ToggleBag()
    {
        SetOpen(!isOpen);
    }

    public void SetOpen(bool open)
    {
        isOpen = open;

        if (animCo != null) StopCoroutine(animCo);
        animCo = StartCoroutine(Animate(open));
    }

    private IEnumerator Animate(bool open)
    {
        // Ensure object is active so we can animate
        if (bagPanel != null && !bagPanel.activeSelf)
            bagPanel.SetActive(true);

        float t = 0f;

        float startA = bagGroup.alpha;
        float endA   = open ? 1f : 0f;

        Vector3 startS = bagRect.localScale;
        Vector3 endS   = open ? openScale : closedScale;

        // clickable only when open (or while opening)
        bagGroup.blocksRaycasts = true;
        bagGroup.interactable = open;

        while (t < animDuration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / animDuration);

            // Smooth step
            u = u * u * (3f - 2f * u);

            bagGroup.alpha = Mathf.Lerp(startA, endA, u);
            bagRect.localScale = Vector3.Lerp(startS, endS, u);

            yield return null;
        }

        bagGroup.alpha = endA;
        bagRect.localScale = endS;

        // After closing: disable clicks and optionally disable object
        if (!open)
        {
            bagGroup.blocksRaycasts = false;
            bagGroup.interactable = false;

            // Optional: keep active (recommended) OR set inactive:
            // bagPanel.SetActive(false);
        }
    }
    private void SetInstant(bool open)
    {
        isOpen = open;

        if (bagPanel != null && !bagPanel.activeSelf)
            bagPanel.SetActive(true);

        bagGroup.alpha = open ? 1f : 0f;
        bagRect.localScale = open ? openScale : closedScale;
        bagGroup.interactable = open;
        bagGroup.blocksRaycasts = open;
    }

}
