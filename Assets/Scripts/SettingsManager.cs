using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.Rendering;
using TMPro;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("Main mixer")]
    [SerializeField] private AudioMixer mainMixer;
    [Tooltip("Slider for music volume")]
    [SerializeField] private Slider musicSlider;
    [Tooltip("Slider for SFX volume")]
    [SerializeField] private Slider sfxSlider;

    [Header("Video")]
    [Tooltip("Dropdown for graphics quality")]
    [SerializeField] private TMP_Dropdown graphicsDropdown;
    [Tooltip("Dropdown for FPS target")]
    [SerializeField] private TMP_Dropdown fpsDropdown;
    [Tooltip("Panel for settings UI")]
    [SerializeField] private GameObject settingsPanel;
    
    [Header("HDR")]
    [Tooltip("Toggle for HDR")]
    [SerializeField] private Toggle hdrToggle;
    [Tooltip("Text status for HDR")]
    [SerializeField] private TMP_Text hdrStatusText;

    [Header("FPS Display")]
    [Tooltip("Toggle for FPS display")]
    [SerializeField] private Toggle fpsDisplayToggle;
    [Tooltip("FPS display object")]
    [SerializeField] private GameObject fpsDisplayObject;

    [Header("Panels")]
    [Tooltip("Overlay for controls")]
    [SerializeField] private GameObject controlsOverlay;

    private const string MIXER_MUSIC = "MusicVol";
    private const string MIXER_SFX = "SFXVol";
    private bool isHDRSupported = false;
    
    [Header("Google Play Games UI")]
    [Tooltip("Button for GPGS login")]
    [SerializeField] private Button gpgsButton;
    [Tooltip("Text on GPGS button")]
    [SerializeField] private TMP_Text gpgsButtonText;
    [Tooltip("Icon for GPGS status")]
    [SerializeField] private Image gpgsIcon;
    [Tooltip("Sprite for connected state")]
    [SerializeField] private Sprite connectedSprite;   
    [Tooltip("Sprite for disconnected state")]
    [SerializeField] private Sprite disconnectedSprite; 

    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        graphicsDropdown.ClearOptions();
        graphicsDropdown.AddOptions(new List<string>(QualitySettings.names));
        
        CheckHDRSupport();
        LoadSettings();
        
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        graphicsDropdown.onValueChanged.AddListener(SetQuality);
        if (hdrToggle != null) hdrToggle.onValueChanged.AddListener(SetHDR);
        if (fpsDisplayToggle != null) fpsDisplayToggle.onValueChanged.AddListener(SetFPSDisplay);
        
        if (gpgsButton != null) gpgsButton.onClick.AddListener(OnGPGSButtonClick);

        UpdateGPGSButton();
        StartCoroutine(UpdateGPGSButtonRoutine());
    }

    private System.Collections.IEnumerator UpdateGPGSButtonRoutine()
    {
        var wait = new WaitForSeconds(1.0f);
        while (true)
        {
            yield return wait;
            UpdateGPGSButton();
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            UpdateGPGSButton();
        }
        if (hasFocus) PlayerPrefs.Save();
    }

    private string cachedUserName = null;
    private string cachedConnectString = null;

    public void UpdateGPGSButton()
    {
        if (gpgsIcon == null || gpgsButtonText == null || gpgsButton == null) return;
        
        gpgsIcon.color = Color.white;

#if UNITY_ANDROID && !UNITY_EDITOR
        bool isAuthenticated = false;
        try
        {
            var platform = GooglePlayGames.PlayGamesPlatform.Instance;
            if (platform != null)
            {
                isAuthenticated = platform.IsAuthenticated();
            }
        }
        catch (System.Exception)
        {
            isAuthenticated = false;
        }

        if (isAuthenticated)
        {
            if (string.IsNullOrEmpty(cachedUserName))
            {
                string userName = "Player";
                try { userName = Social.localUser.userName; } catch { }
                if (string.IsNullOrEmpty(userName)) userName = "Player";
                cachedUserName = userName;
                cachedConnectString = "CONNECTED: " + cachedUserName;
            }
            
            if (gpgsButtonText.text != cachedConnectString)
                gpgsButtonText.text = cachedConnectString;

            if (connectedSprite != null && gpgsIcon.sprite != connectedSprite)
                gpgsIcon.sprite = connectedSprite;
                
            if (gpgsButton.interactable)
                gpgsButton.interactable = false; 
        }
        else
        {
            cachedUserName = null;
            cachedConnectString = null;

            if (gpgsButtonText.text != "CONNECT ACCOUNT")
                gpgsButtonText.text = "CONNECT ACCOUNT";

            if (disconnectedSprite != null && gpgsIcon.sprite != disconnectedSprite)
                gpgsIcon.sprite = disconnectedSprite;
                
            if (!gpgsButton.interactable)
                gpgsButton.interactable = true; 
        }
#else
        if (gpgsButtonText.text != "GPGS (Editor)")
            gpgsButtonText.text = "GPGS (Editor)";
        if (gpgsButton.interactable)
            gpgsButton.interactable = false;
#endif
    }



    public void OnGPGSButtonClick()
    {
        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.SignIn();
        }
    }

    private void CheckHDRSupport()
    {
        HDRDisplaySupportFlags hdrFlags = SystemInfo.hdrDisplaySupportFlags;
        bool flagsSupport = (hdrFlags & HDRDisplaySupportFlags.Supported) != 0;
        
        bool outputAvailable = false;
        try
        {
            var hdrOutput = HDROutputSettings.main;
            outputAvailable = hdrOutput != null && hdrOutput.available;
        }
        catch { }
        
        bool gpuSupportsHDR = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.DefaultHDR);
        isHDRSupported = flagsSupport || outputAvailable || gpuSupportsHDR;
        
        if (hdrToggle != null)
        {
            hdrToggle.interactable = isHDRSupported;
            if (!isHDRSupported) hdrToggle.isOn = false;
        }
        UpdateHDRStatusText();
    }

    private void UpdateHDRStatusText()
    {
        if (hdrStatusText == null) return;
        
        if (!isHDRSupported)
        {
            hdrStatusText.text = "Not Supported";
            hdrStatusText.color = Color.gray;
        }
        else if (hdrToggle != null && hdrToggle.isOn)
        {
            hdrStatusText.text = "Enabled";
            hdrStatusText.color = Color.green;
        }
        else
        {
            hdrStatusText.text = "Disabled";
            hdrStatusText.color = Color.white;
        }
    }

    public void SetHDR(bool enabled)
    {
        if (!isHDRSupported)
        {
            if (hdrToggle != null) hdrToggle.isOn = false;
            return;
        }
        
        try
        {
            var hdrOutput = HDROutputSettings.main;
            if (hdrOutput != null && hdrOutput.available)
                hdrOutput.RequestHDRModeChange(enabled);
        }
        catch { }
        
        if (HDREffectsManager.Instance != null)
            HDREffectsManager.Instance.SetHDRMode(enabled);
        
        PlayerPrefs.SetInt("HDR", enabled ? 1 : 0);
        UpdateHDRStatusText();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) PlayerPrefs.Save();
    }

    private void OnApplicationQuit() => PlayerPrefs.Save();
    public void OpenSettings() => SetPanelActive(settingsPanel, true);
    public void CloseSettings() => SetPanelActive(settingsPanel, false);

    public void SetMusicVolume(float value)
    {
        if (mainMixer == null) return;
        float volume = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
        mainMixer.SetFloat(MIXER_MUSIC, volume);
        PlayerPrefs.SetFloat("MusicVol", value);
    }

    public void SetSFXVolume(float value)
    {
        if (mainMixer == null) return;
        float volume = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
        mainMixer.SetFloat(MIXER_SFX, volume);
        PlayerPrefs.SetFloat("SFXVol", value);
    }

    public void SetQuality(int index)
    {
        if (LoadingScreenManager.Instance != null)
            LoadingScreenManager.Instance.ShowLoadingForQualityChange(index);
        else
        {
            QualitySettings.SetQualityLevel(index, true);
            QualitySettings.vSyncCount = 0;
            MobileOptimizer.ApplyFPSSetting(PlayerPrefs.GetInt("FPS", 1));
            PlayerPrefs.SetInt("Quality", index);
        }
    }

    public void SetFPS(int index) => MobileOptimizer.ApplyFPSSetting(index);
    private void ApplyFPS(int index) => MobileOptimizer.ApplyFPSSetting(index);

    public void SetFPSDisplay(bool enabled)
    {
        if (fpsDisplayObject != null)
            fpsDisplayObject.SetActive(enabled);
        PlayerPrefs.SetInt("FPSDisplay", enabled ? 1 : 0);
    }

    private void LoadSettings()
    {
        float music = PlayerPrefs.GetFloat("MusicVol", 0.75f);
        float sfx = PlayerPrefs.GetFloat("SFXVol", 0.75f);
        musicSlider.SetValueWithoutNotify(music);
        sfxSlider.SetValueWithoutNotify(sfx);
        Invoke(nameof(ApplyAudioSettings), 0.1f);

        int quality = PlayerPrefs.GetInt("Quality", 2);
        quality = Mathf.Clamp(quality, 0, QualitySettings.names.Length - 1);
        graphicsDropdown.SetValueWithoutNotify(quality);
        SetQuality(quality);

        int fpsIndex = PlayerPrefs.GetInt("FPS", 1);
        fpsDropdown.SetValueWithoutNotify(fpsIndex);
        SetFPS(fpsIndex);
        
        if (hdrToggle != null && isHDRSupported)
        {
            bool hdrEnabled = PlayerPrefs.GetInt("HDR", 0) == 1;
            hdrToggle.SetIsOnWithoutNotify(hdrEnabled);
            SetHDR(hdrEnabled);
        }



        if (fpsDisplayToggle != null)
        {
            bool fpsDisplayEnabled = PlayerPrefs.GetInt("FPSDisplay", 0) == 1;
            fpsDisplayToggle.SetIsOnWithoutNotify(fpsDisplayEnabled);
            SetFPSDisplay(fpsDisplayEnabled);
        }
    }

    private void ApplyAudioSettings()
    {
        SetMusicVolume(musicSlider.value);
        SetSFXVolume(sfxSlider.value);
    }

    public void OpenControls()
    {
        SetPanelActive(settingsPanel, false);
        SetPanelActive(controlsOverlay, true);
    }

    public void CloseControls()
    {
        SetPanelActive(controlsOverlay, false);
        SetPanelActive(settingsPanel, true);
    }

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel == null) return;
        
        if (panel.TryGetComponent<UIAnimator>(out var animator))
        {
            if (active) animator.Show();
            else animator.Hide();
        }
        else
        {
            panel.SetActive(active);
        }
    }
    
    public bool IsHDRSupported() => isHDRSupported;
}