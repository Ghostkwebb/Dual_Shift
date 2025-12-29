using UnityEngine;
using UnityEngine.Pool;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance { get; private set; }

    [Tooltip("Prefab used to spawn pooled projectiles")]
    [SerializeField] private GameObject projectilePrefab;
    
    private ObjectPool<GameObject> projectilePool;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        projectilePool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(projectilePrefab),
            actionOnGet: obj => obj?.SetActive(true),
            actionOnRelease: obj => obj?.SetActive(false),
            actionOnDestroy: obj => Destroy(obj),
            defaultCapacity: 10,
            maxSize: 50
        );
    }

    public GameObject GetProjectile(Vector3 position, Quaternion rotation)
    {
        GameObject proj = null;
        
        while (proj == null)
        {
            if (projectilePool.CountInactive == 0 && projectilePool.CountActive >= 50)
            {
                proj = Instantiate(projectilePrefab);
                break;
            }
            proj = projectilePool.Get();
        }

        if (proj != null)
        {
            proj.transform.position = position;
            proj.transform.rotation = rotation;
        }
        return proj;
    }

    public void ReturnProjectile(GameObject proj) => projectilePool.Release(proj);
}