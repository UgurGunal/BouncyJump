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

    private PlayerBallController _playerController;
    private CameraFollow _cameraFollow;
    private float _currentCountdownTime;
    private bool _countdownPausedForAd;
    private UnityRewardedAdsManager _adsEventsSource;

    /// <summary>No Inspector reference needed: <see cref="UnityRewardedAdsManager"/> registers itself as <c>Instance</c> in Awake (singleton).</summary>
    static UnityRewardedAdsManager Ads => UnityRewardedAdsManager.Instance;

    void Awake()
    {
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
        panelObject.SetActive(true);
        contentContainer.transform.localScale = Vector3.zero;
        StartCoroutine(ScaleAnimation());
        
        _currentCountdownTime = reviveCountdownDuration;
        _countdownPausedForAd = false;
        UpdateDiamondButtonState();
        PrepareReviveRewardedAd();
        RefreshWatchAdButtonState();
        StartCoroutine(CountdownCoroutine());
        StartCoroutine(EnsureWatchAdReadyWhilePanelOpenRoutine());
    }

    /// <summary>Waits for singleton manager, init, and ad load â€” fixes inactive button when init or load finishes after the panel opens.</summary>
    IEnumerator EnsureWatchAdReadyWhilePanelOpenRoutine()
    {
        float wait = 0f;
        while (Ads == null && wait < 20f)
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

        if (!Ads.IsInitialized)
        {
            RefreshWatchAdButtonState();
            yield break;
        }

        string placement = RevivePlacementId();
        if (string.IsNullOrEmpty(placement))
        {
            RefreshWatchAdButtonState();
            yield break;
        }

        Ads.LoadPlacement(placement);

        wait = 0f;
        while (!Ads.IsPlacementReady(placement) && wait < 30f && panelObject.activeInHierarchy)
        {
            wait += Time.unscaledDeltaTime;
            RefreshWatchAdButtonState();
            yield return null;
        }

        RefreshWatchAdButtonState();
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
        var mgr = Ads;
        string placement = RevivePlacementId();
        bool ready = mgr != null && !string.IsNullOrEmpty(placement) && mgr.IsPlacementReady(placement);
        watchAdButton.interactable = ready && !_countdownPausedForAd;
    }

    private IEnumerator ScaleAnimation()
    {
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
        _countdownPausedForAd = false;
        panelObject.SetActive(false);
        StopAllCoroutines();
    }

    void UpdateDiamondButtonState()
    {
        if (pay3DiamondButton != null)
        {
            bool canAfford = PointsManager.Instance.GemsCollected >= diamondsToRevive;
            pay3DiamondButton.interactable = canAfford;
        }
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

    void OnPay3DiamondClick()
    {
        if (PointsManager.Instance != null && PointsManager.Instance.GemsCollected >= diamondsToRevive)
        {
            PointsManager.Instance.AddGem(-diamondsToRevive);
            HideRevivePanel();
            StartCoroutine(ReviveCountdown());
        }
    }

    void OnWatchAdClick()
    {
        var mgr = Ads;
        string placement = RevivePlacementId();
        if (mgr == null || string.IsNullOrEmpty(placement) || !mgr.IsPlacementReady(placement))
        {
            return;
        }

        _countdownPausedForAd = true;
        RefreshWatchAdButtonState();

        mgr.ShowRewarded(placement, OnReviveRewardedAdFinished);
    }

    void OnReviveRewardedAdFinished(bool userEarnedReward)
    {
        _countdownPausedForAd = false;
        UpdateDiamondButtonState();
        RefreshWatchAdButtonState();

        if (userEarnedReward)
        {
            HideRevivePanel();
            StartCoroutine(ReviveCountdown());
        }
    }

    void OnQuitClick()
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
            
            // Resume the music from where it stopped when player revives
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.ResumeMusic();
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
        {
            _cameraFollow.ResetRestartTrigger();
        }
    }
}
