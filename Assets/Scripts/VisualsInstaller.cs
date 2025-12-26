using UnityEngine;
using UnityEngine.Rendering.Universal;

// [ExecuteAlways] // Removed to prevent Editor errors
public class VisualsInstaller : MonoBehaviour
{
    [Header("Global Light Settings")]
    [SerializeField] private Color globalLightColor = new Color(0.4f, 0.3f, 0.6f); // Brighter Purple (Was 0.15)
    [SerializeField] private float globalLightIntensity = 1.0f; // Default 1.0 (Was 0.5)

    [Header("Player Light Settings")]
    [SerializeField] private Color playerLightColor = Color.cyan;
    [SerializeField] private float playerLightIntensity = 1.2f;
    [SerializeField] private float playerLightRadius = 6.0f;

    [Header("Enemy Light Settings (Prefab Injection)")]
    [SerializeField] private Color enemyLightColor = new Color(1f, 0f, 0.2f); // Magenta/Red

    private void Start()
    {
        if (Application.isPlaying)
        {
            SetupGlobalLight();
            SetupPlayerLight();
            SetupDustParticles();
        }
    }

    private void OnEnable()
    {
        // Strictly Runtime Only to prevent Editor Errors
        if (Application.isPlaying) 
        {
            SetupGlobalLight(); 
        }
    }

    private void OnValidate()
    {
        // Live update Global Light in Editor when settings change
        SetupGlobalLight();
    }

    private void SetupGlobalLight()
    {
        // 1. Find Existing Light
        Light2D globalLight = null;
        var lights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.lightType == Light2D.LightType.Global)
            {
                globalLight = l;
                break;
            }
        }

        // 2. Create if missing (RUNTIME ONLY)
        if (globalLight == null && Application.isPlaying)
        {
            GameObject globalLightObj = new GameObject("Global Light 2D");
            globalLight = globalLightObj.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
        }

        // 3. Apply Settings (Safely)
        if (globalLight != null)
        {
            globalLight.color = globalLightColor;
            globalLight.intensity = globalLightIntensity;

            // CRITICAL: Ensure it targets EVERYTHING (including Backgrounds)
            // If the background is on a specific layer, "Default" filter would miss it.
            // We use reflection or assumption that "0" usually means "All" or we leave it.
            // Actually, URP Light2D default is usually "All". If user changed it, we reset it?
            // Let's not force-reset sorting layers blindly to avoid breaking other logic, 
            // BUT, user complained about background.
            // We'll rely on MaterialUpgrader's "Manual Tinting" which bypasses sorting layers entirely.
        }
    }

    private void SetupPlayerLight()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            // Check if lights already exist on player to avoid duplicates (if scene doesn't reload fully)
            if (player.GetComponentInChildren<Light2D>() != null) return;

            // 1. Main Player Glow (Small, bright)
            GameObject playerLightObj = new GameObject("Player Light 2D");
            playerLightObj.transform.SetParent(player.transform);
            playerLightObj.transform.localPosition = Vector3.zero;

            Light2D light = playerLightObj.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = playerLightColor;
            light.intensity = playerLightIntensity;
            light.pointLightOuterRadius = playerLightRadius;
            light.pointLightInnerRadius = 0.5f;
            light.falloffIntensity = 0.8f;

            // 2. Background "Backlight" (Large, hits walls)
            // Emulates the Silksong "Hero Glow" on the environment
            GameObject backlightObj = new GameObject("Player Backlight 2D");
            backlightObj.transform.SetParent(player.transform);
            backlightObj.transform.localPosition = new Vector3(0, 0, 0); // Position at player

            Light2D backlight = backlightObj.AddComponent<Light2D>();
            backlight.lightType = Light2D.LightType.Point;
            backlight.color = new Color(0.2f, 0.6f, 1.0f, 1f); // Stronger Cyan/Blue tint for "Cyberpunk" glow
            backlight.intensity = 2.0f; // Significantly Brighter (0.8 -> 2.0)
            backlight.pointLightOuterRadius = 20.0f; // Massive radius to fill screen
            backlight.falloffIntensity = 0.5f;
        }
    }

    // Static helper to be called by Enemy Spawn logic or similar if we can't edit prefabs directly
    public static void AttachEnemyLight(GameObject enemyObj, Color color, float intensity, float radius)
    {
        GameObject lightObj = new GameObject("Enemy Light 2D");
        lightObj.transform.SetParent(enemyObj.transform);
        lightObj.transform.localPosition = Vector3.zero;

        Light2D light = lightObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity; // 5.0 is softer than 8.0 but still bright
        light.pointLightOuterRadius = radius; 
        light.pointLightInnerRadius = 1.0f; // Reduced inner slightly
        light.falloffIntensity = 0.6f; // Higher falloff = softer edge
    }

    public static void AttachProjectileLight(GameObject projectileObj, Color color, float intensity, float radius)
    {
        GameObject lightObj = new GameObject("Projectile Light 2D");
        lightObj.transform.SetParent(projectileObj.transform);
        lightObj.transform.localPosition = Vector3.zero;

        Light2D light = lightObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.pointLightOuterRadius = radius;
        light.falloffIntensity = 0.5f;
    }

    public static void AttachObstacleLight(GameObject obstacleObj, Color color, float intensity, float radius)
    {
        // Avoid duplicate lights
        if (obstacleObj.GetComponentInChildren<Light2D>() != null) return;

        GameObject lightObj = new GameObject("Obstacle Glow 2D");
        lightObj.transform.SetParent(obstacleObj.transform);
        lightObj.transform.localPosition = Vector3.zero;
        
        // Try to use Sprite Light for uniform glow
        SpriteRenderer sr = obstacleObj.GetComponent<SpriteRenderer>();
        Light2D light = lightObj.AddComponent<Light2D>();
        
        if (sr != null && sr.sprite != null)
        {
            // "Neon Tube" Effect: Logically matching the sprite shape
            light.lightType = Light2D.LightType.Sprite;
            light.lightCookieSprite = sr.sprite; 
            light.color = color;
            light.intensity = intensity; // 2.0 (Might need tuning for Sprite mode, usually needs higher)
        }
        else
        {
            // Fallback to Point if no sprite found
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightOuterRadius = radius;
            light.falloffIntensity = 0.5f;
        }
    }

    private ParticleSystem dustPS;

    // Update removed to prevent Editor errors.
    // Logic is handled in OnValidate.

    private void SetupDustParticles()
    {
        GameObject dustObj = new GameObject("Global Dust VFX");
        
        if (Camera.main != null)
        {
            dustObj.transform.SetParent(Camera.main.transform);
            dustObj.transform.localPosition = new Vector3(0, 0, 10);
        }

        dustPS = dustObj.AddComponent<ParticleSystem>();
        var main = dustPS.main;
        main.loop = true;
        main.startLifetime = 5f; // Shorter lifetime to recycle faster
        main.startSpeed = 0f; // Driven by VelocityOverLifetime
        main.startSize = 0.1f;
        main.startColor = new Color(1f, 1f, 1f, 0.3f); // Faint white
        main.maxParticles = 60; // Reduced from 150
        main.simulationSpace = ParticleSystemSimulationSpace.Local; // Move with camera (Local space)

        var emission = dustPS.emission;
        emission.rateOverTime = 8f; // Reduced from 20

        var shape = dustPS.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(30, 20, 1); // Cover screen

        var vel = dustPS.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local; // Local direction

        var renderer = dustObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
    }
}
