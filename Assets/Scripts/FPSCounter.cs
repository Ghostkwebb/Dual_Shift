using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    [Tooltip("The text component displaying FPS")]
    [SerializeField] private TextMeshProUGUI fpsText;
    [Tooltip("How often to update the FPS display")]
    [SerializeField] private float refreshRate = 0.5f;

    private float timer;
    private int lastFPS = -1; // Cache to avoid updating when unchanged

    private void Update()
    {
        if (Time.unscaledTime > timer)
        {
            int fps = Mathf.RoundToInt(1f / Time.unscaledDeltaTime);
            
            if (fps != lastFPS)
            {
                fpsText.SetText("{0} FPS", fps);
                lastFPS = fps;
            }
            timer = Time.unscaledTime + refreshRate;
        }
    }
}