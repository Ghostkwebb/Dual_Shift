using UnityEngine;

public class TutorialEnder : MonoBehaviour
{
    private void Start()
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.FinishTutorialSequence();
            Debug.Log("Tutorial Completed & Saved!");
        }
    }
}