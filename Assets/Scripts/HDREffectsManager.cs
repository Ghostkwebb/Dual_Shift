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

    private void FindPostProcessVolume()
    {
        postProcessVolume = FindFirstObjectByType<Volume>();
        
        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
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
