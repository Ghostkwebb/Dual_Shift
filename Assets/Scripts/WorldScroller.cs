using UnityEngine;

public class WorldScroller : MonoBehaviour
{
    void Update()
    {
        float currentSpeed = GameManager.Instance.worldSpeed;
        transform.position += Vector3.left * currentSpeed * Time.deltaTime;
    }
}