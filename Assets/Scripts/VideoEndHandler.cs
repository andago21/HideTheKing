using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class VideoEndHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private GameObject canvasToDisable;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Text Fade + Pulse")]
    [SerializeField] private CanvasGroup textCanvasGroup;
    [SerializeField] private float textStartDelay = 0.5f;
    [SerializeField] private float textFadeInDuration = 2f;
    [SerializeField] private float textHoldDuration = 1f; 
    [SerializeField] private float textMinAlpha = 0.25f;
    [SerializeField] private float textMaxAlpha = 1f;
    [SerializeField] private float textPulseSpeed = 1.2f;
    [SerializeField] private float textFadeOutDuration = 1.2f;

    [Header("Canvas End Fade")]
    [SerializeField] private float fadeDuration = 1.5f;

    private Coroutine textPulseCoroutine;
    private Coroutine textFadeCoroutine;
    private Coroutine endSequenceCoroutine;

    private void Awake()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (canvasGroup == null && canvasToDisable != null)
            canvasGroup = canvasToDisable.GetComponent<CanvasGroup>();
        if (textCanvasGroup != null)
            textCanvasGroup.alpha = 0f;
    }

    private void OnEnable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.started += OnVideoStarted;
            videoPlayer.loopPointReached += OnVideoEnd;
        }
    }

    private void OnDisable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.started -= OnVideoStarted;
            videoPlayer.loopPointReached -= OnVideoEnd;
        }
    }

    private void Start()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            OnVideoStarted(videoPlayer);
        }
    }

    private void OnVideoStarted(VideoPlayer vp)
    {
        if (textCanvasGroup == null)
            return;

        textCanvasGroup.alpha = 0f;

        if (textFadeCoroutine != null)
            StopCoroutine(textFadeCoroutine);

        if (textPulseCoroutine != null)
            StopCoroutine(textPulseCoroutine);

        if (endSequenceCoroutine != null)
            StopCoroutine(endSequenceCoroutine);

        textFadeCoroutine = StartCoroutine(FadeInThenPulseText());
    }

    private IEnumerator FadeInThenPulseText()
    {
        yield return new WaitForSeconds(textStartDelay);

        float t = 0f;

        // Fade IN
        while (t < textFadeInDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.SmoothStep(0f, 1f, t / textFadeInDuration);
            textCanvasGroup.alpha = Mathf.Lerp(0f, textMaxAlpha, normalized);
            yield return null;
        }

        textCanvasGroup.alpha = textMaxAlpha;

        // ✅ HOLD fully visible
        yield return new WaitForSeconds(textHoldDuration);

        // Start pulsing
        textPulseCoroutine = StartCoroutine(PulseText());
    }

    private IEnumerator PulseText()
    {
        while (true)
        {
            float wave = Mathf.PerlinNoise(Time.time * textPulseSpeed, 0f);
            textCanvasGroup.alpha = Mathf.Lerp(textMinAlpha, textMaxAlpha, wave);
            yield return null;
        }
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        if (textFadeCoroutine != null)
            StopCoroutine(textFadeCoroutine);

        if (textPulseCoroutine != null)
            StopCoroutine(textPulseCoroutine);

        if (endSequenceCoroutine != null)
            StopCoroutine(endSequenceCoroutine);

        if (canvasToDisable == null)
            return;

        if (canvasGroup == null)
        {
            canvasToDisable.SetActive(false);
            return;
        }

        endSequenceCoroutine = StartCoroutine(TextFadeOutThenCanvasFade());
    }

    private IEnumerator TextFadeOutThenCanvasFade()
    {
        // Fade OUT text first
        if (textCanvasGroup != null)
        {
            textCanvasGroup.interactable = false;
            textCanvasGroup.blocksRaycasts = false;

            float startAlpha = textCanvasGroup.alpha;
            float t = 0f;

            while (t < textFadeOutDuration)
            {
                t += Time.deltaTime;
                float normalized = Mathf.SmoothStep(0f, 1f, t / textFadeOutDuration);
                textCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, normalized);
                yield return null;
            }

            textCanvasGroup.alpha = 0f;
        }

        // Then fade canvas
        yield return StartCoroutine(FadeOutAndDisable());
    }

    private IEnumerator FadeOutAndDisable()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float startCanvasAlpha = canvasGroup.alpha;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.SmoothStep(0f, 1f, t / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(startCanvasAlpha, 0f, normalized);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasToDisable.SetActive(false);
    }
}