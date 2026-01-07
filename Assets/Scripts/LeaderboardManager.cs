using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine.SocialPlatforms;

public class LeaderboardManager : MonoBehaviour
{
    [Header("Custom UI")]
    [Tooltip("The panel containing the custom leaderboard UI")]
    [SerializeField] private GameObject customLeaderboardPanel;
    [Tooltip("Container for leaderboard rows")]
    [SerializeField] private Transform rowContainer; 
    [Tooltip("Prefab for leaderboard row")]
    [SerializeField] private GameObject rowPrefab;   
    [Tooltip("The row displaying the player's own score")]
    [SerializeField] private LeaderboardRowUI myScoreRow; 
    
    public static LeaderboardManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

#if UNITY_ANDROID
        try
        {
            PlayGamesPlatform.DebugLogEnabled = true;
            PlayGamesPlatform.Activate();
            // Explicitly force the assignment to be sure
            Social.Active = PlayGamesPlatform.Instance;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[GPGS] Failed to Activate: " + e.Message);
        }
#endif
    }

    private void Start()
    {
#if UNITY_ANDROID
        try
        {
            Debug.Log("[GPGS] Starting Authentication...");
            PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[GPGS] Failed to Authenticate: " + e.Message);
        }
#endif
    }

    private void ProcessAuthentication(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
            Debug.Log("[GPGS] Authenticated successfully.");
            // Re-enforce Social.Active just in case
            Social.Active = PlayGamesPlatform.Instance;
            
            // Sync cloud scores to local device
            SyncCloudScores();
        }
        else
        {
            Debug.LogError($"[GPGS] Authentication Failed. Status: {status}");
        }
    }

    private void SyncCloudScores()
    {
#if UNITY_ANDROID
        if (PlayGamesPlatform.Instance == null || !PlayGamesPlatform.Instance.IsAuthenticated()) return;

        Debug.Log("[GPGS] Starting Score Sync...");

        // 1. Sync High Score
        PlayGamesPlatform.Instance.LoadScores(
            GPGSIds.leaderboard_high_scores,
            LeaderboardStart.PlayerCentered,
            1,
            LeaderboardCollection.Public,
            LeaderboardTimeSpan.AllTime,
            (data) =>
            {
                if (data.Valid && data.PlayerScore != null)
                {
                    long cloudScore = data.PlayerScore.value;
                    float localScore = PlayerPrefs.GetFloat("BestScore", 0);
                    
                    Debug.Log($"[GPGS] Cloud Score: {cloudScore}, Local Score: {localScore}");

                    if (cloudScore > localScore)
                    {
                        Debug.Log("[GPGS] Cloud score is higher. Updating local BestScore.");
                        PlayerPrefs.SetFloat("BestScore", cloudScore);
                        PlayerPrefs.Save();
                    }
                }
            }
        );

        // 2. Sync Max Kills
        PlayGamesPlatform.Instance.LoadScores(
             GPGSIds.leaderboard_max_kills,
             LeaderboardStart.PlayerCentered,
             1,
             LeaderboardCollection.Public,
             LeaderboardTimeSpan.AllTime,
             (data) =>
             {
                 if (data.Valid && data.PlayerScore != null)
                 {
                     long cloudKills = data.PlayerScore.value;
                     int localKills = PlayerPrefs.GetInt("MaxKills", 0);
                     
                     Debug.Log($"[GPGS] Cloud Kills: {cloudKills}, Local Kills: {localKills}");

                     if (cloudKills > localKills)
                     {
                         Debug.Log("[GPGS] Cloud kills are higher. Updating local MaxKills.");
                         PlayerPrefs.SetInt("MaxKills", (int)cloudKills);
                         PlayerPrefs.Save();
                     }
                 }
             }
         );
#endif
    }

    public void SubmitScore(long score)
    {
        try
        {
            if (PlayGamesPlatform.Instance != null && PlayGamesPlatform.Instance.IsAuthenticated())
            {
                PlayGamesPlatform.Instance.ReportScore(score, GPGSIds.leaderboard_high_scores, (bool success) => {});
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[GPGS] SubmitScore error: " + e.Message);
        }
    }
    
    public void SubmitKills(long kills)
    {
        try
        {
            if (PlayGamesPlatform.Instance != null && PlayGamesPlatform.Instance.IsAuthenticated())
            {
                PlayGamesPlatform.Instance.ReportScore(kills, GPGSIds.leaderboard_max_kills, (bool success) => {});
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[GPGS] SubmitKills error: " + e.Message);
        }
    }

    public void ShowLeaderboard()
    {
#if UNITY_ANDROID
        try
        {
            if (PlayGamesPlatform.Instance == null)
            {
                Debug.LogWarning("[GPGS] PlayGamesPlatform.Instance is null!");
                return;
            }
            
            if (!PlayGamesPlatform.Instance.IsAuthenticated())
            {
                PlayGamesPlatform.Instance.ManuallyAuthenticate(ProcessAuthentication);
                return;
            }

            if (customLeaderboardPanel != null)
            {
                customLeaderboardPanel.SetActive(true);
                OpenHighScoreTab();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[GPGS] ShowLeaderboard error: " + e.Message);
        }
#endif
    }
    
    public void CloseLeaderboard()
    {
        if (customLeaderboardPanel != null)
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
#if UNITY_ANDROID
        try
        {
            if (PlayGamesPlatform.Instance != null && !PlayGamesPlatform.Instance.IsAuthenticated())
            {
                PlayGamesPlatform.Instance.ManuallyAuthenticate(ProcessAuthentication);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[GPGS] SignIn error: " + e.Message);
        }
#endif
    }
    
    private void RefreshBoard(string leaderboardId)
    {
        if (PlayGamesPlatform.Instance == null)
        {
            Debug.LogWarning("[GPGS] PlayGamesPlatform.Instance is null in RefreshBoard!");
            return;
        }
        
        if (rowContainer != null)
        {
            foreach (Transform child in rowContainer) Destroy(child.gameObject);
        }

        PlayGamesPlatform.Instance.LoadScores(
            leaderboardId,
            LeaderboardStart.TopScores,
            10,
            LeaderboardCollection.Public,
            LeaderboardTimeSpan.AllTime,
            (data) =>
            {
                if (data.Valid)
                {
                    List<string> userIds = new List<string>();
                    foreach (var score in data.Scores)
                    {
                        userIds.Add(score.userID);
                    }

                    // Explicitly use PlayGamesPlatform instance cast to ISocialPlatform for LoadUsers
                    ((ISocialPlatform)PlayGamesPlatform.Instance).LoadUsers(userIds.ToArray(), (users) =>
                    {
                        Dictionary<string, string> names = new Dictionary<string, string>();
                        
                        if (users != null)
                        {
                            foreach (var user in users)
                            {
                                names[user.id] = user.userName;
                            }
                        }

                        foreach (var score in data.Scores)
                        {
                            GameObject rowObj = Instantiate(rowPrefab, rowContainer);
                            LeaderboardRowUI rowScript = rowObj.GetComponent<LeaderboardRowUI>();

                            string displayName = score.userID; 

                            if (names.ContainsKey(score.userID))
                            {
                                displayName = names[score.userID];
                            }
                            
                            // Use PlayGamesPlatform local user
                            if (score.userID == PlayGamesPlatform.Instance.localUser.id)
                            {
                                displayName = $"YOU ({PlayGamesPlatform.Instance.localUser.userName})";
                            }

                            rowScript.SetData(
                                score.rank.ToString(), 
                                displayName, 
                                score.value.ToString()
                            );
                        }
                    });
                    
                    if (data.PlayerScore != null)
                    {
                        myScoreRow.gameObject.SetActive(true);
                        myScoreRow.SetData(
                            data.PlayerScore.rank.ToString(),
                            $"YOU ({PlayGamesPlatform.Instance.localUser.userName})", 
                            data.PlayerScore.value.ToString()
                        );
                    }
                    else
                    {
                        myScoreRow.gameObject.SetActive(false);
                    }
                }
                else
                {
                    Debug.LogWarning("Error loading leaderboard data.");
                }
            }
        );
    }
    
    public void FetchAndShowCustomUI()
    {
        if (!PlayGamesPlatform.Instance.IsAuthenticated())
        {
            return;
        }

        customLeaderboardPanel.SetActive(true);
        foreach (Transform child in rowContainer) Destroy(child.gameObject);

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
                    foreach (var score in data.Scores)
                    {
                        GameObject rowObj = Instantiate(rowPrefab, rowContainer);
                        LeaderboardRowUI rowScript = rowObj.GetComponent<LeaderboardRowUI>();
                        
                        rowScript.SetData(
                            score.rank.ToString(), 
                            score.userID, 
                            score.value.ToString()
                        );
                    }
                }
                else
                {
                    Debug.LogWarning("Error loading leaderboard data.");
                }
            }
        );
    }
}