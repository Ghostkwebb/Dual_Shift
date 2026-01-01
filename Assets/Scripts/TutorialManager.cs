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

        // Don't pause immediately if we want an entrance animation
        // But for tutorial we usually want instant stop.
        // Let's pause first, but UIAnimator uses unscaled time so it's fine.
        Time.timeScale = 0f;
        
        SetPanelActive(tutorialOverlay, true);
        instructionText.text = text;
    }

    private void Update()
    {
        if (!IsTutorialActive) return;
        bool inputReceived = false;
        
        // Null-check all input devices to prevent errors on devices without keyboard/mouse
        if ((Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) || 
            (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) ||
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame))
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
        SetPanelActive(tutorialOverlay, false);
    }

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel == null) return;
        
        if (panel.TryGetComponent<UIAnimator>(out var animator))
        {
            if (active) animator.Show();
            else animator.Hide();
        }
        else
        {
            panel.SetActive(active);
        }
    }

    public void FinishTutorialSequence()
    {
        PlayerPrefs.SetInt("TutorialDone", 1);
        PlayerPrefs.Save();
        InputsLocked = false; // Ensure inputs are free forever
    }
}