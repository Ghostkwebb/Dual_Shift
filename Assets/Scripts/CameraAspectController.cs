using UnityEngine;

[ExecuteInEditMode]
public class CameraAspectController : MonoBehaviour
{
    [Tooltip("The vertical size you want on a standard 16:9 phone.")]
    [SerializeField] private float targetSize = 5f;

    [Tooltip("The aspect ratio you designed for (e.g., 16/9 = 1.77).")]
    [SerializeField] private float targetAspect = 16f / 9f;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        AdjustCamera();
    }

    void Update()
    {
#if UNITY_EDITOR
        AdjustCamera(); // Update in editor to test resizing
#endif
    }

    void AdjustCamera()
    {
        float currentAspect = (float)Screen.width / Screen.height;

        if (currentAspect >= targetAspect)
        {
            // Screen is WIDER than 16:9 (e.g., iPhone X, Samsung S20)
            // Keep fixed height. We just see more level ahead.
            cam.orthographicSize = targetSize;
        }
        else
        {
            // Screen is NARROWER than 16:9 (e.g., iPad)
            // Zoom out so the width matches the 16:9 width.
            // This prevents "blind" play where enemies appear too suddenly.
            cam.orthographicSize = targetSize * (targetAspect / currentAspect);
        }
    }
}