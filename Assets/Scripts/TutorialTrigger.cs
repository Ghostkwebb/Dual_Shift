using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [TextArea] public string textToDisplay = "Tap to Switch!";
    public string actionType = "Switch"; // "Switch" or "Attack"

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Tutorial Trigger hit by: {other.gameObject.name}");

        if (other.CompareTag("Player"))
        {
            Debug.Log("Tag matched! Checking PlayerPrefs...");
            
            if (PlayerPrefs.GetInt("TutorialDone", 0) == 0)
            {
                Debug.Log("Tutorial Triggering NOW.");
                TutorialManager.Instance.TriggerTutorial(textToDisplay, actionType);
            }
            else
            {
                Debug.Log("Tutorial skipped (TutorialDone is 1).");
            }
            
            Destroy(gameObject);
        }
    }
}