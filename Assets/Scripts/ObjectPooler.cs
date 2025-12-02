using UnityEngine;
using UnityEngine.Pool;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance { get; private set; }

    [SerializeField] private GameObject projectilePrefab;

    private ObjectPool<GameObject> projectilePool;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        // Initialize the pool
        projectilePool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(projectilePrefab),
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj),
            defaultCapacity: 10,
            maxSize: 50
        );
    }

    public GameObject GetProjectile(Vector3 position, Quaternion rotation)
    {
        GameObject proj = projectilePool.Get();
        proj.transform.position = position;
        proj.transform.rotation = rotation;
        return proj;
    }

    public void ReturnProjectile(GameObject proj)
    {
        projectilePool.Release(proj);
    }
}