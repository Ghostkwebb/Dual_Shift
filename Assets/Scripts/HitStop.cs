using UnityEngine;
using System.Collections;

public class HitStop : MonoBehaviour
{
    public static HitStop Instance { get; private set; }

    private bool isStopping = false;

    private void Awake()
    {
        Instance = this;
    }

    public void Stop(float duration)
    {
        if (isStopping) return; // Don't stack stops
        StartCoroutine(StopRoutine(duration));
    }

    private IEnumerator StopRoutine(float duration)
    {
        isStopping = true;

        // 1. Store previous scale (usually 1)
        float originalScale = Time.timeScale;
        
        // 2. Freeze
        Time.timeScale = 0f;

        // 3. Wait (using Realtime, because game time is frozen)
        yield return new WaitForSecondsRealtime(duration);

        // 4. Resume (Only if game is still playing!)
        if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            Time.timeScale = originalScale;
        }

        isStopping = false;
    }
}