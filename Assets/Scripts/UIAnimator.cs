using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class UIAnimator : MonoBehaviour
{
    public enum AnimationType
    {
        Fade,
        Scale,
        FadeAndScale,
        Slide
    }

    [Header("Animation Settings")]
    [Tooltip("Type of animation to play")]
    [SerializeField] private AnimationType animationType = AnimationType.FadeAndScale;
    [Tooltip("Duration of the animation")]
    [SerializeField] private float duration = 0.3f;
    [Tooltip("Easing curve for the animation")]
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Scale Settings")]
    [Tooltip("Starting scale for scale animations")]
    [SerializeField] private Vector3 startScale = new Vector3(0.8f, 0.8f, 1f);
    
    [Header("Slide Settings")]
    [Tooltip("Offset for slide animations")]
    [SerializeField] private Vector2 slideOffset = new Vector2(0, -100);

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Vector2 originalPosition;
    private Coroutine currentCoroutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        
        if (rectTransform != null)
        {
            originalScale = rectTransform.localScale;
            originalPosition = rectTransform.anchoredPosition;
        }
    }

    public void Show(Action onComplete = null)
    {
        if (!gameObject.activeSelf) 
        {
            gameObject.SetActive(true);
            ResetToHiddenState();
        }

        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(AnimateRoutine(true, onComplete));
    }

    public void Hide(Action onComplete = null)
    {
        if (!gameObject.activeSelf) return;

        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(AnimateRoutine(false, () => 
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
        }));
    }

    public void HideImmediately()
    {
        ResetToHiddenState();
        gameObject.SetActive(false);
    }

    private void ResetToHiddenState()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        
        if (rectTransform != null)
        {
            if (animationType == AnimationType.Scale || animationType == AnimationType.FadeAndScale)
                rectTransform.localScale = startScale;
            
            if (animationType == AnimationType.Slide)
                rectTransform.anchoredPosition = originalPosition + slideOffset;
        }
    }

    private IEnumerator AnimateRoutine(bool showing, Action onComplete)
    {
        float timer = 0f;
        
        float startAlpha = canvasGroup.alpha;
        float targetAlpha = showing ? 1f : 0f;

        Vector3 currentScale = rectTransform.localScale;
        Vector3 targetScale = showing ? originalScale : startScale;

        Vector2 currentPos = rectTransform.anchoredPosition;
        Vector2 targetPos = showing ? originalPosition : originalPosition + slideOffset;

        while (timer < duration)
        {
            if (this == null || gameObject == null) yield break; // Safety check

            timer += Time.unscaledDeltaTime; 
            float t = Mathf.Clamp01(timer / duration);
            float curveValue = easeCurve.Evaluate(t);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, curveValue);
            }

            if (rectTransform != null && (animationType == AnimationType.Scale || animationType == AnimationType.FadeAndScale))
            {
                rectTransform.localScale = Vector3.Lerp(currentScale, targetScale, curveValue);
            }

            if (rectTransform != null && animationType == AnimationType.Slide)
            {
               rectTransform.anchoredPosition = Vector2.Lerp(currentPos, targetPos, curveValue);
            }

            yield return null;
        }

        if (this == null || gameObject == null) yield break;

        if (canvasGroup != null) canvasGroup.alpha = targetAlpha;
        if (rectTransform != null)
        {
             if (animationType == AnimationType.Scale || animationType == AnimationType.FadeAndScale)
                rectTransform.localScale = targetScale;
             if (animationType == AnimationType.Slide)
                rectTransform.anchoredPosition = targetPos;
        }
        
        currentCoroutine = null;
        onComplete?.Invoke();
    }
}
