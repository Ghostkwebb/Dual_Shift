using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class HDREffectsManager : MonoBehaviour
{
    [Tooltip("Multiplier for HDR bloom intensity")]
    [SerializeField] private float hdrBloomMultiplier = 1.4f;
    [Tooltip("Whether to disable lights when HDR is off")]
    [SerializeField] private bool disableLightsWhenNoHDR = true;
    
    private Volume postProcessVolume;
    private Bloom bloom;
    private ColorAdjustments colorAdjustments;
    private float baseBloomIntensity = 1f;
    
    
    private List<Light2D> cachedPointLights = new List<Light2D>();
    private bool lightsNeedRefresh = true;
    
    private static HDREffectsManager instance;
    public static HDREffectsManager Instance => instance;
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        FindPostProcessVolume();
        
        FindPostProcessVolume();
        
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        lightsNeedRefresh = true;
        FindPostProcessVolume();
    }

    private void Start()
    {
         StartCoroutine(RetryFindVolume());
    }

    private System.Collections.IEnumerator RetryFindVolume()
    {
        int retries = 0;
        while (postProcessVolume == null && retries < 10)
        {
            yield return new WaitForSeconds(0.5f);
            FindPostProcessVolume();
            retries++;
            if (postProcessVolume != null)
            {
                 Debug.Log("HDREffectsManager: Found Volume after retry!");
                 // Force re-apply brightness if we have a stored value, though usually user input drives this.
                 // We could store 'lastSetBrightness' if needed, but the main issue is just finding the volume initially.
            }
        }
    }

    private void FindPostProcessVolume()
    {
        // Find all volumes (including inactive) and prefer the global one
        var volumes = FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var vol in volumes)
        {
            if (vol.isGlobal)
            {
                postProcessVolume = vol;
                break;
            }
        }
        
        // Fallback to first if no global found
        if (postProcessVolume == null && volumes.Length > 0)
            postProcessVolume = volumes[0];

        if (postProcessVolume != null)
        {
            Debug.Log($"HDREffectsManager: Using Volume '{postProcessVolume.gameObject.name}'");
            
            if (postProcessVolume.profile == null) return;

            if (postProcessVolume.profile.TryGet(out bloom))
            {
                baseBloomIntensity = bloom.intensity.value;
            }
            else
            {
                bloom = postProcessVolume.profile.Add<Bloom>(true);
                bloom.intensity.value = 1f;
                bloom.threshold.value = 0.9f;
                bloom.scatter.value = 0.7f;
                baseBloomIntensity = 1f;
            }

            if (!postProcessVolume.profile.TryGet(out colorAdjustments))
            {
                 colorAdjustments = postProcessVolume.profile.Add<ColorAdjustments>(true);
            }
            
            // Ensure we grab the reference if it existed
            if (colorAdjustments == null) postProcessVolume.profile.TryGet(out colorAdjustments);
        }
        else
        {
            // Debug.LogWarning("HDREffectsManager: No Volume found in scene!");
        }
    }
    
    public void SetBrightness(float value)
    {
        // Try finding if missing
        if (postProcessVolume == null) FindPostProcessVolume();

        // If still missing, we can't do anything yet (the retry coroutine might catch it later)
        if (postProcessVolume != null)
        {
            if (colorAdjustments == null)
            {
                postProcessVolume.profile.TryGet(out colorAdjustments);
                if (colorAdjustments == null) colorAdjustments = postProcessVolume.profile.Add<ColorAdjustments>(true);
            }

            if (colorAdjustments != null)
            {
                colorAdjustments.active = true;
                colorAdjustments.postExposure.overrideState = true;
                colorAdjustments.postExposure.value = value;
                Debug.Log($"HDREffects SetBrightness: {value}");
            }
        }
    }

    private void DiagnoseVolumeSettings()
    {
        var cam = Camera.main;
        if (cam == null) 
        {
             Debug.LogError("HDREffects: Main Camera not found!");
             return;
        }

        var data = cam.GetComponent<UniversalAdditionalCameraData>();
        if (data != null)
        {
             if (!data.renderPostProcessing) Debug.LogError("HDREffects: 'Render Post Processing' is UNCHECKED on Main Camera!");
             if (postProcessVolume != null)
             {
                 int volumeLayer = postProcessVolume.gameObject.layer;
                 if ((data.volumeLayerMask & (1 << volumeLayer)) == 0)
                 {
                     Debug.LogError($"HDREffects: Camera VolumeMask ({data.volumeLayerMask.value}) does NOT include Volume Layer '{LayerMask.LayerToName(volumeLayer)}'!");
                 }
             }
        }
        else
        {
            // If built-in pipeline or data missing, checking 'cam.usePhysicalProperties' logic etc, but assuming URP here.
             Debug.LogWarning("HDREffects: standard URP CameraData not found on Main Camera.");
        }
        
        if (postProcessVolume != null)
        {
            if (!postProcessVolume.enabled) Debug.LogError("HDREffects: Volume component is DISABLED!");
            if (postProcessVolume.weight <= 0) Debug.LogError("HDREffects: Volume Weight is 0!");
        }
    }
    

    
    public void RegisterLight(Light2D light)
    {
        if (light != null && light.lightType == Light2D.LightType.Point && !cachedPointLights.Contains(light))
        {
            cachedPointLights.Add(light);
        }
    }
    

    
    private void CleanupDestroyedLights()
    {
        cachedPointLights.RemoveAll(l => l == null);
    }

    public void SetHDRMode(bool enabled)
    {
        if (postProcessVolume == null) FindPostProcessVolume();


        
        if (QualitySettings.GetQualityLevel() == 0)
        {
            if (postProcessVolume != null) postProcessVolume.enabled = false;
            return;
        }
        
        if (postProcessVolume != null) postProcessVolume.enabled = true;
        

        
        if (bloom != null)
        {
            bloom.active = true;
            bloom.intensity.value = enabled ? baseBloomIntensity * hdrBloomMultiplier : baseBloomIntensity;
            bloom.threshold.value = enabled ? 0.8f : 0.9f;
            bloom.scatter.value = enabled ? 0.75f : 0.7f;
        }
        

        
        if (disableLightsWhenNoHDR)
        {
            CleanupDestroyedLights();
            foreach (var light in cachedPointLights)
            {
                if (light != null)
                {
                    light.enabled = enabled;
                }
            }
        }
    }
}
