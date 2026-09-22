using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Drives the "Gain More Chakra" / "Select Jutsu" world-space prompt that lives
/// next to the Chakra Charger guide hands. No other prompt states for now.
///
/// - While the chakra guide is showing (chakra not full, debounced show = true): "Gain More Chakra"
/// - Once chakra is full and no jutsu is currently being detected: "Select Jutsu"
/// - While a jutsu is being detected (player pressed a jutsu button): hidden
/// </summary>
public class ChakraGuidePromptController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ChakraSystem chakra;
    [SerializeField] private JutsuManager jutsuManager;

    [Tooltip("The Chakra Charger's own HandPoseGuideVisual (Drive Externally = false) — used to stay in sync with its debounced show/hide state.")]
    [SerializeField] private HandPoseGuideVisual chakraGuide;

    [Header("Canvas")]
    [Tooltip("CanvasGroup on the prompt canvas. Its alpha is faded; the GameObject itself is only disabled once fully faded out.")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text promptText;

    [Header("Text")]
    [SerializeField] private string gainChakraText = "Gain More Chakra";
    [SerializeField] private string selectJutsuText = "Select Jutsu";

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.3f;

    private enum PromptState { Hidden, GainChakra, SelectJutsu }
    private PromptState _currentState = PromptState.Hidden;
    private Coroutine _fadeRoutine;

    private void Awake()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        PromptState desired = DetermineState();
        if (desired != _currentState)
        {
            _currentState = desired;
            ApplyState(desired);
        }
    }

    private PromptState DetermineState()
    {
        bool detecting = jutsuManager != null && jutsuManager.IsDetecting;
        if (detecting) return PromptState.Hidden;

        if (chakraGuide != null && chakraGuide.ShouldShow)
            return PromptState.GainChakra;

        if (chakra != null && chakra.IsFull)
            return PromptState.SelectJutsu;

        return PromptState.Hidden;
    }

    private void ApplyState(PromptState state)
    {
        if (state == PromptState.Hidden)
        {
            FadeTo(0f);
            return;
        }

        if (promptText != null)
            promptText.text = (state == PromptState.GainChakra) ? gainChakraText : selectJutsuText;

        if (canvasGroup != null && !canvasGroup.gameObject.activeSelf)
            canvasGroup.gameObject.SetActive(true);

        FadeTo(1f);
    }

    private void FadeTo(float target)
    {
        if (canvasGroup == null) return;
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeRoutine(target));
    }

    private IEnumerator FadeRoutine(float target)
    {
        float start = canvasGroup.alpha;
        float t = 0f;
        float duration = Mathf.Max(fadeDuration, 0.01f);

        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }

        canvasGroup.alpha = target;

        if (target <= 0f)
            canvasGroup.gameObject.SetActive(false);
    }
}