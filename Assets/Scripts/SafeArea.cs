using UnityEngine;

public class SafeArea : MonoBehaviour
{
    private RectTransform panel;

    void Awake()
    {
        panel = GetComponent<RectTransform>();
        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        Rect safeArea = Screen.safeArea;
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        panel.anchorMin = anchorMin;
        panel.anchorMax = anchorMax;
    }
}