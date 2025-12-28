using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.Rendering;
using TMPro;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Video")]
    [SerializeField] private TMP_Dropdown graphicsDropdown;
    [SerializeField] private TMP_Dropdown fpsDropdown;
    [SerializeField] private GameObject settingsPanel;
    
    [Header("HDR")]
    [SerializeField] private Toggle hdrToggle;
    [SerializeField] private TMP_Text hdrStatusText;

    [Header("FPS Display")]
    [SerializeField] private Toggle fpsDisplayToggle;
    [SerializeField] private GameObject fpsDisplayObject;

    [Header("Panels")]
    [SerializeField] private GameObject controlsOverlay;

    private const string MIXER_MUSIC = "MusicVol";
    private const string MIXER_SFX = "SFXVol";
    private bool isHDRSupported = false;

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
        fpsDropdown.onValueChanged.AddListener(SetFPS);
        if (hdrToggle != null) hdrToggle.onValueChanged.AddListener(SetHDR);
        if (fpsDisplayToggle != null) fpsDisplayToggle.onValueChanged.AddListener(SetFPSDisplay);
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
    public void OpenSettings() => settingsPanel.SetActive(true);
    public void CloseSettings() => settingsPanel.SetActive(false);

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

        // FPS Display - disabled by default
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
        settingsPanel.SetActive(false);
        controlsOverlay.SetActive(true);
    }

    public void CloseControls()
    {
        controlsOverlay.SetActive(false);
        settingsPanel.SetActive(true);
    }
    
    public bool IsHDRSupported() => isHDRSupported;
}