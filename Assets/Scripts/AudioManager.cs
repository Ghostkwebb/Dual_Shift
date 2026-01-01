using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [Tooltip("Main audio mixer for volume control")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("Audio Sources (Assign in Inspector)")]
    [Tooltip("SFX audio source - route to SFX mixer group")]
    [SerializeField] private AudioSource sfxSource;
    [Tooltip("Running sound source - route to SFX mixer group")]
    [SerializeField] private AudioSource runSource;

    [Header("Music Settings")]
    [Tooltip("Background music clip")]
    [SerializeField] private AudioClip backgroundMusic;

    [Header("SFX Clips")]
    [Tooltip("Clip played when running")]
    [SerializeField] private AudioClip runClip;
    [Tooltip("Clip played when jumping")]
    [SerializeField] private AudioClip jumpClip;
    [Tooltip("Clip played when attacking")]
    [SerializeField] private AudioClip attackClip;
    [Tooltip("Clip played when hit")]
    [SerializeField] private AudioClip hitClip;
    [Tooltip("Clip played on death")]
    [SerializeField] private AudioClip deathClip;
    [Tooltip("Clip played on UI click")]
    [SerializeField] private AudioClip uiClickClip;

    private AudioSource musicSource;
    private AudioLowPassFilter musicFilter;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        
        CreateIsolatedMusicPlayer();
    }

    private void CreateIsolatedMusicPlayer()
    {
        // Create a child object for music with its own AudioSource + LowPassFilter
        GameObject musicObj = new GameObject("MusicPlayer");
        musicObj.transform.SetParent(transform);
        
        musicSource = musicObj.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        
        if (mainMixer != null)
        {
            var groups = mainMixer.FindMatchingGroups("Music");
            if (groups.Length > 0) musicSource.outputAudioMixerGroup = groups[0];
        }
        
        musicFilter = musicObj.AddComponent<AudioLowPassFilter>();
        musicFilter.cutoffFrequency = 22000f;
    }

    private void Start() => PlayMusic();

    private void Update()
    {
        if (musicFilter != null && musicSource != null && musicSource.isPlaying)
        {
            float targetFreq = (GameManager.Instance.CurrentState == GameManager.GameState.Playing) ? 22000f : 800f;
            musicFilter.cutoffFrequency = Mathf.Lerp(musicFilter.cutoffFrequency, targetFreq, Time.unscaledDeltaTime * 3f);
        }
        
        if (runSource != null && runClip != null)
        {
            bool shouldRun = GameManager.Instance.CurrentState == GameManager.GameState.Playing && Time.timeScale > 0;
            
            if (shouldRun && !runSource.isPlaying)
            {
                runSource.clip = runClip;
                runSource.Play();
            }
            else if (!shouldRun && runSource.isPlaying)
            {
                runSource.Stop();
            }
        }
    }

    public void PlayMusic()
    {
        if (backgroundMusic != null && musicSource != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    public void PlayJump() => PlaySFX(jumpClip);
    public void PlayAttack() => PlaySFX(attackClip);
    public void PlayHit() => PlaySFX(hitClip);
    public void PlayDeath() => PlaySFX(deathClip);
    public void PlayClick() => PlaySFX(uiClickClip);

    private void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip);
    }
}