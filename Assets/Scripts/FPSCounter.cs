using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private float refreshRate = 0.5f; // Update text every 0.5s

    private float timer;

    private void Update()
    {
        // 1. Calculate FPS (1 / time between frames)
        // We use unscaledDeltaTime so it works even when the game is paused
        float fps = 1f / Time.unscaledDeltaTime;

        // 2. Refresh the Text periodically (not every frame, or it's unreadable)
        if (Time.unscaledTime > timer)
        {
            // Display as integer (e.g., "60 FPS")
            fpsText.text = Mathf.RoundToInt(fps) + " FPS";
            timer = Time.unscaledTime + refreshRate;
        }
    }
}