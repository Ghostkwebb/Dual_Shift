using UnityEngine;
using TMPro;

public class UIPulser : MonoBehaviour
{
    [Header("Pulse Settings")]
    [SerializeField] private float speed = 1f;
    [SerializeField] private float minAlpha = 0.2f;
    [SerializeField] private float maxAlpha = 1f;

    private CanvasGroup canvasGroup;
    private TMP_Text tmpText;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        tmpText = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        // Calculate pulsed alpha using a sine wave for smoother ease-in/out feeling compared to linear PingPong
        // Sin goes from -1 to 1. We map it to 0 to 1.
        float t = (Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2) + 1f) / 2f;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
        else if (tmpText != null)
        {
            tmpText.alpha = alpha;
        }
    }
}
