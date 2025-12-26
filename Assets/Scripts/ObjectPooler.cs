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
            actionOnGet: (obj) => { if (obj != null) obj.SetActive(true); },
            actionOnRelease: (obj) => { if (obj != null) obj.SetActive(false); },
            actionOnDestroy: (obj) => Destroy(obj),
            defaultCapacity: 10,
            maxSize: 50
        );
    }

    public GameObject GetProjectile(Vector3 position, Quaternion rotation)
    {
        GameObject proj = null;
        
        // Loop until we get a valid (non-destroyed) object or create a new one
        while (proj == null) 
        {
            if (projectilePool.CountInactive == 0 && projectilePool.CountActive >= 50) 
            {
                // Safety: If pool is maxed out/corrupted, just force create
                proj = Instantiate(projectilePrefab);
                break;
            }
            
            proj = projectilePool.Get();
            
            // If the pool gave us a destroyed object (null check on Unity Object), release it and try again?
            // Actually, we can't release a destroyed object back to the pool easily?
            // Just let the GC handle it and ask for another.
        }

        if (proj != null)
        {
            proj.transform.position = position;
            proj.transform.rotation = rotation;
        }
        return proj;
    }

    public void ReturnProjectile(GameObject proj)
    {
        projectilePool.Release(proj);
    }
}