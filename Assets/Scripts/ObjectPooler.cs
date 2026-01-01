using UnityEngine;
using UnityEngine.Pool;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance { get; private set; }

    [Header("Projectile Pool")]
    [Tooltip("Prefab used to spawn pooled projectiles")]
    [SerializeField] private GameObject projectilePrefab;
    
    [Header("VFX Pool")]
    [Tooltip("Prefab used for death VFX (must have PooledVFX component)")]
    [SerializeField] private PooledVFX deathVFXPrefab;
    
    private ObjectPool<GameObject> projectilePool;
    private ObjectPool<PooledVFX> vfxPool;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        projectilePool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(projectilePrefab, transform),
            actionOnGet: obj => obj?.SetActive(true),
            actionOnRelease: obj => obj?.SetActive(false),
            actionOnDestroy: obj => Destroy(obj),
            defaultCapacity: 50,
            maxSize: 300 
        );
        
        if (deathVFXPrefab != null)
        {
            vfxPool = new ObjectPool<PooledVFX>(
                createFunc: () => Instantiate(deathVFXPrefab, transform),
                actionOnGet: vfx => vfx?.gameObject.SetActive(true),
                actionOnRelease: vfx => vfx?.gameObject.SetActive(false),
                actionOnDestroy: vfx => { if (vfx != null) Destroy(vfx.gameObject); },
                defaultCapacity: 20,
                maxSize: 100
            );
        }
    }

    
    public GameObject GetProjectile(Vector3 position, Quaternion rotation)
    {
        GameObject proj = projectilePool.Get();

        if (proj != null)
        {
            proj.transform.position = position;
            proj.transform.rotation = rotation;
        }
        return proj;
    }



    public void ReturnProjectile(GameObject proj) => projectilePool.Release(proj);


    public PooledVFX GetVFX(Vector3 position)
    {
        if (vfxPool == null)
        {
            Debug.LogWarning("VFX Pool is missing! Did you assign the DeathVFX Prefab in ObjectPooler inspector?");
            return null;
        }
        
        var vfx = vfxPool.Get();
        if (vfx != null)
        {
            vfx.Play(position);
        }
        return vfx;
    }

    public void ReturnVFX(PooledVFX vfx)
    {
        if (vfxPool != null && vfx != null)
            vfxPool.Release(vfx);
    }

}