using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Tooltip("0 = No movement. 1 = Full World Speed (Foreground). 0.1 = Far Background.")]
    [Range(0f, 1f)] [SerializeField] private float parallaxFactor = 0.5f;
    
    private float length;
    private Vector3 startPos;
    private GameObject clone;
    
    // Expose for MaterialUpgrader
    public SpriteRenderer CloneRenderer { get; private set; }

    private void Start()
    {
        // 1. Calculate width of the sprite
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            length = rend.bounds.size.x;
        }
        startPos = transform.position;

        // 2. Create a clone for infinite scrolling
        clone = Instantiate(gameObject, transform.parent);
        CloneRenderer = clone.GetComponent<SpriteRenderer>();
        
        // Remove the script from the clone so it doesn't run this logic too
        Destroy(clone.GetComponent<ParallaxLayer>());
        
        // Position clone exactly to the right
        clone.transform.position = transform.position + new Vector3(length, 0, 0);
        clone.transform.localScale = transform.localScale;
        clone.transform.rotation = transform.rotation;
    }

    private void Update()
    {
        // 1. Calculate movement
        // We move relative to the camera OR just based on WorldSpeed?
        // Original code was: offset += speed * Time.deltaTime.
        // Let's move the object left based on WorldSpeed * Factor
        
        float dist = (GameManager.Instance.worldSpeed * parallaxFactor) * Time.deltaTime;
        transform.Translate(Vector3.left * dist);
        clone.transform.Translate(Vector3.left * dist);

        // 2. Wrap around
        // If main object is too far left
        if (transform.position.x < startPos.x - length)
        {
            transform.position += new Vector3(length * 2, 0, 0);
        }
        
        // If clone is too far left
        if (clone.transform.position.x < startPos.x - length)
        {
            clone.transform.position += new Vector3(length * 2, 0, 0);
        }
    }
}