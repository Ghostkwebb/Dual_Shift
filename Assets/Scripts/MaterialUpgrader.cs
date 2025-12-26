using UnityEngine;

// [ExecuteAlways] // Disabled to stop console spam
public class MaterialUpgrader : MonoBehaviour
{
    [Tooltip("Control the brightness of the background. 0 = Black, 1 = Full Bright.")]
    [SerializeField] private Color backgroundTint = Color.white; // FIX: White = Crisp (No Mud) 
    [Tooltip("Drag 'ScrollableLitSprite' shader here if auto-find fails.")]
    [SerializeField] private Shader customLitShader;

    private UnityEngine.Rendering.Universal.Light2D cachedGlobalLight;
    private const string ShaderName = "DualShift/ScrollableLit";

    private void Start()
    {
        // 1. Cache References
        CacheGlobalLight();
        
        // 2. Initial Setup (Runs once on start)
        AssignShadersToBackgrounds();
    }

    private void Update()
    {
        // 3. Continuous Update (Runs every frame in Play Mode)
        UpdateGlobalLighting();
    }

    [ContextMenu("Force Fix Backgrounds")]
    public void ForceFixBackgrounds()
    {
        Debug.Log("MaterialUpgrader: Starting Manual Fix...");
        
        // 1. Ensure Light isn't pitch black in Editor
        Shader.SetGlobalFloat("_CustomGlobalLight", 1.0f);
        
        AssignShadersToBackgrounds();
    }

    private void AssignShadersToBackgrounds()
    {
        // Find Shader
        Shader targetShader = customLitShader;
        if (targetShader == null) targetShader = Shader.Find(ShaderName);

        if (targetShader == null)
        {
            Debug.LogError($"MaterialUpgrader: CRITICAL - Could not find shader {ShaderName}");
            return;
        }

        // Assign to Objects
        var layers = FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None);
        int updatedCount = 0;
        
        // Debug.Log($"MaterialUpgrader: Found {layers.Length} Parallax Layers. Starting Update Loop...");

        foreach (var layer in layers)
        {
            if (layer == null) continue;

            // CRITICAL CHANGE: Use generic Renderer to catch MeshRenderers/TilemapRenderers too
            var r = layer.GetComponent<Renderer>();
            
            if (r == null)
            {
                Debug.LogWarning($"MaterialUpgrader: Object '{layer.name}' has ParallaxLayer but NO RENDERER of any kind!");
            }
            else
            {
                // FORCE UPDATE
                if (r.sharedMaterial != null)
                {
                    // 1. Switch Shader
                    r.sharedMaterial.shader = targetShader;

                    // 2. FORCE RECOVERY OF TEXTURES (Fix for "Foggy" / Missing Texture issue)
#if UNITY_EDITOR
                    string fileName = "";
                    string layerName = layer.name.Replace("(Clone)", "").Trim();

                    // Map Layer Name -> File Name
                    if (layerName == "Layer1") fileName = "1-Background.png";
                    else if (layerName == "Layer2") fileName = "2-super far.png";
                    else if (layerName == "Layer3") fileName = "3-far.png";
                    else if (layerName == "Layer4") fileName = "4-far light.png";
                    else if (layerName == "Layer5") fileName = "5-close.png";
                    else if (layerName == "Layer6") fileName = "6-close light.png";
                    else if (layerName == "Layer7") fileName = "7-tileset.png";

                    if (!string.IsNullOrEmpty(fileName))
                    {
                        string path = "Assets/External Assets/Graphics/Background/" + fileName;
                        Texture2D recoveredTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        if (recoveredTex != null)
                        {
                            r.sharedMaterial.SetTexture("_MainTex", recoveredTex);
                            r.sharedMaterial.SetColor("_Color", Color.white); // Access tint cleanly
                            // Ensure Tiling is 1,1 (x,y) and Offset 0,0 (z,w)
                            r.sharedMaterial.SetVector("_MainTex_ST", new Vector4(1, 1, 0, 0));
                            Debug.Log($"MaterialUpgrader: SUCCESS - Loaded & Assigned '{fileName}' to {layer.name}.");
                        }
                        else
                        {
                            // If auto-load fails, try to fall back to existing texture if it exists
                            if (r.sharedMaterial.mainTexture == null)
                                Debug.LogError($"MaterialUpgrader: FAILED - Could not load texture at {path} AND no existing texture found.");
                        }
                    }
#endif
                    updatedCount++;
                }
                else
                {
                    Debug.LogWarning($"MaterialUpgrader: Object '{layer.name}' has {r.GetType().Name} but NO MATERIAL assigned!");
                }
            }
        }
        
        Debug.Log($"MaterialUpgrader: Init - Force-updated {updatedCount} renderers to use {targetShader.name}.");
    }



    private void UpdateGlobalLighting()
    {
        // 1. Get Light Intensity
        if (cachedGlobalLight == null) CacheGlobalLight();

        float intensity = 1.0f; // Default full bright if no light found
        if (cachedGlobalLight != null) intensity = cachedGlobalLight.intensity;

        // 2. Get Tint Brightness
        float tintFactor = backgroundTint.grayscale; 

        // 3. Calculate Final Global Value (Intensity Only)
        // Kept for backward compatibility or if shader wants just float
        float finalIntensity = intensity * tintFactor;
        finalIntensity *= 1.0f; // 1:1 Ratio

        // 4. Calculate Final Color (Light Color * Intensity * Tint)
        // This is what we really want for "Mood"
        Color finalColor = Color.white;
        
        if (cachedGlobalLight != null && cachedGlobalLight.enabled && cachedGlobalLight.gameObject.activeInHierarchy)
        {
             finalColor = cachedGlobalLight.color;
        }
        else
        {
            // If no light found or disabled, default to full white so game is visible
            intensity = 1.0f; 
        }
        
        // Apply Intensity to the Color
        finalColor *= intensity;
        
        // Apply Background Tint (e.g. if user wants it greyscale)
        finalColor *= backgroundTint;

        
        // 5. Send to Shader
        Shader.SetGlobalFloat("_CustomGlobalLight", finalIntensity); // Legacy/Validation
        Shader.SetGlobalColor("_CustomGlobalLightColor", finalColor); 
    }

    private void CacheGlobalLight()
    {
        if (cachedGlobalLight == null)
        {
            var lights = FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(FindObjectsSortMode.None);
            foreach(var l in lights)
            {
                if (l.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global)
                {
                    cachedGlobalLight = l;
                    break;
                }
            }
        }
    }
}
