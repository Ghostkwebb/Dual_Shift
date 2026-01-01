using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using System;
using System.Collections.Generic;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance;

    // TEST IDs (Replace with Real IDs for Release)
    private string interstitialId = "ca-app-pub-2195761497058047/6075879131"; //ca-app-pub-3940256099942544/1033173712
    private string rewardedId = "ca-app-pub-2195761497058047/9372976372"; //ca-app-pub-3940256099942544/5224354917

    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;

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

    private void Start()
    {
        // 1. Check for Consent (GDPR)
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

        ConsentInformation.Update(request, OnConsentInfoUpdated);
    }

    private void OnConsentInfoUpdated(FormError error)
    {
        if (error != null)
        {
            Debug.LogError("Consent Error: " + error);
            return;
        }

        ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
        {
            if (formError != null)
            {
                Debug.LogError("Consent Form Error: " + formError);
                return;
            }
            
            if (ConsentInformation.CanRequestAds())
            {
                MobileAds.Initialize(initStatus =>
                {
                    LoadInterstitial();
                    LoadRewarded();
                });
            }
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

        InterstitialAd.Load(interstitialId, new AdRequest(),
            (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null) return;
                interstitialAd = ad;
                interstitialAd.OnAdFullScreenContentClosed += LoadInterstitial;
            });
    }

    public void ShowInterstitial()
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.Show();
        }
        else
        {
            LoadInterstitial();
        }
    }

    // --- REWARDED ---
    public void LoadRewarded()
    {
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        RewardedAd.Load(rewardedId, new AdRequest(),
            (RewardedAd ad, LoadAdError error) =>
            {
                if (error != null) return;
                rewardedAd = ad;
                rewardedAd.OnAdFullScreenContentClosed += LoadRewarded;
            });
    }

    public void ShowRewarded(Action<bool> onReward)
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            bool rewardEarned = false;
            
            void HandleAdClosed()
            {
                rewardedAd.OnAdFullScreenContentClosed -= HandleAdClosed;
                
                if (rewardEarned)
                {
                    onReward?.Invoke(true);
                }
                else
                {
                    onReward?.Invoke(false);
                }
                
                LoadRewarded();
            }

            rewardedAd.OnAdFullScreenContentClosed += HandleAdClosed;
            
            rewardedAd.Show((Reward reward) =>
            {
                rewardEarned = true;
            });
        }
        else
        {
            Debug.Log("Ad not ready.");
            onReward?.Invoke(false);
            LoadRewarded();
        }
    }
}