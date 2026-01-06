using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;


public class AdManager : MonoBehaviour
{
    public static AdManager Instance;

    // TEST IDs (Replace with Real IDs for Release)
    private string interstitialId = "ca-app-pub-2195761497058047/7260698795"; //ca-app-pub-3940256099942544/1033173712
    private string rewardedId = "ca-app-pub-2195761497058047/4682636913"; //ca-app-pub-3940256099942544/5224354917

    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;

    // DEBUG UI

    private ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

    // Callbacks for loading flow
    private Action<bool> _onRewardedAdLoadComplete;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }



    private void Update()
    {
        while (mainThreadActions.TryDequeue(out Action action))
        {
            try 
            { 
                action.Invoke(); 
            }
            catch (Exception e) 
            { 
                 Log("<color=red>Background Action Crash: " + e.Message + "\n" + e.StackTrace + "</color>"); 
            }
        }


    }

    private void Start()
    {
        Log("Starting AdManager...");

        var debugSettings = new ConsentDebugSettings
        {
            DebugGeography = DebugGeography.EEA,
            TestDeviceHashedIds = new List<string>() { "YOUR_DEVICE_HASH_HERE" } 
        };

        ConsentRequestParameters request = new ConsentRequestParameters
        {
            TagForUnderAgeOfConsent = false,
            ConsentDebugSettings = debugSettings
        };

        Log("Checking Consent...");
        ConsentInformation.Update(request, OnConsentInfoUpdated);
    }

    private void Log(string msg) { Debug.Log("[AdManager] " + msg); }

    private void OnConsentInfoUpdated(FormError error)
    {
        mainThreadActions.Enqueue(() =>
        {
            if (error != null) Log("Consent Info Error: " + error.Message);

            Log("Consent Info Updated. Loading Form...");
            ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
            {
                mainThreadActions.Enqueue(() =>
                {
                    if (formError != null)
                    {
                        Log("Consent Form Error: " + formError.Message);
                        return;
                    }
                    
                    Log("CanRequestAds: " + ConsentInformation.CanRequestAds());

                    if (ConsentInformation.CanRequestAds())
                    {
                        Log("Initializing MobileAds...");
                        MobileAds.Initialize(initStatus =>
                        {
                            mainThreadActions.Enqueue(() => 
                            {
                                Log("MobileAds Initialized.");
                                LoadInterstitial();
                                LoadRewarded();
                            });
                        });
                    }
                    else
                    {
                        Log("Cannot Request Ads (Consent false).");
                    }
                });
            });
        });
    }

    // --- INTERSTITIAL ---
    public void LoadInterstitial()
    {
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        Log("Loading Interstitial...");
        InterstitialAd.Load(interstitialId, new AdRequest(), (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null)
            {
                    Log($"Interstitial Load Failed: {error.GetMessage()}");
                    return;
            }
            Log("Interstitial Loaded.");
            interstitialAd = ad;
            interstitialAd.OnAdFullScreenContentClosed += LoadInterstitial;
        });
    }

    public void ShowInterstitial()
    {
        if (interstitialAd != null && interstitialAd.CanShowAd()) interstitialAd.Show();
        else { Log("Interstitial not ready."); LoadInterstitial(); }
    }

    // --- REWARDED ---
    public void LoadRewarded()
    {
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        Log("Loading Rewarded...");
        RewardedAd.Load(rewardedId, new AdRequest(), (RewardedAd ad, LoadAdError error) =>
        {
            mainThreadActions.Enqueue(() => 
            {
                if (error != null)
                {
                     // Provide very clear feedback on Code 3
                     string err = $"Rewarded Load Failed: {error.GetMessage()} Code:{error.GetCode()}";
                     Log(err);
                     if(error.GetCode() == 3)
                     {
                         Log("<color=red>CODE 3 DETECTED: Account Config Issue!</color>");
                     }

                     _onRewardedAdLoadComplete?.Invoke(false);
                     _onRewardedAdLoadComplete = null;
                     return;
                }
                
                Log("Rewarded Loaded Successfully.");
                rewardedAd = ad;
                rewardedAd.OnAdFullScreenContentClosed += LoadRewarded;
                rewardedAd.OnAdFullScreenContentFailed += (AdError adError) =>
                {
                    Log("Rewarded Show Failed: " + adError.GetMessage());
                    LoadRewarded();
                };

                _onRewardedAdLoadComplete?.Invoke(true);
                _onRewardedAdLoadComplete = null;
            });
        });
    }

    // new Loading Screen Method
    public void ShowRewardedWithLoading(Action<bool> onReward)
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            ShowRewarded(onReward);
        }
        else
        {
            Log("Ad not ready. Showing Loading Screen...");
            // Use existing LoadingScreenManager
            if (LoadingScreenManager.Instance != null)
                LoadingScreenManager.Instance.ShowLoadingScreen("Loading Ad...");
            else
                Log("LoadingScreenManager not found!");
            
            // Set callback
            _onRewardedAdLoadComplete = (bool success) => 
            {
                if (LoadingScreenManager.Instance != null)
                    LoadingScreenManager.Instance.HideLoadingScreen();

                if (success)
                {
                    Log("Ad loaded during wait. Showing now.");
                    ShowRewarded(onReward);
                }
                else
                {
                    Log("Ad failed to load after wait.");
                    // Optional: Show a toast/message to user saying "Ad Unavailable"
                    onReward?.Invoke(false);
                }
            };

            LoadRewarded();
        }
    }

    public void ShowRewarded(Action<bool> onReward)
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            // Capture specific instance to avoid race conditions with checking 'this.rewardedAd'
            RewardedAd adToShow = rewardedAd;
            bool rewardEarned = false;
            
            void HandleAdClosed()
            {
                // Wrap ENTIRE callback in try-catch to diagnosis crash here
                try
                {
                    // Unsubscribe from the specific instance we showed
                    if (adToShow != null)
                    {
                        adToShow.OnAdFullScreenContentClosed -= HandleAdClosed;
                    }

                    // Dispatch result to main thread
                    mainThreadActions.Enqueue(() => 
                    {
                        try
                        {
                            if (rewardEarned)
                            {
                                Log("User earned reward. Invoking callback...");
                                if (onReward == null) Log("onReward is NULL!");
                                else onReward.Invoke(true);
                            }
                            else
                            {
                                Log("User closed without reward. Invoking callback...");
                                onReward?.Invoke(false);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"<color=red>Crash invoking callback: {ex.Message}\n{ex.StackTrace}</color>");
                        }
                        
                        // Load next ad
                        LoadRewarded();
                    });
                }
                catch (Exception e)
                {
                    // This catches the crash if 'adToShow' interaction fails 
                    // or if something else in this background callback fails
                     // We MUST still try to dispatch the result to main thread so the game doesn't hang
                    mainThreadActions.Enqueue(() => 
                    {
                        Log($"<color=red>HandleAdClosed CRASHED: {e.Message}</color>");
                        // Assume success if reward was earned before crash? Or just fail safe.
                        onReward?.Invoke(rewardEarned); 
                        LoadRewarded();
                    });
                }
            }

            adToShow.OnAdFullScreenContentClosed += HandleAdClosed;
            
            adToShow.Show((Reward reward) => { rewardEarned = true; });
        }
        else
        {
            Log("Rewarded Ad not ready (ShowRewarded called directly).");
            onReward?.Invoke(false);
            LoadRewarded();
        }
    }
}