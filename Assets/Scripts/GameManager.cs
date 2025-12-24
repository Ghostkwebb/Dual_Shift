using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, Playing, GameOver }
    public GameState CurrentState { get; private set; }

    [Header("Settings")]
    [Tooltip("The speed of the world over time. Draw a plateau for the 'Sweet Spot'.")]
    public AnimationCurve speedCurve; 
    [Tooltip("If the game lasts longer than the curve, add this much speed per second.")]
    public float lateGameRamp = 0.5f;
    public float worldSpeed; 
    [Tooltip("Points awarded for destroying a single enemy.")]
    public int scorePerKill = 50;
    [Tooltip("Time in seconds before the combo multiplier resets.")]
    public float comboDuration = 2.0f;

    [Header("UI References")]
    [Tooltip("UI Text for the running score.")]
    [SerializeField] private TMP_Text scoreText;
    [Tooltip("UI Text for the current combo multiplier.")]
    [SerializeField] private TMP_Text comboText;
    [Tooltip("The Panel object shown upon death.")]
    [SerializeField] private GameObject gameOverPanel;
    [Tooltip("UI Text on the Game Over screen for final stats.")]
    [SerializeField] private TMP_Text finalScoreText;
    [Tooltip("The Panel object shown when paused.")]
    [SerializeField] private GameObject pausePanel;
    [Tooltip("The Panel object for the Main Menu.")]
    [SerializeField] private GameObject mainMenuPanel;
    [Tooltip("Parent object containing In-Game UI (Score, Pause Button).")]
    [SerializeField] private GameObject gameHUD;

    private float score;
    private int kills;

    private int comboMultiplier;
    private float comboTimer;
    private bool isPaused = false;
    private float levelTime = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        Application.targetFrameRate = 120;
    }

    private void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        CurrentState = GameState.Menu;

        // Reset Logic
        worldSpeed = 0; 
        score = 0;
        kills = 0;
        comboMultiplier = 0;
        levelTime = 0f; 

        // UI State
        mainMenuPanel.SetActive(true);
        gameHUD.SetActive(false);
        gameOverPanel.SetActive(false);
        pausePanel.SetActive(false);

        Time.timeScale = 1; 
    }

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        levelTime = 0f;
        worldSpeed = speedCurve.Evaluate(0f); 

        mainMenuPanel.SetActive(false);
        gameHUD.SetActive(true);
    }

    private void Update()
    {
        if (CurrentState != GameState.Playing) return;
        levelTime += Time.deltaTime;
        
        if (speedCurve.length > 0)
        {
            float curveDuration = speedCurve.keys[speedCurve.length - 1].time;

            if (levelTime <= curveDuration)
            {
                worldSpeed = speedCurve.Evaluate(levelTime);
            }
            else
            {
                float lastSpeed = speedCurve.Evaluate(curveDuration);
                float timePassedSinceEnd = levelTime - curveDuration;
                worldSpeed = lastSpeed + (timePassedSinceEnd * lateGameRamp);
            }
        }

        score += worldSpeed * Time.deltaTime;

        if (comboTimer > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0)
            {
                comboMultiplier = 0;
                UpdateUI();
            }
        }
        UpdateUI();
    }

    public void AddKill()
    {
        kills++;
        comboMultiplier++;
        comboTimer = comboDuration;
        score += scorePerKill * comboMultiplier;
        UpdateUI();
    }

    public void GameOver()
    {
        if (CurrentState == GameState.GameOver) return; 

        StartCoroutine(GameOverSequence());
    }

    private System.Collections.IEnumerator GameOverSequence()
    {
        CurrentState = GameState.GameOver;

        CameraShake.Instance.Shake(1.2f, 0.5f);
        Time.timeScale = 0.1f;
        yield return new WaitForSecondsRealtime(1.0f);
        Time.timeScale = 0;

        // --- SAVE BEST SCORE ---
        float bestScore = PlayerPrefs.GetFloat("BestScore", 0);
        if (score > bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetFloat("BestScore", score);
        }

        // --- SAVE MAX KILLS ---
        int maxKills = PlayerPrefs.GetInt("MaxKills", 0);
        if (kills > maxKills)
        {
            maxKills = kills;
            PlayerPrefs.SetInt("MaxKills", kills);
        }

        gameHUD.SetActive(false);
        gameOverPanel.SetActive(true);

        finalScoreText.text = $"SCORE: {(int)score}\n" +
                              $"BEST: {(int)bestScore}\n\n" +
                              $"KILLS: {kills}\n" +
                              $"MAX KILLS: {maxKills}";
    }

    public void RestartGame()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void TogglePause()
    {
        if (CurrentState != GameState.Playing) return;

        isPaused = !isPaused;
        pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0 : 1;
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1;
        RestartGame(); 
    }

    private void UpdateUI()
    {
        scoreText.text = ((int)score).ToString("D5");
        comboText.gameObject.SetActive(comboMultiplier > 1);
        if (comboMultiplier > 1) comboText.text = $"x{comboMultiplier}";
    }
}