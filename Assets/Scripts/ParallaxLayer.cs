using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Tooltip("0 = No movement. 1 = Full World Speed (Foreground). 0.1 = Far Background.")]
    [Range(0f, 1f)] [SerializeField] private float parallaxFactor = 0.5f;
    
    private Material mat;
    private float offset;

    private void Awake()
    {
        mat = GetComponent<Renderer>().material;
    }

    private void Update()
    {
        float speed = GameManager.Instance.worldSpeed * parallaxFactor;
        offset += (speed * Time.deltaTime) / transform.localScale.x; 
        mat.mainTextureOffset = new Vector2(offset, 0);
    }
}