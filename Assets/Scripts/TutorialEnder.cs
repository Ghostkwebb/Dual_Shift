using UnityEngine;

public class TutorialEnder : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.FinishTutorialSequence();
                Debug.Log("Tutorial Completed & Saved! (Player reached end)");
            }
        }
    }
}