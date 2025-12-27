using UnityEngine;

public class GruntAI : MonoBehaviour
{
    [SerializeField] private Animator animator;

    public bool IsDead { get; private set; } = false;

    public void TriggerDeath()
    {
        if (IsDead) return;
        IsDead = true;
        
        // Disable collider FIRST to prevent any more collision events
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        
        animator.SetTrigger("Death");
        Destroy(gameObject, 0.5f);
    }
}