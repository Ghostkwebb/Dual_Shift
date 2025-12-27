using UnityEngine;

public class MobileOptimizer : MonoBehaviour
{
    private static readonly int[] FPS_TARGETS = { 31, 61, 91, 121 };
    private static MobileOptimizer instance;
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        ApplyPerformanceSettings();
        RequestHighRefreshRate();
    }

    private void ApplyPerformanceSettings()
    {
        QualitySettings.vSyncCount = 0;
        int savedFPSIndex = PlayerPrefs.GetInt("FPS", 1);
        savedFPSIndex = Mathf.Clamp(savedFPSIndex, 0, FPS_TARGETS.Length - 1);
        Application.targetFrameRate = FPS_TARGETS[savedFPSIndex];
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    private void RequestHighRefreshRate()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow");
                AndroidJavaObject layoutParams = window.Call<AndroidJavaObject>("getAttributes");
                AndroidJavaObject windowManager = activity.Call<AndroidJavaObject>("getWindowManager");
                AndroidJavaObject display = windowManager.Call<AndroidJavaObject>("getDefaultDisplay");
                AndroidJavaObject[] modes = display.Call<AndroidJavaObject[]>("getSupportedModes");
                
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
                
                if (bestModeId > 0)
                {
                    layoutParams.Set("preferredDisplayModeId", bestModeId);
                    activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                    {
                        window.Call("setAttributes", layoutParams);
                    }));
                    Application.targetFrameRate = Mathf.RoundToInt(highestRefreshRate) + 1;
                }
            }
        }
        catch (System.Exception) { }
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

    public static void ApplyFPSSetting(int fpsIndex)
    {
        QualitySettings.vSyncCount = 0;
        fpsIndex = Mathf.Clamp(fpsIndex, 0, FPS_TARGETS.Length - 1);
        Application.targetFrameRate = FPS_TARGETS[fpsIndex];
        PlayerPrefs.SetInt("FPS", fpsIndex);
    }
}
