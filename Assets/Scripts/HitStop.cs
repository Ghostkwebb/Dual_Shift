using UnityEngine;
using System.Collections;

public class HitStop : MonoBehaviour
{
    public static HitStop Instance { get; private set; }
    private bool isStopping = false;

    private void Awake() => Instance = this;

    public void Stop(float duration)
    {
        if (isStopping) return;
        StartCoroutine(StopRoutine(duration));
    }

    private IEnumerator StopRoutine(float duration)
    {
        isStopping = true;
        float originalScale = Time.timeScale;
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(duration);

        if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            Time.timeScale = originalScale;

        isStopping = false;
    }
}