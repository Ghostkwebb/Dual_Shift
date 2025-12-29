using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem;

public class SplashManager : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "MainScene";
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float fadeDuration = 0.5f;

    private VideoPlayer videoPlayer;
    private RawImage rawImage;
    private bool isTransitioning = false;

    private void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        rawImage = GetComponent<RawImage>();
    }

    private void Start()
    {
        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        rawImage.texture = vp.texture;
        rawImage.color = Color.white;
        vp.Play();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        StartCoroutine(TransitionRoutine());
    }

    public void Skip()
    {
        if (!isTransitioning) StartCoroutine(TransitionRoutine());
    }

    private void Update()
    {
        bool inputReceived = false;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            inputReceived = true;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            inputReceived = true;


        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            inputReceived = true;

        if (inputReceived)
        {
            Skip();
        }
    }

    private IEnumerator TransitionRoutine()
    {
        isTransitioning = true;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeOverlay.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }

        fadeOverlay.alpha = 1f;
        SceneManager.LoadScene(nextSceneName);
    }
}