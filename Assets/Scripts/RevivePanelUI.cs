using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class RevivePanelUI : MonoBehaviour
{
    public static RevivePanelUI Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject panelObject;
    public GameObject contentContainer; // The content to animate (excludes background)
    public Button pay3DiamondButton; // Pay 3 diamonds to revive
    public Button watchAdButton; // Watch ad to revive
    public Button quitButton; // Quit to game end panel
    public Slider countdownSlider;
    public TextMeshProUGUI countdownText; // Text to display countdown seconds

    [Header("Revive Settings")]
    public int diamondsToRevive = 3;
    public float reviveCountdownDuration = 10f;
    public float reviveYOffset = -0.6f;
    [Min(0)]
    [Tooltip("Maximum successful revives allowed before the next run restart.")]
    public int maxRevivesPerSession = 2;

    private PlayerBallController _playerController;
    private CameraFollow _cameraFollow;
    private float _currentCountdownTime;
    private bool _countdownPausedForAd;
    private UnityRewardedAdsManager _adsEventsSource;
    private int _revivesUsedThisSession;
    /// <summary>Bumped to cancel an in-flight watch-ad wait when the panel closes.</summary>
    private int _watchAdRequestId;

    public int RevivesUsedThisSession => _revivesUsedThisSession;
    public int RevivesRemaining => Mathf.Max(0, maxRevivesPerSession - _revivesUsedThisSession);

    /// <summary>No Inspector reference needed: <see cref="UnityRewardedAdsManager"/> registers itself as <c>Instance</c> in Awake (singleton).</summary>
    static UnityRewardedAdsManager Ads => UnityRewardedAdsManager.Instance;

    void Awake()
    {
        if (panelObject != null)
            panelObject.SetActive(false);
    }

    static string RevivePlacementId()
    {
        var mgr = Ads;
        return mgr != null ? mgr.GetReviveRewardedAdUnitId() : "";
    }

    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (pay3DiamondButton != null)
            pay3DiamondButton.onClick.AddListener(OnPay3DiamondClick);
        if (watchAdButton != null)
            watchAdButton.onClick.AddListener(OnWatchAdClick);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClick);

        EnsureRewardedAdsEventsSubscribed();

        _playerController = FindObjectOfType<PlayerBallController>();
        _cameraFollow = FindObjectOfType<CameraFollow>();
    }

    public void ShowRevivePanel()
    {
        if (!CanReviveThisSession())
        {
            ShowGameEndPanel();
            return;
        }

        if (panelObject == null)
            return;

        panelObject.SetActive(true);
        if (contentContainer != null)
            contentContainer.transform.localScale = Vector3.zero;
        StartCoroutine(ScaleAnimation());
        
        _currentCountdownTime = reviveCountdownDuration;
        _countdownPausedForAd = false;
        PrepareReviveRewardedAd();
        RefreshWatchAdButtonState();
        StartCoroutine(CountdownCoroutine());
        StartCoroutine(EnsureWatchAdReadyWhilePanelOpenRoutine());
    }

    /// <summary>Keeps requesting a revive ad the whole time the panel is open.</summary>
    IEnumerator EnsureWatchAdReadyWhilePanelOpenRoutine()
    {
        float wait = 0f;
        while (Ads == null && wait < 20f && panelObject != null && panelObject.activeInHierarchy)
        {
            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        if (Ads == null)
        {
            RefreshWatchAdButtonState();
            yield break;
        }

        EnsureRewardedAdsEventsSubscribed();

        wait = 0f;
        while (!Ads.IsInitialized && wait < 45f && panelObject.activeInHierarchy)
        {
            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        string placement = RevivePlacementId();
        if (string.IsNullOrEmpty(placement))
        {
            RefreshWatchAdButtonState();
            yield break;
        }

        // Keep warming until the panel closes — do not give up after a fixed timeout.
        while (panelObject != null && panelObject.activeInHierarchy)
        {
            if (Ads != null && Ads.IsInitialized && !Ads.IsPlacementReady(placement))
                Ads.LoadPlacement(placement);

            RefreshWatchAdButtonState();
            yield return new WaitForSecondsRealtime(1f);
        }
    }

    void EnsureRewardedAdsEventsSubscribed()
    {
        var mgr = Ads;
        if (mgr == null)
            return;
        if (_adsEventsSource == mgr)
            return;
        if (_adsEventsSource != null)
            _adsEventsSource.PlacementBecameReady -= OnRewardedPlacementReady;
        _adsEventsSource = mgr;
        _adsEventsSource.PlacementBecameReady += OnRewardedPlacementReady;
    }

    void OnDestroy()
    {
        if (_adsEventsSource != null)
        {
            _adsEventsSource.PlacementBecameReady -= OnRewardedPlacementReady;
            _adsEventsSource = null;
        }
    }

    void OnRewardedPlacementReady(string adUnitId)
    {
        if (adUnitId != RevivePlacementId())
            return;
        RefreshWatchAdButtonState();
    }

    void PrepareReviveRewardedAd()
    {
        EnsureRewardedAdsEventsSubscribed();
        var mgr = Ads;
        if (mgr == null || !mgr.IsInitialized)
            return;
        string placement = RevivePlacementId();
        if (string.IsNullOrEmpty(placement))
            return;
        mgr.LoadPlacement(placement);
    }

    void RefreshWatchAdButtonState()
    {
        if (watchAdButton == null)
            return;
        // Always keep the watch-ad option clickable while the panel is open.
        // Only lock it while an ad is loading/showing for this click.
        watchAdButton.interactable = !_countdownPausedForAd;
    }

    private IEnumerator ScaleAnimation()
    {
        if (contentContainer == null)
            yield break;

        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            
            // Smooth ease-out curve (starts fast, slows down at the end)
            float smoothProgress = 1f - Mathf.Pow(1f - progress, 3f);
            
            contentContainer.transform.localScale = Vector3.Lerp(startScale, endScale, smoothProgress);
            yield return null;
        }
        
        contentContainer.transform.localScale = endScale;
    }

    void HideRevivePanel()
    {
        _watchAdRequestId++;
        _countdownPausedForAd = false;
        if (panelObject != null)
            panelObject.SetActive(false);
        StopAllCoroutines();
        // Warm the next revive ad as soon as this panel closes (after a successful revive or quit).
        PrepareReviveRewardedAd();
    }

    void OnPay3DiamondClick()
    {
        if (!CanReviveThisSession())
        {
            ShowGameEndPanel();
            return;
        }

        if (ShopManager.TrySpendSavedDiamonds(diamondsToRevive))
        {
            BeginRevive();
            return;
        }

        ShopManager.OpenInGameDiamondPurchase();
    }

    IEnumerator CountdownCoroutine()
    {
        while (_currentCountdownTime > 0)
        {
            while (_countdownPausedForAd)
                yield return null;

            _currentCountdownTime -= Time.unscaledDeltaTime;
            if (countdownSlider != null)
            { 
                countdownSlider.value = _currentCountdownTime / reviveCountdownDuration;
            }
            yield return null;
        }
        OnCountdownFinished();
    }

    void OnWatchAdClick()
    {
        if (_countdownPausedForAd)
            return;

        var mgr = Ads;
        string revivePlacement = RevivePlacementId();
        if (mgr == null || string.IsNullOrEmpty(revivePlacement) || !mgr.IsInitialized)
        {
            Debug.LogWarning("Revive ad cannot start because Unity Ads is not initialized.");
            return;
        }

        int requestId = ++_watchAdRequestId;
        _countdownPausedForAd = true;
        RefreshWatchAdButtonState();

        if (mgr.IsPlacementReady(revivePlacement))
        {
            mgr.ShowRewarded(revivePlacement, OnReviveRewardedAdFinished);
            return;
        }

        // A new or temporarily unfilled revive placement must not block reviving.
        // Prefer the dedicated placement, but use the already-cached shop ad as a fallback.
        string shopFallbackPlacement = mgr.GetShopRewardedAdUnitId();
        if (!string.IsNullOrEmpty(shopFallbackPlacement) &&
            mgr.IsPlacementReady(shopFallbackPlacement))
        {
            Debug.LogWarning(
                $"Revive ad '{revivePlacement}' is not ready; using fallback '{shopFallbackPlacement}'.");
            mgr.ShowRewarded(shopFallbackPlacement, OnReviveRewardedAdFinished);
            return;
        }

        mgr.ShowRewardedWhenReady(
            revivePlacement,
            OnReviveRewardedAdFinished,
            8f,
            () => requestId != _watchAdRequestId);
    }

    void OnReviveRewardedAdFinished(bool userEarnedReward)
    {
        _countdownPausedForAd = false;

        if (panelObject == null || !panelObject.activeInHierarchy)
        {
            PrepareReviveRewardedAd();
            return;
        }

        RefreshWatchAdButtonState();

        if (userEarnedReward)
        {
            if (CanReviveThisSession())
                BeginRevive();
            else
                ShowGameEndPanel();
            return;
        }

        // Ad failed / no fill — keep option open and immediately request another.
        PrepareReviveRewardedAd();
    }

    void OnQuitClick()
    {
        ShowGameEndPanel();
    }

    bool CanReviveThisSession()
    {
        return _revivesUsedThisSession < Mathf.Max(0, maxRevivesPerSession);
    }

    void BeginRevive()
    {
        if (!CanReviveThisSession())
        {
            ShowGameEndPanel();
            return;
        }

        _revivesUsedThisSession++;
        HideRevivePanel();
        StartCoroutine(ReviveCountdown());
    }

    void ShowGameEndPanel()
    {
        HideRevivePanel();
        if (GameEndPanelUI.Instance != null)
        {
            GameEndPanelUI.Instance.ShowGameEndPanel();
        }
        else
        {
            // Reset the persistent loader flag since we're leaving the game
            PersistentLoader.ResetForRestart();
            
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("HomeScene");
        }
    }

    public void ResetSessionReviveCount()
    {
        _revivesUsedThisSession = 0;
    }

    void OnCountdownFinished()
    {
        OnQuitClick();
    }

    void RevivePlayer()
    {
        if (_playerController != null)
        {
            float reviveY = Camera.main.transform.position.y + reviveYOffset;
            Vector3 newPosition = new Vector3(Camera.main.transform.position.x, reviveY, 0);
            _playerController.Revive(newPosition);
            
            // Resume the session to continue tracking stats after revive
            if (PointsManager.Instance != null)
            {
                PointsManager.Instance.ResumeSession();
            }
        }
        else
        {
            OnQuitClick();
        }
    }

    IEnumerator ReviveCountdown()
    {
        RevivePlayer();

        int countdown = 3;
        
        // Show countdown text if available
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }
        
        while (countdown > 0)
        {
            // Update countdown text
            if (countdownText != null)
            {
                countdownText.text = countdown.ToString();
            }
            
            yield return new WaitForSecondsRealtime(1f);
            countdown--;
        }
        
        // Hide countdown text when finished
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }

        Time.timeScale = 1f;

        if (_cameraFollow != null)
            _cameraFollow.ResetRestartTrigger();

        if (MusicManager.Instance != null)
            MusicManager.Instance.ResumeMusic();

        if (PausePanelUI.Instance != null)
            PausePanelUI.Instance.SetPauseOpenAllowed(true);
    }
}
