using UnityEngine;

public class GruntAI : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private bool isDead = false;

    public void TriggerDeath()
    {
        if (isDead) return;
        isDead = true;
        animator.SetTrigger("Death"); 
        GetComponent<Collider2D>().enabled = false;
        Destroy(gameObject, 0.5f);
    }
}