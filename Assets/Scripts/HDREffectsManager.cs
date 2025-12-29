using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HDREffectsManager : MonoBehaviour
{
    [SerializeField] private float hdrBloomMultiplier = 1.8f;
    [SerializeField] private float hdrLightMultiplier = 1.5f;
    
    private Volume postProcessVolume;
    private Bloom bloom;
    private float baseBloomIntensity = 1f;
    
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

    public void SetHDRMode(bool enabled)
    {
        if (postProcessVolume == null) FindPostProcessVolume();
        
        if (bloom != null)
        {
            bloom.intensity.value = enabled ? baseBloomIntensity * hdrBloomMultiplier : baseBloomIntensity;
            bloom.threshold.value = enabled ? 0.7f : 0.9f;
            bloom.scatter.value = enabled ? 0.8f : 0.7f;
        }
        Boost2DLights(enabled);
    }

    private void Boost2DLights(bool hdrEnabled)
    {
        var lights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        foreach (var light in lights)
        {
            if (light.lightType != Light2D.LightType.Point) continue;
            
            if (!light.name.Contains("_base"))
                light.name += $"_base{light.intensity:F2}";
            
            float baseIntensity = light.intensity;
            int idx = light.name.IndexOf("_base");
            if (idx >= 0)
            {
                string baseStr = light.name.Substring(idx + 5);
                float.TryParse(baseStr, out baseIntensity);
            }
            
            light.intensity = hdrEnabled ? baseIntensity * hdrLightMultiplier : baseIntensity;
        }
    }
}
