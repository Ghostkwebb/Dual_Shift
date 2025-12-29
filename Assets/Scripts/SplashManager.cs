using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

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
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
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