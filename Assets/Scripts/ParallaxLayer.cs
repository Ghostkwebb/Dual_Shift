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

    private GameManager gm;
    
    // Cache to avoid GC allocations in Update
    private Vector3 wrapOffset;
    private Transform cloneTransform;

    private void Start()
    {
        gm = GameManager.Instance;
        
        // 1. Calculate width of the sprite
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            length = rend.bounds.size.x;
        }
        startPos = transform.position;
        
        // Cache wrap offset vector
        wrapOffset = new Vector3(length * 2, 0, 0);

        clone = Instantiate(gameObject, transform.parent);
        cloneTransform = clone.transform;
        CloneRenderer = clone.GetComponent<SpriteRenderer>();
        
        // Remove the script from the clone so it doesn't run this logic too
        Destroy(clone.GetComponent<ParallaxLayer>());
        
        // Position clone exactly to the right
        cloneTransform.position = transform.position + new Vector3(length, 0, 0);
        cloneTransform.localScale = transform.localScale;
        cloneTransform.rotation = transform.rotation;
    }

    private void Update()
    {
        if (gm == null) return;
        
        // 1. Calculate movement (Vector3.left is cached by Unity)
        float dist = (gm.worldSpeed * parallaxFactor) * Time.deltaTime;
        transform.Translate(Vector3.left * dist);
        cloneTransform.Translate(Vector3.left * dist);

        // 2. Wrap around using cached offset
        if (transform.position.x < startPos.x - length)
        {
            transform.position += wrapOffset;
        }
        
        if (cloneTransform.position.x < startPos.x - length)
        {
            cloneTransform.position += wrapOffset;
        }
    }
}