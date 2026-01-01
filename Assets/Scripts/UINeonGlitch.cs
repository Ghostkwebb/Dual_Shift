using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UINeonGlitch : MonoBehaviour
{
    [Header("Neon Settings")]
    [Tooltip("Minimum delay between flickers")]
    public float minFlickerDelay = 0.5f;
    [Tooltip("Maximum delay between flickers")]
    public float maxFlickerDelay = 4.0f;
    [Tooltip("Brightness when dimmed")]
    [Range(0f, 1f)] public float dimBrightness = 0.6f;

    [Header("Glitch Settings")]
    [Tooltip("Minimum delay between glitches")]
    public float minGlitchDelay = 2.0f;
    [Tooltip("Maximum delay between glitches")]
    public float maxGlitchDelay = 6.0f;
    [Tooltip("Strength of the glitch shake")]
    public float glitchShakeStrength = 15.0f; 
    [Tooltip("Tint color applied during glitch")]
    public Color glitchTint = new Color(1f, 0f, 1f, 1f); 

    private Image targetImage;
    private RectTransform rectTrans;
    private Vector2 originalPos;
    private Color originalColor;
    

    
    private static readonly WaitForSeconds waitShort = new WaitForSeconds(0.05f);
    private static readonly WaitForSeconds waitMedium = new WaitForSeconds(0.1f);
    
    private static readonly WaitForSeconds[] waitPool = new WaitForSeconds[]
    {
        new WaitForSeconds(0.5f),
        new WaitForSeconds(1.0f),
        new WaitForSeconds(1.5f),
        new WaitForSeconds(2.0f),
        new WaitForSeconds(2.5f),
        new WaitForSeconds(3.0f),
        new WaitForSeconds(3.5f),
        new WaitForSeconds(4.0f),
        new WaitForSeconds(5.0f),
        new WaitForSeconds(6.0f)
    };

    void Start()
    {
        targetImage = GetComponent<Image>();
        rectTrans = GetComponent<RectTransform>();

        if (rectTrans != null) originalPos = rectTrans.anchoredPosition;
        if (targetImage != null) originalColor = targetImage.color;

        if (targetImage) StartCoroutine(NeonFlickerLoop());
        if (rectTrans) StartCoroutine(GlitchLoop());
    }
    
    private WaitForSeconds GetRandomWait(float min, float max)
    {
        int index = Random.Range(0, waitPool.Length);
        return waitPool[index];
    }

    IEnumerator NeonFlickerLoop()
    {
        while (true)
        {
            yield return GetRandomWait(minFlickerDelay, maxFlickerDelay);

            targetImage.color = originalColor * dimBrightness;
            yield return waitShort;
            
            targetImage.color = originalColor;

            // Sometimes double flicker for realism
            if (Random.value > 0.5f)
            {
                yield return waitShort;
                targetImage.color = originalColor * dimBrightness;
                yield return waitShort;
                targetImage.color = originalColor;
            }
        }
    }

    IEnumerator GlitchLoop()
    {
        while (true)
        {
            yield return GetRandomWait(minGlitchDelay, maxGlitchDelay);

            float xNoise = Random.Range(-glitchShakeStrength, glitchShakeStrength);
            float yNoise = Random.Range(-glitchShakeStrength / 2f, glitchShakeStrength / 2f);
            rectTrans.anchoredPosition = originalPos + new Vector2(xNoise, yNoise);

            if (targetImage) targetImage.color = glitchTint;

            yield return waitMedium;

            rectTrans.anchoredPosition = originalPos;
            if (targetImage) targetImage.color = originalColor;
        }
    }
}