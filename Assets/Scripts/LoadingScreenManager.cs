using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingScreenManager : MonoBehaviour
{
    [Header("Loading Screen UI")]
    [SerializeField] private GameObject loadingScreenPanel;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private GameObject spinner;

    [Header("FPS Stability Settings")]
    [SerializeField] private float minStableFPS = 50f;
    [SerializeField] private int stableFrameCount = 30;
    [SerializeField] private float maxWaitTime = 5f;
    [SerializeField] private float minWaitTime = 0.5f;

    private int consecutiveStableFrames = 0;
    private float[] fpsHistory = new float[10];
    private int fpsHistoryIndex = 0;
    
    public static LoadingScreenManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (loadingScreenPanel == null)
            CreateDefaultLoadingScreen();
        
        ShowLoadingScreen("Loading...");
        StartCoroutine(WaitForStableFrameRate());
    }

    private void CreateDefaultLoadingScreen()
    {
        GameObject canvas = new GameObject("LoadingCanvas");
        canvas.transform.SetParent(transform);
        Canvas c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 9999;
        canvas.AddComponent<CanvasScaler>();
        canvas.AddComponent<GraphicRaycaster>();
        
        loadingScreenPanel = new GameObject("LoadingPanel");
        loadingScreenPanel.transform.SetParent(canvas.transform, false);
        Image bg = loadingScreenPanel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 1f);
        RectTransform rt = loadingScreenPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
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

    public void ShowLoadingScreen(string message = "Loading...")
    {
        consecutiveStableFrames = 0;
        if (loadingScreenPanel != null) loadingScreenPanel.SetActive(true);
        if (loadingText != null) loadingText.text = message;
        if (spinner != null) spinner.SetActive(true);
    }

    public void HideLoadingScreen()
    {
        if (loadingScreenPanel != null) loadingScreenPanel.SetActive(false);
        if (spinner != null) spinner.SetActive(false);
    }

    private IEnumerator WaitForStableFrameRate()
    {
        float startTime = Time.realtimeSinceStartup;
        yield return new WaitForSecondsRealtime(minWaitTime);
        
        if (loadingText != null) loadingText.text = "Optimizing...";
        
        while (Time.realtimeSinceStartup - startTime < maxWaitTime)
        {
            float currentFPS = 1f / Time.unscaledDeltaTime;
            fpsHistory[fpsHistoryIndex] = currentFPS;
            fpsHistoryIndex = (fpsHistoryIndex + 1) % fpsHistory.Length;
            
            if (currentFPS >= minStableFPS)
            {
                consecutiveStableFrames++;
                if (consecutiveStableFrames >= stableFrameCount) break;
            }
            else
            {
                consecutiveStableFrames = 0;
            }
            yield return null;
        }
        HideLoadingScreen();
    }

    public void ShowLoadingForQualityChange(int qualityIndex, System.Action onComplete = null)
    {
        StartCoroutine(ApplyQualityWithLoadingScreen(qualityIndex, onComplete));
    }

    private IEnumerator ApplyQualityWithLoadingScreen(int qualityIndex, System.Action onComplete)
    {
        ShowLoadingScreen("Applying Graphics...");
        yield return null;
        yield return null;
        
        QualitySettings.SetQualityLevel(qualityIndex, true);
        QualitySettings.vSyncCount = 0;
        MobileOptimizer.ApplyFPSSetting(PlayerPrefs.GetInt("FPS", 1));
        PlayerPrefs.SetInt("Quality", qualityIndex);
        
        if (loadingText != null) loadingText.text = "Optimizing Shaders...";
        for (int i = 0; i < 10; i++) yield return null;
        yield return new WaitForSecondsRealtime(0.5f);
        
        if (loadingText != null) loadingText.text = "Almost Ready...";
        consecutiveStableFrames = 0;
        float stabilityStartTime = Time.realtimeSinceStartup;
        
        while (Time.realtimeSinceStartup - stabilityStartTime < 4f)
        {
            float currentFPS = 1f / Time.unscaledDeltaTime;
            if (currentFPS >= minStableFPS * 0.7f)
            {
                consecutiveStableFrames++;
                if (consecutiveStableFrames >= 60) break;
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

    public float GetAverageFPS()
    {
        float sum = 0;
        foreach (float fps in fpsHistory) sum += fps;
        return sum / fpsHistory.Length;
    }
}
