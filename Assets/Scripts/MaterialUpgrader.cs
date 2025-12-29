using UnityEngine;

public class MaterialUpgrader : MonoBehaviour
{
    [Tooltip("Tint color applied to background layers")]
    [SerializeField] private Color backgroundTint = new Color(0.8f, 0.85f, 0.95f);
    [Tooltip("Custom shader for backgrounds (auto-finds if empty)")]
    [SerializeField] private Shader customLitShader;

    private UnityEngine.Rendering.Universal.Light2D cachedGlobalLight;
    private const string ShaderName = "DualShift/ScrollableLit";

    private void Start()
    {
        CacheGlobalLight();
        AssignShadersToBackgrounds();
    }

    private void Update() => UpdateGlobalLighting();

    [ContextMenu("Force Fix Backgrounds")]
    public void ForceFixBackgrounds()
    {
        Shader.SetGlobalFloat("_CustomGlobalLight", 1.0f);
        AssignShadersToBackgrounds();
    }

    private void AssignShadersToBackgrounds()
    {
        Shader targetShader = customLitShader ?? Shader.Find(ShaderName);
        if (targetShader == null) return;

        var layers = FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None);
        
        foreach (var layer in layers)
        {
            if (layer == null) continue;
            var r = layer.GetComponent<Renderer>();
            
            if (r?.sharedMaterial != null)
            {
                r.sharedMaterial.shader = targetShader;

#if UNITY_EDITOR
                string fileName = GetTextureFileName(layer.name);
                if (!string.IsNullOrEmpty(fileName))
                {
                    string path = "Assets/External Assets/Graphics/Background/" + fileName;
                    Texture2D tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (tex != null)
                    {
                        r.sharedMaterial.SetTexture("_MainTex", tex);
                        r.sharedMaterial.SetColor("_Color", Color.white);
                        r.sharedMaterial.SetVector("_MainTex_ST", new Vector4(1, 1, 0, 0));
                    }
                }
#endif
            }
        }
    }

    private string GetTextureFileName(string layerName)
    {
        layerName = layerName.Replace("(Clone)", "").Trim();
        return layerName switch
        {
            "Layer1" => "1-Background.png",
            "Layer2" => "2-super far.png",
            "Layer3" => "3-far.png",
            "Layer4" => "4-far light.png",
            "Layer5" => "5-close.png",
            "Layer6" => "6-close light.png",
            "Layer7" => "7-tileset.png",
            _ => ""
        };
    }

    private void UpdateGlobalLighting()
    {
        if (cachedGlobalLight == null) CacheGlobalLight();

        float intensity = cachedGlobalLight?.intensity ?? 1.0f;
        float tintFactor = backgroundTint.grayscale;
        float finalIntensity = intensity * tintFactor;

        Color finalColor = Color.white;
        if (cachedGlobalLight != null && cachedGlobalLight.enabled)
        {
            finalColor = cachedGlobalLight.color * intensity * backgroundTint;
        }

        Shader.SetGlobalFloat("_CustomGlobalLight", finalIntensity);
        Shader.SetGlobalColor("_CustomGlobalLightColor", finalColor);
    }

    private void CacheGlobalLight()
    {
        var lights = FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global)
            {
                cachedGlobalLight = l;
                break;
            }
        }
    }
}
