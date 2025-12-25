using UnityEngine;

[ExecuteInEditMode]
public class CameraAspectController : MonoBehaviour
{
    [Tooltip("The vertical size you want on a standard 16:9 phone.")]
    [SerializeField] private float targetSize = 5f; 
    
    [Tooltip("The aspect ratio you designed for (16/9 = 1.77).")]
    [SerializeField] private float targetAspect = 16f / 9f;

    [Tooltip("0 = Fixed Height (Tiny on UltraWide). 1 = Fixed Width (Zoomed on UltraWide). Try 0.5.")]
    [Range(0f, 1f)] [SerializeField] private float wideScreenZoom = 0.5f;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        AdjustCamera();
    }

    void Update()
    {
#if UNITY_EDITOR
        AdjustCamera(); 
#endif
    }

    void AdjustCamera()
    {
        if (cam == null) cam = GetComponent<Camera>();

        float currentAspect = (float)Screen.width / Screen.height;

        if (currentAspect >= targetAspect)
        {
            float ratioDifference = currentAspect / targetAspect;
            float zoomFactor = Mathf.Lerp(1f, ratioDifference, wideScreenZoom);
            cam.orthographicSize = targetSize / zoomFactor;
        }
        else
        {
            cam.orthographicSize = targetSize * (targetAspect / currentAspect);
        }
    }
}