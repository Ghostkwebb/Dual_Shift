using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("Main audio mixer for volume control")]
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Video")]
    [SerializeField] private TMP_Dropdown graphicsDropdown;
    [SerializeField] private TMP_Dropdown fpsDropdown;
    [SerializeField] private GameObject settingsPanel;

    [Header("Panels")]
    [SerializeField] private GameObject controlsOverlay;

    private const string MIXER_MUSIC = "MusicVol";
    private const string MIXER_SFX = "SFXVol";

    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        
        graphicsDropdown.ClearOptions();
        graphicsDropdown.AddOptions(new List<string>(QualitySettings.names));
        
        LoadSettings();
        
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        graphicsDropdown.onValueChanged.AddListener(SetQuality);
        fpsDropdown.onValueChanged.AddListener(SetFPS);
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
        // Don't call PlayerPrefs.Save() here - causes lag on mobile!
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
        // Use loading screen for quality changes (if available)
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.ShowLoadingForQualityChange(index);
        }
        else
        {
            // Fallback if no loading screen manager
            QualitySettings.SetQualityLevel(index, true);
            QualitySettings.vSyncCount = 0;
            
            int savedFPS = PlayerPrefs.GetInt("FPS", 1);
            ApplyFPS(savedFPS);
            
            PlayerPrefs.SetInt("Quality", index);
        }
    }

    public void SetFPS(int index)
    {
        // Use MobileOptimizer which has the +1 FPS offset fix for stable frame pacing
        MobileOptimizer.ApplyFPSSetting(index);
    }

    private void ApplyFPS(int index)
    {
        // Delegate to MobileOptimizer
        MobileOptimizer.ApplyFPSSetting(index);
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
}