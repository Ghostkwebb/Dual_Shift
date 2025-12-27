using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Smart loading screen that waits until FPS actually stabilizes.
/// Also provides loading screen for quality settings changes.
/// </summary>
public class LoadingScreenManager : MonoBehaviour
{
    [Header("Loading Screen UI")]
    [Tooltip("The loading screen panel - will be auto-created if null")]
    [SerializeField] private GameObject loadingScreenPanel;
    
    [Tooltip("Optional: Text to show loading status")]
    [SerializeField] private TMP_Text loadingText;
    
    [Tooltip("Optional: Loading spinner or progress indicator")]
    [SerializeField] private GameObject spinner;

    [Header("FPS Stability Settings")]
    [Tooltip("Minimum FPS to consider 'stable'")]
    [SerializeField] private float minStableFPS = 50f;
    
    [Tooltip("How many consecutive stable frames needed")]
    [SerializeField] private int stableFrameCount = 30;
    
    [Tooltip("Maximum wait time before forcing hide (seconds)")]
    [SerializeField] private float maxWaitTime = 5f;
    
    [Tooltip("Minimum wait time (seconds)")]
    [SerializeField] private float minWaitTime = 0.5f;

    private int consecutiveStableFrames = 0;
    private float[] fpsHistory = new float[10];
    private int fpsHistoryIndex = 0;
    private bool isLoading = false;
    
    public static LoadingScreenManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Auto-create loading screen if not assigned
        if (loadingScreenPanel == null)
        {
            CreateDefaultLoadingScreen();
        }
        
        // Show loading screen on startup
        ShowLoadingScreen("Loading...");
        StartCoroutine(WaitForStableFrameRate());
    }

    private void CreateDefaultLoadingScreen()
    {
        // Create a simple loading screen programmatically
        GameObject canvas = new GameObject("LoadingCanvas");
        canvas.transform.SetParent(transform);
        Canvas c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 9999; // Always on top
        canvas.AddComponent<CanvasScaler>();
        canvas.AddComponent<GraphicRaycaster>();
        
        // Dark background
        loadingScreenPanel = new GameObject("LoadingPanel");
        loadingScreenPanel.transform.SetParent(canvas.transform, false);
        Image bg = loadingScreenPanel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 1f); // Dark blue-ish
        RectTransform rt = loadingScreenPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        // Loading text
        GameObject textObj = new GameObject("LoadingText");
        textObj.transform.SetParent(loadingScreenPanel.transform, false);
        loadingText = textObj.AddComponent<TextMeshProUGUI>();
        loadingText.text = "Loading...";
        loadingText.fontSize = 42;
        loadingText.alignment = TextAlignmentOptions.Center;
        loadingText.color = Color.white;
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.5f, 0.5f);
        textRt.anchorMax = new Vector2(0.5f, 0.5f);
        textRt.sizeDelta = new Vector2(600, 100);
    }

    /// <summary>
    /// Show loading screen with custom message
    /// </summary>
    public void ShowLoadingScreen(string message = "Loading...")
    {
        isLoading = true;
        consecutiveStableFrames = 0;
        
        if (loadingScreenPanel != null)
            loadingScreenPanel.SetActive(true);
        
        if (loadingText != null)
            loadingText.text = message;
        
        if (spinner != null)
            spinner.SetActive(true);
    }

    /// <summary>
    /// Hide loading screen
    /// </summary>
    public void HideLoadingScreen()
    {
        isLoading = false;
        
        if (loadingScreenPanel != null)
            loadingScreenPanel.SetActive(false);
        
        if (spinner != null)
            spinner.SetActive(false);
    }

    /// <summary>
    /// Wait until FPS is stable before hiding loading screen
    /// </summary>
    private IEnumerator WaitForStableFrameRate()
    {
        float startTime = Time.realtimeSinceStartup;
        
        // Minimum wait
        yield return new WaitForSecondsRealtime(minWaitTime);
        
        // Update text
        if (loadingText != null)
            loadingText.text = "Optimizing...";
        
        // Wait for stable FPS or timeout
        while (Time.realtimeSinceStartup - startTime < maxWaitTime)
        {
            float currentFPS = 1f / Time.unscaledDeltaTime;
            
            // Update FPS history
            fpsHistory[fpsHistoryIndex] = currentFPS;
            fpsHistoryIndex = (fpsHistoryIndex + 1) % fpsHistory.Length;
            
            // Check if FPS is above minimum
            if (currentFPS >= minStableFPS)
            {
                consecutiveStableFrames++;
                
                if (consecutiveStableFrames >= stableFrameCount)
                {
                    // FPS is stable!
                    break;
                }
            }
            else
            {
                consecutiveStableFrames = 0;
            }
            
            yield return null;
        }
        
        // Hide loading screen
        HideLoadingScreen();
    }

    /// <summary>
    /// Show loading screen while applying quality settings
    /// </summary>
    public void ShowLoadingForQualityChange(int qualityIndex, System.Action onComplete = null)
    {
        StartCoroutine(ApplyQualityWithLoadingScreen(qualityIndex, onComplete));
    }

    private IEnumerator ApplyQualityWithLoadingScreen(int qualityIndex, System.Action onComplete)
    {
        ShowLoadingScreen("Applying Graphics...");
        
        // Wait a frame for loading screen to render
        yield return null;
        yield return null;
        
        // Apply quality settings
        QualitySettings.SetQualityLevel(qualityIndex, true);
        QualitySettings.vSyncCount = 0;
        
        // Re-apply FPS
        MobileOptimizer.ApplyFPSSetting(PlayerPrefs.GetInt("FPS", 1));
        
        // Save setting
        PlayerPrefs.SetInt("Quality", qualityIndex);
        
        // Wait for FPS to stabilize
        consecutiveStableFrames = 0;
        float stabilityStartTime = Time.realtimeSinceStartup;
        
        while (Time.realtimeSinceStartup - stabilityStartTime < 3f) // Max 3 seconds for quality change
        {
            float currentFPS = 1f / Time.unscaledDeltaTime;
            
            if (currentFPS >= minStableFPS * 0.8f) // Slightly lower threshold for quality change
            {
                consecutiveStableFrames++;
                if (consecutiveStableFrames >= 20)
                    break;
            }
            else
            {
                consecutiveStableFrames = 0;
            }
            
            yield return null;
        }
        
        HideLoadingScreen();
        onComplete?.Invoke();
    }

    /// <summary>
    /// Get average FPS from history
    /// </summary>
    public float GetAverageFPS()
    {
        float sum = 0;
        foreach (float fps in fpsHistory)
            sum += fps;
        return sum / fpsHistory.Length;
    }
}
