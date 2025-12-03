using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music Clips")]
    [SerializeField] private AudioClip backgroundMusic;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip jumpClip;     // Lane Switch
    [SerializeField] private AudioClip attackClip;   // Swing
    [SerializeField] private AudioClip hitClip;      // Impact
    [SerializeField] private AudioClip deathClip;    // Player Die
    [SerializeField] private AudioClip uiClickClip;  // Buttons

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        PlayMusic();
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
        // Random pitch variation makes repetitive sounds less annoying
        sfxSource.pitch = Random.Range(0.9f, 1.1f);

        if (clip != null) sfxSource.PlayOneShot(clip);

        // Reset pitch for next sound
        sfxSource.pitch = 1.0f;
    }
}