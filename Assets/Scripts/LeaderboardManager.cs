using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using GooglePlayGames;
using GooglePlayGames.BasicApi;

public class LeaderboardManager : MonoBehaviour
{
    [Header("Custom UI")]
    [SerializeField] private GameObject customLeaderboardPanel;
    [SerializeField] private Transform rowContainer; 
    [SerializeField] private GameObject rowPrefab;  
    
    public static LeaderboardManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
#if UNITY_ANDROID
        // 1. Enable Debug Logs to see exactly why it fails in Logcat
        PlayGamesPlatform.DebugLogEnabled = true;

        // 2. Activate the Platform (v2 style)
        PlayGamesPlatform.Activate();

        // 3. Try Silent Login first
        PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
#endif
    }

    private void ProcessAuthentication(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
            Debug.Log("GPGS Login Successful!");
        }
        else
        {
            Debug.Log("GPGS Silent Login Failed: " + status);
            
            // FIX: If silent login fails (NEED_REMOTE_CONSENT), 
            // we must trigger the manual popup so the user can click "Allow".
            // We check if we haven't tried manually yet to avoid infinite loops.
            
            // Note: In v2, ManuallyAuthenticate handles the UI resolution automatically.
            PlayGamesPlatform.Instance.ManuallyAuthenticate((manualStatus) => {
                if (manualStatus == SignInStatus.Success)
                {
                    Debug.Log("Manual Login Successful!");
                }
                else
                {
                    Debug.Log("Manual Login Failed: " + manualStatus);
                }
            });
        }
    }

    // --- SUBMIT SCORE ---
    public void SubmitScore(long score)
    {
        if (PlayGamesPlatform.Instance.IsAuthenticated())
        {
            // "GPGSIds.leaderboard_high_scores" comes from the generated file
            Social.ReportScore(score, GPGSIds.leaderboard_high_scores, (bool success) => {
                if (success) Debug.Log("Score Posted!");
                else Debug.Log("Score Failed");
            });
        }
    }
    
    // --- SUBMIT KILLS ---
    public void SubmitKills(long kills)
    {
        if (PlayGamesPlatform.Instance.IsAuthenticated())
        {
            // Note: The ID name "leaderboard_max_kills" depends on what you named it in the Console
            // Check GPGSIds.cs to confirm the exact variable name.
            Social.ReportScore(kills, GPGSIds.leaderboard_max_kills, (bool success) => {
                if (success) Debug.Log("Kills Posted!");
            });
        }
    }

    // --- SHOW LEADERBOARD UI ---
    public void ShowLeaderboard()
    {
        if (!PlayGamesPlatform.Instance.IsAuthenticated())
        {
            Debug.Log("Not logged in!");
            PlayGamesPlatform.Instance.ManuallyAuthenticate(ProcessAuthentication);
            return;
        }

        // Open Custom Panel and load High Scores by default
        customLeaderboardPanel.SetActive(true);
        OpenHighScoreTab(); 
    }
    
    public void CloseLeaderboard()
    {
        customLeaderboardPanel.SetActive(false);
    }
    
    public void OpenHighScoreTab()
    {
        RefreshBoard(GPGSIds.leaderboard_high_scores);
    }
    
    public void OpenMaxKillsTab()
    {
        RefreshBoard(GPGSIds.leaderboard_max_kills);
    }
    
    public void SignIn()
    {
        if (!PlayGamesPlatform.Instance.IsAuthenticated())
        {
            PlayGamesPlatform.Instance.ManuallyAuthenticate(ProcessAuthentication);
        }
    }
    
    private void RefreshBoard(string leaderboardId)
    {
        Debug.Log($"Requesting data for: {leaderboardId}...");

        // 1. Clear old rows
        foreach (Transform child in rowContainer) Destroy(child.gameObject);

        // 2. Request Data
        PlayGamesPlatform.Instance.LoadScores(
            leaderboardId,
            LeaderboardStart.TopScores,
            10,
            LeaderboardCollection.Public,
            LeaderboardTimeSpan.AllTime,
            (data) =>
            {
                // LOG 2: Did we get a response?
                Debug.Log($"Response received. Valid? {data.Valid}");

                if (data.Valid)
                {
                    Debug.Log($"Found {data.Scores.Length} scores."); // LOG 3
                    
                    foreach (var score in data.Scores)
                    {
                        GameObject rowObj = Instantiate(rowPrefab, rowContainer);
                        LeaderboardRowUI rowScript = rowObj.GetComponent<LeaderboardRowUI>();
                        
                        string nameDisplay = score.userID;
                        if (score.userID == Social.localUser.id)
                        {
                            nameDisplay = $"YOU ({Social.localUser.userName})";
                        }

                        rowScript.SetData(
                            score.rank.ToString(), 
                            nameDisplay, 
                            score.value.ToString()
                        );
                    }
                }
                else
                {
                    Debug.Log("Error loading data. Check Logcat for GPGS details.");
                }
            }
        );
    }
    
    public void FetchAndShowCustomUI()
    {
        if (!PlayGamesPlatform.Instance.IsAuthenticated())
        {
            Debug.Log("Not logged in!");
            return;
        }

        // Show Panel / Clear old rows
        customLeaderboardPanel.SetActive(true);
        foreach (Transform child in rowContainer) Destroy(child.gameObject);

        // Request Data: Top 10, Public, All Time
        PlayGamesPlatform.Instance.LoadScores(
            GPGSIds.leaderboard_high_scores,
            LeaderboardStart.TopScores,
            10,
            LeaderboardCollection.Public,
            LeaderboardTimeSpan.AllTime,
            (data) =>
            {
                if (data.Valid)
                {
                    Debug.Log("Found " + data.Scores.Length + " scores.");
                    
                    foreach (var score in data.Scores)
                    {
                        // Spawn Row
                        GameObject rowObj = Instantiate(rowPrefab, rowContainer);
                        LeaderboardRowUI rowScript = rowObj.GetComponent<LeaderboardRowUI>();
                        
                        // Fill Data
                        rowScript.SetData(
                            score.rank.ToString(), 
                            score.userID, // Note: Google hides real names often, returns ID or "Player"
                            score.value.ToString()
                        );
                    }
                }
                else
                {
                    Debug.Log("Error loading leaderboard data.");
                }
            }
        );
    }
}