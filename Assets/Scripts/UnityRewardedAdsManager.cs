using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;
#if UNITY_IOS && !UNITY_EDITOR
using Unity.Advertisement.IosSupport;
#endif

/// <summary>
/// One place for Unity Ads init and rewarded Load/Show. Use different ad unit IDs in the dashboard
/// for shop diamonds vs revive (or reuse the same placement if you prefer).
/// Add this to your first-loaded scene (e.g. Home); it persists with DontDestroyOnLoad.
/// On iOS, App Tracking Transparency is requested before Advertisement.Initialize.
/// </summary>
public class UnityRewardedAdsManager : MonoBehaviour,
    IUnityAdsInitializationListener,
    IUnityAdsLoadListener,
    IUnityAdsShowListener
{
    public static UnityRewardedAdsManager Instance { get; private set; }

    /// <summary>Fired when an ad unit finishes loading and is ready to show.</summary>
    public event Action<string> PlacementBecameReady;

    [Header("Unity Ads - Game IDs (Unity dashboard)")]
    [SerializeField] string androidGameId = "";
    [SerializeField] string iOSGameId = "";
    [SerializeField] bool testMode = true;

    [Header("Rewarded ad unit IDs (dashboard placements)")]
    [Tooltip("Shop / diamonds button uses this placement.")]
    [SerializeField] string androidShopRewardedAdUnitId = "Rewarded_Shop_Android";
    [SerializeField] string iOSShopRewardedAdUnitId = "Rewarded_Shop_IOS";
    [Tooltip("Revive panel placement — keep separate from shop so they do not share one loaded ad.")]
    [SerializeField] string androidReviveRewardedAdUnitId = "Rewarded_Revive_Android";
    [SerializeField] string iOSReviveRewardedAdUnitId = "Rewarded_Revive_IOS";

    [Header("Revive preload")]
    [Tooltip("How often to re-request a revive ad when none is ready.")]
    [SerializeField] float reviveWarmRetrySeconds = 3f;

    string _gameId;
    bool _initialized;
    readonly HashSet<string> _loadedUnits = new HashSet<string>();
    readonly HashSet<string> _loadingUnits = new HashSet<string>();
    readonly HashSet<string> _reportedLoadFailures = new HashSet<string>();

    string _activeShowUnitId;
    Action<bool> _onShowFinished;
    bool _showWhenReadyRunning;

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

        if (Advertisement.isInitialized)
        {
            _initialized = true;
            return;
        }

        StartCoroutine(RequestTrackingThenInitializeAds());
    }

    /// <summary>
    /// iOS: show ATT on first launch (when status is NotDetermined), then init Unity Ads.
    /// Android / Editor: init ads immediately.
    /// </summary>
    IEnumerator RequestTrackingThenInitializeAds()
    {
#if UNITY_IOS && !UNITY_EDITOR
        // ATT only appears while the app is active / focused.
        yield return new WaitUntil(() => Application.isFocused);
        yield return null;

        var status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
        if (status == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
        {
            ATTrackingStatusBinding.RequestAuthorizationTracking();
            // Wait until the user answers (or Settings already set a value).
            yield return new WaitUntil(() =>
                ATTrackingStatusBinding.GetAuthorizationTrackingStatus()
                != ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED);
        }

        Debug.Log(
            $"ATT status before Unity Ads init: {ATTrackingStatusBinding.GetAuthorizationTrackingStatus()}");
#else
        yield return null;
#endif

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

    /// <summary>
    /// Loads if needed, waits up to <paramref name="timeoutSeconds"/>, then shows.
    /// Use for revive so the button can stay clickable before the ad is cached.
    /// </summary>
    public void ShowRewardedWhenReady(
        string adUnitId,
        Action<bool> onUserEarnedReward,
        float timeoutSeconds = 20f,
        Func<bool> isCancelled = null)
    {
        if (_showWhenReadyRunning || _activeShowUnitId != null)
        {
            onUserEarnedReward?.Invoke(false);
            return;
        }

        StartCoroutine(ShowRewardedWhenReadyRoutine(adUnitId, onUserEarnedReward, timeoutSeconds, isCancelled));
    }

    IEnumerator ShowRewardedWhenReadyRoutine(
        string adUnitId,
        Action<bool> onUserEarnedReward,
        float timeoutSeconds,
        Func<bool> isCancelled)
    {
        _showWhenReadyRunning = true;
        try
        {
            if (string.IsNullOrEmpty(adUnitId) || !IsInitialized)
            {
                onUserEarnedReward?.Invoke(false);
                yield break;
            }

            float wait = 0f;
            LoadPlacement(adUnitId);
            while (!IsPlacementReady(adUnitId) && wait < timeoutSeconds)
            {
                if (isCancelled != null && isCancelled())
                {
                    onUserEarnedReward?.Invoke(false);
                    yield break;
                }

                if (!_loadingUnits.Contains(adUnitId) && !_loadedUnits.Contains(adUnitId))
                    LoadPlacement(adUnitId);

                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            if (isCancelled != null && isCancelled())
            {
                onUserEarnedReward?.Invoke(false);
                yield break;
            }

            if (!IsPlacementReady(adUnitId))
            {
                onUserEarnedReward?.Invoke(false);
                yield break;
            }

            ShowRewarded(adUnitId, onUserEarnedReward);
        }
        finally
        {
            _showWhenReadyRunning = false;
        }
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
        Debug.Log($"Unity Ads initialized. Game ID: {_gameId}");
        LoadPlacement(GetShopRewardedAdUnitId());
        LoadPlacement(GetReviveRewardedAdUnitId());
        StartCoroutine(KeepRevivePlacementWarmRoutine());
    }

    /// <summary>Keeps a revive ad cached whenever possible so death UI can show one quickly.</summary>
    IEnumerator KeepRevivePlacementWarmRoutine()
    {
        float interval = Mathf.Max(1f, reviveWarmRetrySeconds);
        while (Instance == this)
        {
            yield return new WaitForSecondsRealtime(interval);
            if (!IsInitialized)
                continue;

            string reviveId = GetReviveRewardedAdUnitId();
            if (string.IsNullOrEmpty(reviveId))
                continue;
            if (_activeShowUnitId == reviveId)
                continue;
            if (IsPlacementReady(reviveId) || _loadingUnits.Contains(reviveId))
                continue;

            LoadPlacement(reviveId);
        }
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        _initialized = false;
        Debug.LogError($"Unity Ads initialization failed: {error} - {message}");
    }

    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        _loadingUnits.Remove(adUnitId);
        _reportedLoadFailures.Remove(adUnitId);
        _loadedUnits.Add(adUnitId);
        Debug.Log($"Unity Ads loaded ad unit: {adUnitId}");
        PlacementBecameReady?.Invoke(adUnitId);
    }

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        _loadingUnits.Remove(adUnitId);
        if (_reportedLoadFailures.Add(adUnitId))
            Debug.LogWarning($"Unity Ads failed to load '{adUnitId}': {error} - {message}");
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

        Debug.LogWarning($"Unity Ads failed to show '{adUnitId}': {error} - {message}");
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
