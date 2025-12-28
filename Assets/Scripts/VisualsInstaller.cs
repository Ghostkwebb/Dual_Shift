using UnityEngine;
using UnityEngine.Rendering.Universal;

public class VisualsInstaller : MonoBehaviour
{
    [Header("Global Light")]
    [SerializeField] private Color globalLightColor = new Color(0.22f, 0.19f, 0.28f);
    [SerializeField] private float globalLightIntensity = 1.1f;

    [Header("Player Light")]
    [SerializeField] private Color playerLightColor = Color.cyan;
    [SerializeField] private float playerLightIntensity = 1.5f;
    [SerializeField] private float playerLightRadius = 8.0f;

    [Header("Enemy Light")]
    [SerializeField] private Color enemyLightColor = new Color(1f, 0f, 0.2f);

    private ParticleSystem dustPS;

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
        main.startLifetime = 5f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
        main.startColor = Color.white;
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = dustPS.emission;
        emission.rateOverTime = 8f;

        var shape = dustPS.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(25, 15, 1);

        var vel = dustPS.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-8f, -12f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var colorOverLifetime = dustPS.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.15f),
                new GradientAlphaKey(1f, 0.85f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        var renderer = dustObj.GetComponent<ParticleSystemRenderer>();
        
        // Load material from Resources (ensures URP Particles shader is included in build)
        Material mat = Resources.Load<Material>("ParticleGlow");
        if (mat != null)
        {
            mat = new Material(mat);
        }
        else
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            mat = new Material(shader);
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 1);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
        }
        
        renderer.material = mat;
    }
}
