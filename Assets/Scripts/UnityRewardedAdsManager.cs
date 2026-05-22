using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;

/// <summary>
/// One place for Unity Ads init and rewarded Load/Show. Use different ad unit IDs in the dashboard
/// for shop diamonds vs revive (or reuse the same placement if you prefer).
/// Add this to your first-loaded scene (e.g. Home); it persists with DontDestroyOnLoad.
/// </summary>
public class UnityRewardedAdsManager : MonoBehaviour,
    IUnityAdsInitializationListener,
    IUnityAdsLoadListener,
    IUnityAdsShowListener
{
    public static UnityRewardedAdsManager Instance { get; private set; }

    /// <summary>Fired when an ad unit finishes loading and is ready to show.</summary>
    public event Action<string> PlacementBecameReady;

    [Header("Unity Ads â€” Game IDs (Unity dashboard)")]
    [SerializeField] string androidGameId = "";
    [SerializeField] string iOSGameId = "";
    [SerializeField] bool testMode = true;

    [Header("Rewarded ad unit IDs (dashboard placements)")]
    [Tooltip("Shop / diamonds button uses this placement.")]
    [SerializeField] string androidShopRewardedAdUnitId = "Rewarded_Android";
    [SerializeField] string iOSShopRewardedAdUnitId = "Rewarded_iOS";
    [Tooltip("Revive panel uses this placement. Use the same IDs as shop if you only have one rewarded unit per platform (e.g. Rewarded_Android).")]
    [SerializeField] string androidReviveRewardedAdUnitId = "Rewarded_Android";
    [SerializeField] string iOSReviveRewardedAdUnitId = "Rewarded_iOS";

    string _gameId;
    bool _initialized;
    readonly HashSet<string> _loadedUnits = new HashSet<string>();
    readonly HashSet<string> _loadingUnits = new HashSet<string>();

    string _activeShowUnitId;
    Action<bool> _onShowFinished;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _gameId = GetPlatformGameId();
        if (string.IsNullOrEmpty(_gameId))
        {
            return;
        }

        if (!Advertisement.isInitialized)
            Advertisement.Initialize(_gameId, testMode, this);
        else
            _initialized = true;
    }

    public bool IsInitialized => _initialized && Advertisement.isInitialized;

    public bool IsPlacementReady(string adUnitId)
    {
        return !string.IsNullOrEmpty(adUnitId) && _loadedUnits.Contains(adUnitId);
    }

    /// <summary>Starts loading if not already loaded or loading.</summary>
    public void LoadPlacement(string adUnitId)
    {
        if (string.IsNullOrEmpty(adUnitId) || !IsInitialized)
            return;

        if (_loadedUnits.Contains(adUnitId))
        {
            PlacementBecameReady?.Invoke(adUnitId);
            return;
        }

        if (_loadingUnits.Contains(adUnitId))
            return;

        _loadingUnits.Add(adUnitId);
        Advertisement.Load(adUnitId, this);
    }

    /// <summary>Shows a loaded rewarded ad. Callback: true only if the user completed the video.</summary>
    public void ShowRewarded(string adUnitId, Action<bool> onUserEarnedReward)
    {
        if (string.IsNullOrEmpty(adUnitId) || !IsInitialized)
        {
            onUserEarnedReward?.Invoke(false);
            return;
        }

        if (_activeShowUnitId != null)
        {
            onUserEarnedReward?.Invoke(false);
            return;
        }

        if (!_loadedUnits.Contains(adUnitId))
        {
            onUserEarnedReward?.Invoke(false);
            return;
        }

        _loadedUnits.Remove(adUnitId);
        _activeShowUnitId = adUnitId;
        _onShowFinished = onUserEarnedReward;
        Advertisement.Show(adUnitId, this);
    }

    string GetPlatformGameId()
    {
#if UNITY_IOS
        return iOSGameId;
#elif UNITY_ANDROID
        return androidGameId;
#else
        return androidGameId;
#endif
    }

    /// <summary>Current platformâ€™s shop rewarded placement id (from this componentâ€™s Inspector).</summary>
    public string GetShopRewardedAdUnitId()
    {
#if UNITY_IOS
        return iOSShopRewardedAdUnitId;
#else
        return androidShopRewardedAdUnitId;
#endif
    }

    /// <summary>Current platformâ€™s revive rewarded placement id (from this componentâ€™s Inspector).</summary>
    public string GetReviveRewardedAdUnitId()
    {
#if UNITY_IOS
        return iOSReviveRewardedAdUnitId;
#else
        return androidReviveRewardedAdUnitId;
#endif
    }

    public void OnInitializationComplete()
    {
        _initialized = true;
        LoadPlacement(GetShopRewardedAdUnitId());
        LoadPlacement(GetReviveRewardedAdUnitId());
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        _initialized = false;
    }

    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        _loadingUnits.Remove(adUnitId);
        _loadedUnits.Add(adUnitId);
        PlacementBecameReady?.Invoke(adUnitId);
    }

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        _loadingUnits.Remove(adUnitId);
        StartCoroutine(ReloadAfterDelay(adUnitId, 2f));
    }

    IEnumerator ReloadAfterDelay(string adUnitId, float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (Instance == this && isActiveAndEnabled && IsInitialized)
            LoadPlacement(adUnitId);
    }

    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        if (adUnitId != _activeShowUnitId)
            return;

        FinishShow(adUnitId, false);
        LoadPlacement(adUnitId);
    }

    public void OnUnityAdsShowStart(string adUnitId) { }

    public void OnUnityAdsShowClick(string adUnitId) { }

    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        if (adUnitId != _activeShowUnitId)
            return;

        bool earned = showCompletionState == UnityAdsShowCompletionState.COMPLETED;
        FinishShow(adUnitId, earned);
        LoadPlacement(adUnitId);
    }

    void FinishShow(string adUnitId, bool earned)
    {
        _activeShowUnitId = null;
        var cb = _onShowFinished;
        _onShowFinished = null;
        cb?.Invoke(earned);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
