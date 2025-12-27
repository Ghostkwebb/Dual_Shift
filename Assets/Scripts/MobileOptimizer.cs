using UnityEngine;

/// <summary>
/// Handles mobile-specific optimizations including:
/// - Requesting high refresh rate (120Hz) on Android
/// - Persistent FPS settings
/// Works with LoadingScreenManager for smart loading screens.
/// </summary>
public class MobileOptimizer : MonoBehaviour
{
    // Target FPS with +1 offset trick for stable frame pacing
    private static readonly int[] FPS_TARGETS = { 31, 61, 91, 121 };
    
    private static MobileOptimizer instance;
    
    private void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Apply performance settings
        ApplyPerformanceSettings();
        
        // Request high refresh rate on Android
        RequestHighRefreshRate();
    }

    private void ApplyPerformanceSettings()
    {
        // CRITICAL: Disable VSync completely - required for targetFrameRate to work
        QualitySettings.vSyncCount = 0;
        
        // Get saved FPS setting (default to 60fps = index 1)
        int savedFPSIndex = PlayerPrefs.GetInt("FPS", 1);
        savedFPSIndex = Mathf.Clamp(savedFPSIndex, 0, FPS_TARGETS.Length - 1);
        
        // Set target frame rate with +1 offset
        Application.targetFrameRate = FPS_TARGETS[savedFPSIndex];
        
        // Force screen to never sleep
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    /// <summary>
    /// Request high refresh rate on Android devices.
    /// Android defaults to 60Hz for games - we need to explicitly request higher.
    /// </summary>
    private void RequestHighRefreshRate()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // Get the UnityPlayer activity
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow");
                
                // Get WindowManager.LayoutParams
                AndroidJavaObject layoutParams = window.Call<AndroidJavaObject>("getAttributes");
                
                // Get the display and supported modes
                AndroidJavaObject windowManager = activity.Call<AndroidJavaObject>("getWindowManager");
                AndroidJavaObject display = windowManager.Call<AndroidJavaObject>("getDefaultDisplay");
                AndroidJavaObject[] modes = display.Call<AndroidJavaObject[]>("getSupportedModes");
                
                // Find the highest refresh rate mode
                float highestRefreshRate = 60f;
                int bestModeId = -1;
                
                foreach (var mode in modes)
                {
                    float refreshRate = mode.Call<float>("getRefreshRate");
                    if (refreshRate > highestRefreshRate)
                    {
                        highestRefreshRate = refreshRate;
                        bestModeId = mode.Call<int>("getModeId");
                    }
                }
                
                // Set the preferred display mode to highest refresh rate
                if (bestModeId > 0)
                {
                    // Set via the field
                    layoutParams.Set("preferredDisplayModeId", bestModeId);
                    
                    // Apply the changes on UI thread
                    activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                    {
                        window.Call("setAttributes", layoutParams);
                    }));
                    
                    Debug.Log($"MobileOptimizer: Requested {highestRefreshRate}Hz refresh rate");
                    
                    // Update target frame rate to match
                    int targetFPS = Mathf.RoundToInt(highestRefreshRate) + 1;
                    Application.targetFrameRate = targetFPS;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"MobileOptimizer: Could not set high refresh rate: {e.Message}");
        }
#endif
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            ApplyPerformanceSettings();
            RequestHighRefreshRate();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            ApplyPerformanceSettings();
            RequestHighRefreshRate();
        }
    }

    /// <summary>
    /// Call this when user changes FPS in settings
    /// </summary>
    public static void ApplyFPSSetting(int fpsIndex)
    {
        QualitySettings.vSyncCount = 0;
        fpsIndex = Mathf.Clamp(fpsIndex, 0, FPS_TARGETS.Length - 1);
        Application.targetFrameRate = FPS_TARGETS[fpsIndex];
        PlayerPrefs.SetInt("FPS", fpsIndex);
    }
}
