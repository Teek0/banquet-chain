using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public sealed class ScreenFader : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private bool startOpaque = true;

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        canvasGroup.alpha = startOpaque ? 1f : 0f;
        canvasGroup.blocksRaycasts = startOpaque;
    }

    public IEnumerator FadeTo(float targetAlpha, float duration)
    {
        targetAlpha = Mathf.Clamp01(targetAlpha);
        float startAlpha = canvasGroup.alpha;

        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            canvasGroup.blocksRaycasts = targetAlpha > 0.001f;
            yield break;
        }

        canvasGroup.blocksRaycasts = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                progress
            );
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        canvasGroup.blocksRaycasts = targetAlpha > 0.001f;
    }
}