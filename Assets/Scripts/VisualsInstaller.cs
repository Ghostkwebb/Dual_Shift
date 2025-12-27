using UnityEngine;
using UnityEngine.Rendering.Universal;

public class VisualsInstaller : MonoBehaviour
{
    [Header("Global Light Settings")]
    [Tooltip("Color tint for the global ambient light")]
    [SerializeField] private Color globalLightColor = new Color(0.18f, 0.15f, 0.25f);
    [Tooltip("Brightness of global ambient light (0-2)")]
    [SerializeField] private float globalLightIntensity = 1.0f;

    [Header("Player Light Settings")]
    [Tooltip("Color of the player's personal glow")]
    [SerializeField] private Color playerLightColor = Color.cyan;
    [Tooltip("Brightness of player light")]
    [SerializeField] private float playerLightIntensity = 1.5f;
    [Tooltip("Radius of player light in world units")]
    [SerializeField] private float playerLightRadius = 8.0f;

    [Header("Enemy Light Settings")]
    [Tooltip("Default color for enemy glow lights")]
    [SerializeField] private Color enemyLightColor = new Color(1f, 0f, 0.2f);

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
        if (Application.isPlaying) SetupGlobalLight();
    }

    private void OnValidate() => SetupGlobalLight();

    private void SetupGlobalLight()
    {
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

        if (globalLight == null && Application.isPlaying)
        {
            GameObject obj = new GameObject("Global Light 2D");
            globalLight = obj.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
        }

        if (globalLight != null)
        {
            globalLight.color = globalLightColor;
            globalLight.intensity = globalLightIntensity;
        }
    }

    private void SetupPlayerLight()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null || player.GetComponentInChildren<Light2D>() != null) return;

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

        GameObject backlightObj = new GameObject("Player Backlight 2D");
        backlightObj.transform.SetParent(player.transform);
        backlightObj.transform.localPosition = Vector3.zero;

        Light2D backlight = backlightObj.AddComponent<Light2D>();
        backlight.lightType = Light2D.LightType.Point;
        backlight.color = new Color(0.1f, 0.4f, 0.8f, 1f);
        backlight.intensity = 1.5f;
        backlight.pointLightOuterRadius = 15.0f;
        backlight.falloffIntensity = 0.6f;
    }

    public static void AttachEnemyLight(GameObject enemyObj, Color color, float intensity, float radius)
    {
        GameObject lightObj = new GameObject("Enemy Light 2D");
        lightObj.transform.SetParent(enemyObj.transform);
        lightObj.transform.localPosition = Vector3.zero;

        Light2D light = lightObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = 1.0f;
        light.falloffIntensity = 0.6f;
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
        if (obstacleObj.GetComponentInChildren<Light2D>() != null) return;

        SpriteRenderer sr = obstacleObj.GetComponent<SpriteRenderer>();
        Collider2D col = obstacleObj.GetComponent<Collider2D>();
        
        float height = 2f;
        if (sr != null) height = sr.bounds.size.y;
        else if (col != null) height = col.bounds.size.y;

        GameObject lightObj = new GameObject("Obstacle Glow 2D");
        lightObj.transform.SetParent(obstacleObj.transform);
        lightObj.transform.localPosition = new Vector3(0, height * 0.5f, 0);
        
        Light2D light = lightObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity * 2f;
        light.pointLightOuterRadius = Mathf.Max(radius, height * 1.5f);
        light.pointLightInnerRadius = 0.5f;
        light.falloffIntensity = 0.3f;
    }

    private ParticleSystem dustPS;

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
        main.startLifetime = 4f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f); // Slight size variation
        
        // Glowing color using HDR values (intensity > 1 creates bloom/glow effect)
        Color glowColor = new Color(0.7f, 0.8f, 1.2f, 0.4f); // Bright blue-white with glow
        main.startColor = glowColor;
        main.maxParticles = 60;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = dustPS.emission;
        emission.rateOverTime = 8f;

        var shape = dustPS.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(25, 15, 1);

        // Move particles LEFT to simulate world scrolling
        var vel = dustPS.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-8f, -12f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // Color over lifetime for subtle fade and glow pulse
        var colorOverLifetime = dustPS.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.6f, 0.7f, 1f), 0f),    // Start: soft blue
                new GradientColorKey(new Color(0.9f, 0.95f, 1f), 0.5f), // Mid: bright white-blue (glow peak)
                new GradientColorKey(new Color(0.5f, 0.6f, 0.9f), 1f)   // End: fade to softer blue
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),      // Fade in
                new GradientAlphaKey(0.5f, 0.3f), // Peak visibility
                new GradientAlphaKey(0.3f, 0.7f), // Start fading
                new GradientAlphaKey(0f, 1f)      // Fade out
            }
        );
        colorOverLifetime.color = gradient;

        var renderer = dustObj.GetComponent<ParticleSystemRenderer>();
        
        // Use URP particle shader for proper HDR/bloom support
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        
        if (shader != null)
        {
            Material mat = new Material(shader);
            
            // Enable HDR emission for bloom glow
            mat.EnableKeyword("_EMISSION");
            
            // Set base color with HDR intensity (values > 1 trigger bloom)
            Color hdrGlowColor = Color.white * 2.5f; // White glow, 2.5x intensity for bloom
            mat.SetColor("_BaseColor", hdrGlowColor);
            mat.SetColor("_EmissionColor", hdrGlowColor);
            
            // Set to additive blending for glow effect
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 1); // Additive
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_ZWrite", 0);
            mat.renderQueue = 3000; // Transparent queue
            
            renderer.material = mat;
        }
        else
        {
            renderer.material = new Material(Shader.Find("Sprites/Default"));
        }
    }
}
