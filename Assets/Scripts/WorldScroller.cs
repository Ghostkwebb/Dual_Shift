using UnityEngine;

public class WorldScroller : MonoBehaviour
{
    private float worldSpeed;

    void Start()
    {
        worldSpeed = GameManager.Instance.worldSpeed;
    }

    void Update()
    {
        transform.position += Vector3.left * worldSpeed * Time.deltaTime;
    }
}