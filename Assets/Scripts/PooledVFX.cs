using UnityEngine;

/// <summary>
/// Component for pooled VFX particles. Automatically returns to pool when complete.
/// Attach this to your DeathVFX prefab.
/// </summary>
public class PooledVFX : MonoBehaviour
{
    [Tooltip("The particle system to pool")]
    [SerializeField] private ParticleSystem particles;
    
    private float duration;
    private float timer;
    private bool isPlaying;

    private void Awake()
    {
        if (particles == null)
            particles = GetComponent<ParticleSystem>();
    }

    private void Update()
    {
        if (!isPlaying) return;
        
        // Check if particles (and children) are done playing
        if (particles != null && !particles.IsAlive(true))
        {
            isPlaying = false;
            ObjectPooler.Instance.ReturnVFX(this);
        }
    }

    /// <summary>
    /// Call this to play the VFX at a specific position
    /// </summary>
    public void Play(Vector3 position)
    {
        transform.position = position;
        isPlaying = true;
        
        if (particles != null)
        {
            particles.Clear();
            particles.Play();
        }
    }
}
