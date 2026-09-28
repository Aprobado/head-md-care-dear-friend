using System.Collections;
using UnityEngine;

public class FadeController : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 1f;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    public async Awaitable FadeOutAsync()
    {
        await FadeAsync(0f, 1f);
    }

    public async Awaitable FadeInAsync()
    {
        await FadeAsync(1f, 0f);
    }

    public IEnumerator FadeOut()
    {
        yield return Fade(0f, 1f);
    }

    public IEnumerator FadeIn()
    {
        yield return Fade(1f, 0f);
    }

    private IEnumerator Fade(float start, float end)
    {
        float elapsed = 0f;

        canvasGroup.alpha = start;
        canvasGroup.blocksRaycasts = true;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, end, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = end;
        canvasGroup.blocksRaycasts = end > 0f;
    }

    private async Awaitable FadeAsync(float start, float end)
    {
        float elapsed = 0f;

        canvasGroup.alpha = start;
        canvasGroup.blocksRaycasts = true;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, end, elapsed / fadeDuration);
            await Awaitable.NextFrameAsync();
        }

        canvasGroup.alpha = end;
        canvasGroup.blocksRaycasts = end > 0f;
    }
}