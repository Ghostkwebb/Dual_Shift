using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Settings")]
    public float worldSpeed = 10f;
    public float speedRamp = 0.1f;
    public int scorePerKill = 50;
    public float comboDuration = 2.0f;

    [Header("UI References")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private TMP_Text finalScoreText;


    private float score;
    private int comboMultiplier;
    private float comboTimer;
    private bool isGameOver = false;
    private bool isPaused = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Update()
    {
        if (isGameOver) return;

        // 1. Distance Score
        score += worldSpeed * Time.deltaTime;

        // 2. Speed Ramp (Difficulty)
        worldSpeed += speedRamp * Time.deltaTime;

        // 3. Combo Timer
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
        isGameOver = true;
        Time.timeScale = 0; // Freeze Game

        // Save High Score
        float bestScore = PlayerPrefs.GetFloat("BestScore", 0);
        if (score > bestScore) PlayerPrefs.SetFloat("BestScore", score);

        // Show UI
        scoreText.gameObject.SetActive(false);
        comboText.gameObject.SetActive(false);
        gameOverPanel.SetActive(true);
        finalScoreText.text = $"Score: {(int)score}\nBest: {(int)PlayerPrefs.GetFloat("BestScore")}";
    }

    public void RestartGame()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void UpdateUI()
    {
        scoreText.text = ((int)score).ToString("D5");

        if (comboMultiplier > 1)
        {
            comboText.text = $"x{comboMultiplier}";
            comboText.gameObject.SetActive(true);
        }
        else
        {
            comboText.gameObject.SetActive(false);
        }
    }

    public void TogglePause()
    {
        if (isGameOver) return;

        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0; // Freeze time
            pausePanel.SetActive(true);
        }
        else
        {
            Time.timeScale = 1; // Resume time
            pausePanel.SetActive(false);
        }
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1;
        // We will create the MainMenu scene later. For now, reload.
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}