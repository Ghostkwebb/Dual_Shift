using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MobileDebugUI : MonoBehaviour
{
    private string debugInfo = "";
    private bool showDebug = false;
    private GUIStyle style;
    
    private void Start()
    {
        // Collect debug info at startup
        UpdateDebugInfo();
        
        // Auto-show on mobile for debugging
        #if !UNITY_EDITOR
        showDebug = true;
        #endif
    }
    
    private void UpdateDebugInfo()
    {
        debugInfo = "";
        debugInfo += $"Quality: {QualitySettings.names[QualitySettings.GetQualityLevel()]}\n";
        debugInfo += $"HDR Camera: {Camera.main?.allowHDR}\n";
        
        // Check URP asset
        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset != null)
        {
            debugInfo += $"URP HDR: {urpAsset.supportsHDR}\n";
        }
        else
        {
            debugInfo += "URP Asset: NULL!\n";
        }
        
        // Check bloom
        var volume = FindFirstObjectByType<Volume>();
        if (volume != null && volume.profile != null)
        {
            if (volume.profile.TryGet<Bloom>(out var bloom))
            {
                debugInfo += $"Bloom Active: {bloom.active}\n";
                debugInfo += $"Bloom Intensity: {bloom.intensity.value}\n";
                debugInfo += $"Bloom Threshold: {bloom.threshold.value}\n";
            }
            else
            {
                debugInfo += "Bloom: NOT FOUND\n";
            }
        }
        else
        {
            debugInfo += "Volume: NULL!\n";
        }
        
        debugInfo += $"API: {SystemInfo.graphicsDeviceType}\n";
    }
    
    private void Update()
    {
        // Toggle with 3-finger tap or D key
        if (Input.GetKeyDown(KeyCode.D) || Input.touchCount >= 3)
        {
            showDebug = !showDebug;
            if (showDebug) UpdateDebugInfo();
        }
    }
    
    private void OnGUI()
    {
        if (!showDebug) return;
        
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box);
            style.fontSize = 24;
            style.alignment = TextAnchor.UpperLeft;
            style.normal.textColor = Color.white;
        }
        
        GUI.Box(new Rect(10, 100, 400, 300), debugInfo, style);
        
        if (GUI.Button(new Rect(10, 410, 150, 50), "Close"))
        {
            showDebug = false;
        }
        
        if (GUI.Button(new Rect(170, 410, 150, 50), "Refresh"))
        {
            UpdateDebugInfo();
        }
    }
}
