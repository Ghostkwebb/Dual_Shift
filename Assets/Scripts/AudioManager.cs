using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource runSource; 


    [Header("Music Clips")]
    [SerializeField] private AudioClip backgroundMusic;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip runClip;     
    [SerializeField] private AudioClip jumpClip;     
    [SerializeField] private AudioClip attackClip;   
    [SerializeField] private AudioClip hitClip;      
    [SerializeField] private AudioClip deathClip;    
    [SerializeField] private AudioClip uiClickClip;  
    
    [Header("Dynamic Music")]
    [SerializeField] private AudioLowPassFilter musicFilter;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        PlayMusic();
    }

    private void Update()
    {
        if (musicFilter != null && musicSource != null && musicSource.isPlaying)
        {
            float targetFreq = (GameManager.Instance.CurrentState == GameManager.GameState.Playing) ? 22000f : 500f;
            musicFilter.cutoffFrequency = Mathf.Lerp(musicFilter.cutoffFrequency, targetFreq, Time.deltaTime * 2.0f);
        }
        
        if (runSource != null)
        {
            // Ensure runtime check for clip
            if (runClip == null) return;

            bool shouldRun = GameManager.Instance.CurrentState == GameManager.GameState.Playing && Time.timeScale > 0;
            
            if (shouldRun)
            {
                if (!runSource.isPlaying)
                {
                    runSource.clip = runClip;
                    runSource.Play();
                }
            }
            else
            {
                if (runSource.isPlaying) runSource.Stop();
            }
        }
    }

    public void PlayMusic()
    {
        if (backgroundMusic != null)
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
        if (sfxSource == null) return;
        
        sfxSource.pitch = Random.Range(0.9f, 1.1f);
        if (clip != null) sfxSource.PlayOneShot(clip);
        sfxSource.pitch = 1.0f;
    }
}