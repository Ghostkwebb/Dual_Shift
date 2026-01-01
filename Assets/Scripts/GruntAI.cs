using UnityEngine;

public class GruntAI : MonoBehaviour
{
    [Tooltip("Animator for the grunt")]
    [SerializeField] private Animator animator;

    public bool IsDead { get; private set; } = false;
    
    // Cached component to avoid GetComponent calls
    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        IsDead = false;
        if (col != null) col.enabled = true;
    }

    public void TriggerDeath()
    {
        if (IsDead) return;
        IsDead = true;
        
        // Disable collider FIRST to prevent any more collision events
        if (col != null) col.enabled = false;
        
        animator.SetTrigger("Death");
        Destroy(gameObject, 0.5f);
    }
}