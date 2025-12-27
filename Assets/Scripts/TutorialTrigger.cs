using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [TextArea] public string textToDisplay = "Tap to Switch!";
    public string actionType = "Switch"; // "Switch" or "Attack"

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Only trigger if tutorial is NOT done
            if (PlayerPrefs.GetInt("TutorialDone", 0) == 0)
            {
                TutorialManager.Instance.TriggerTutorial(textToDisplay, actionType);
            }
            
            // Destroy trigger so it doesn't happen again this run
            Destroy(gameObject);
        }
    }
}