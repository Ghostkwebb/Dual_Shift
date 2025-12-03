using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
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

    private const string MIXER_MUSIC = "MusicVol";
    private const string MIXER_SFX = "SFXVol";

    private void Start()
    {
        QualitySettings.vSyncCount = 0;

        // 1. Setup Graphics Dropdown
        graphicsDropdown.ClearOptions();
        List<string> options = new List<string>(QualitySettings.names);
        graphicsDropdown.AddOptions(options);

        // 2. Load Saved Preferences
        LoadSettings();

        // 3. Add Listeners
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        graphicsDropdown.onValueChanged.AddListener(SetQuality);
        fpsDropdown.onValueChanged.AddListener(SetFPS);
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    public void SetMusicVolume(float value)
    {
        // Convert 0-1 slider to -80dB to 0dB logarithmic scale
        float volume = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20;
        mainMixer.SetFloat(MIXER_MUSIC, volume);
        PlayerPrefs.SetFloat("MusicVol", value);
    }

    public void SetSFXVolume(float value)
    {
        float volume = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20;
        mainMixer.SetFloat(MIXER_SFX, volume);
        PlayerPrefs.SetFloat("SFXVol", value);
    }

    public void SetQuality(int index)
    {
        QualitySettings.SetQualityLevel(index);
        PlayerPrefs.SetInt("Quality", index);
    }

    public void SetFPS(int index)
    {
        int fps = 60;
        switch (index)
        {
            case 0: fps = 30; break;
            case 1: fps = 60; break;
            case 2: fps = 90; break;
            case 3: fps = 120; break;
        }
        Application.targetFrameRate = fps;
        PlayerPrefs.SetInt("FPS", index);
    }

    private void LoadSettings()
    {
        // Audio
        float music = PlayerPrefs.GetFloat("MusicVol", 0.75f);
        float sfx = PlayerPrefs.GetFloat("SFXVol", 0.75f);
        musicSlider.value = music;
        sfxSlider.value = sfx;
        SetMusicVolume(music);
        SetSFXVolume(sfx);

        // Quality
        int quality = PlayerPrefs.GetInt("Quality", 2); // Default to Medium/High
        graphicsDropdown.value = quality;
        SetQuality(quality);

        // FPS
        int fpsIndex = PlayerPrefs.GetInt("FPS", 1); // Default to 60
        fpsDropdown.value = fpsIndex;
        SetFPS(fpsIndex);
    }
}