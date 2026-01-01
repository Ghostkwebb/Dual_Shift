using UnityEngine;

public class WorldScroller : MonoBehaviour
{
    private GameManager gm;
    
    private void Start() => gm = GameManager.Instance;
    
    void Update()
    {
        if (gm == null) return;
        float currentSpeed = gm.worldSpeed;
        transform.position += Vector3.left * currentSpeed * Time.deltaTime;
    }
}