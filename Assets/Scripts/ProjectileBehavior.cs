using UnityEngine;

public class ProjectileBehavior : MonoBehaviour
{
    private float speed;

    public void Initialize(float worldSpeed)
    {
        // Move slightly faster than the world (1.5x)
        this.speed = worldSpeed * 1.5f;
        Destroy(gameObject, 5f); // Auto-destroy after 5 seconds
    }

    void Update()
    {
        transform.position += Vector3.left * speed * Time.deltaTime;
    }
}