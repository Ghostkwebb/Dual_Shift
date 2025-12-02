using UnityEngine;
using System.Collections;

public class ProjectileBehavior : MonoBehaviour
{
    private float speed;

    public void Initialize(float worldSpeed)
    {
        this.speed = worldSpeed * 1.5f;
        StartCoroutine(DeactivateRoutine());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    void Update()
    {
        transform.position += Vector3.left * speed * Time.deltaTime;
    }

    private IEnumerator DeactivateRoutine()
    {
        yield return new WaitForSeconds(5f);
        ObjectPooler.Instance.ReturnProjectile(this.gameObject);
    }
}