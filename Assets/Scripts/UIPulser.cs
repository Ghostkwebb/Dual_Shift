using UnityEngine;
using TMPro;

public class UIPulser : MonoBehaviour
{
    [Header("Pulse Settings")]
    [Tooltip("Speed of the pulse effect")]
    [SerializeField] private float speed = 1f;
    [Tooltip("Minimum alpha value")]
    [SerializeField] private float minAlpha = 0.2f;
    [Tooltip("Maximum alpha value")]
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
