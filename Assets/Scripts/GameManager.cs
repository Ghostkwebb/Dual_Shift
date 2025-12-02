using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, Playing, GameOver }
    public GameState CurrentState { get; private set; }

    [Header("Settings")]
    public float initialWorldSpeed = 10f;
    public float worldSpeed; // Internal tracking
    public float speedRamp = 0.1f;
    public int scorePerKill = 50;
    public float comboDuration = 2.0f;

    [Header("UI References")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gameHUD; // Parent object for Score/Combo/PauseButton

    private float score;
    private int comboMultiplier;
    private float comboTimer;
    private bool isPaused = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        CurrentState = GameState.Menu;

        // Reset Logic
        worldSpeed = 0; // Stop the world
        score = 0;

        // UI State
        mainMenuPanel.SetActive(true);
        gameHUD.SetActive(false);
        gameOverPanel.SetActive(false);
        pausePanel.SetActive(false);

        Time.timeScale = 1; // Keep time running so Idle animations play
    }

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        worldSpeed = initialWorldSpeed;

        mainMenuPanel.SetActive(false);
        gameHUD.SetActive(true);
    }

    private void Update()
    {
        if (CurrentState != GameState.Playing) return;

        // Score & Speed logic
        score += worldSpeed * Time.deltaTime;
        worldSpeed += speedRamp * Time.deltaTime;

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
        comboMultiplier++;
        comboTimer = comboDuration;
        score += scorePerKill * comboMultiplier;
        UpdateUI();
    }

    public void GameOver()
    {
        CurrentState = GameState.GameOver;
        Time.timeScale = 0;

        float bestScore = PlayerPrefs.GetFloat("BestScore", 0);
        if (score > bestScore) PlayerPrefs.SetFloat("BestScore", score);

        gameHUD.SetActive(false);
        gameOverPanel.SetActive(true);
        finalScoreText.text = $"Score: {(int)score}\nBest: {(int)PlayerPrefs.GetFloat("BestScore")}";
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
        RestartGame(); // Simpler for now, reloads scene to Menu
    }

    private void UpdateUI()
    {
        scoreText.text = ((int)score).ToString("D5");
        comboText.gameObject.SetActive(comboMultiplier > 1);
        if (comboMultiplier > 1) comboText.text = $"x{comboMultiplier}";
    }
}