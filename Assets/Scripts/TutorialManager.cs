using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject tutorialOverlay;
    [SerializeField] private TMP_Text instructionText;

    [Header("Player Reference")]
    [SerializeField] private PlayerController player; 

    public bool IsTutorialActive { get; private set; } = false;
    public bool InputsLocked { get; private set; } = false; 
    
    private string expectedAction; 

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        // If Tutorial NOT done, Lock Inputs immediately
        if (PlayerPrefs.GetInt("TutorialDone", 0) == 0)
        {
            InputsLocked = true;
        }
        
        tutorialOverlay.SetActive(false);
    }

    public void TriggerTutorial(string text, string actionKey)
    {
        if (IsTutorialActive) return;

        IsTutorialActive = true;
        expectedAction = actionKey;

        Time.timeScale = 0f;
        tutorialOverlay.SetActive(true);
        instructionText.text = text;
    }

    private void Update()
    {
        if (!IsTutorialActive) return;
        bool inputReceived = false;
        if (Keyboard.current.spaceKey.wasPressedThisFrame || 
            (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) ||
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            inputReceived = true;
        }

        if (inputReceived)
        {
            PerformTutorialAction();
            ResumeGame();
        }
    }

    private void PerformTutorialAction()
    {
        if (expectedAction == "Switch")
        {
            player.ExecuteLaneSwitch();
            InputsLocked = false; 
        }
        else if (expectedAction == "Attack")
        {
            player.ExecuteAttack();
            InputsLocked = false;
        }
    }

    public void ResumeGame()
    {
        IsTutorialActive = false;
        Time.timeScale = 1f;
        tutorialOverlay.SetActive(false);
    }

    public void FinishTutorialSequence()
    {
        PlayerPrefs.SetInt("TutorialDone", 1);
        PlayerPrefs.Save();
        InputsLocked = false; // Ensure inputs are free forever
    }
}