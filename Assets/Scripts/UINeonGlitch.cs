using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UINeonGlitch : MonoBehaviour
{
    [Header("Neon Settings")]
    public float minFlickerDelay = 0.5f;
    public float maxFlickerDelay = 4.0f;
    [Range(0f, 1f)] public float dimBrightness = 0.6f;

    [Header("Glitch Settings")]
    public float minGlitchDelay = 2.0f;
    public float maxGlitchDelay = 6.0f;
    public float glitchShakeStrength = 15.0f; 
    public Color glitchTint = new Color(1f, 0f, 1f, 1f); 

    private Image targetImage;
    private RectTransform rectTrans;
    private Vector2 originalPos;
    private Color originalColor;

    void Start()
    {
        targetImage = GetComponent<Image>();
        rectTrans = GetComponent<RectTransform>();

        // Save defaults so we can reset to them
        if (rectTrans != null) originalPos = rectTrans.anchoredPosition;
        if (targetImage != null) originalColor = targetImage.color;

        if (targetImage) StartCoroutine(NeonFlickerLoop());
        if (rectTrans) StartCoroutine(GlitchLoop());
    }

    IEnumerator NeonFlickerLoop()
    {
        while (true)
        {
            // Wait for a random time before next flicker
            yield return new WaitForSeconds(Random.Range(minFlickerDelay, maxFlickerDelay));

            // Quick dim (simulating failing voltage)
            targetImage.color = originalColor * dimBrightness;
            yield return new WaitForSeconds(Random.Range(0.05f, 0.1f));
            
            // Restore
            targetImage.color = originalColor;

            // Sometimes double flicker for realism
            if (Random.value > 0.5f)
            {
                yield return new WaitForSeconds(0.05f);
                targetImage.color = originalColor * dimBrightness;
                yield return new WaitForSeconds(0.05f);
                targetImage.color = originalColor;
            }
        }
    }

    IEnumerator GlitchLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minGlitchDelay, maxGlitchDelay));

            // 1. Shake Position
            float xNoise = Random.Range(-glitchShakeStrength, glitchShakeStrength);
            float yNoise = Random.Range(-glitchShakeStrength / 2f, glitchShakeStrength / 2f);
            rectTrans.anchoredPosition = originalPos + new Vector2(xNoise, yNoise);

            // 2. Flash Color (simulates signal error)
            if (targetImage) targetImage.color = glitchTint;

            // Hold the glitch very briefly
            yield return new WaitForSeconds(Random.Range(0.05f, 0.1f));

            // Reset everything
            rectTrans.anchoredPosition = originalPos;
            if (targetImage) targetImage.color = originalColor;
        }
    }
}