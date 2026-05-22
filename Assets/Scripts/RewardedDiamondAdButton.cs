using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shop: watch a rewarded ad to earn diamonds. Ad unit IDs come from <see cref="UnityRewardedAdsManager"/> only.
/// </summary>
public class RewardedDiamondAdButton : MonoBehaviour
{
    [Header("Reward")]
    [SerializeField] int diamondReward = 5;
    [SerializeField] ShopManager shopManager;

    [Header("Optional")]
    [SerializeField] Button targetButton;

    string _shopAdUnitId;
    UnityRewardedAdsManager _mgr;

    void Awake()
    {
        if (targetButton == null)
            targetButton = GetComponent<Button>();
        if (shopManager == null)
            shopManager = FindObjectOfType<ShopManager>();
    }

    void OnEnable()
    {
        if (targetButton != null)
            targetButton.onClick.AddListener(OnWatchAdClicked);
        StartCoroutine(BootstrapRoutine());
    }

    void OnDisable()
    {
        if (_mgr != null)
        {
            _mgr.PlacementBecameReady -= OnPlacementReady;
            _mgr = null;
        }

        if (targetButton != null)
            targetButton.onClick.RemoveListener(OnWatchAdClicked);
    }

    void OnPlacementReady(string adUnitId)
    {
        if (adUnitId != _shopAdUnitId)
            return;
        SetButtonInteractable(true);
    }

    IEnumerator BootstrapRoutine()
    {
        SetButtonInteractable(false);

        float wait = 0f;
        while (UnityRewardedAdsManager.Instance == null && wait < 5f)
        {
            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        _mgr = UnityRewardedAdsManager.Instance;
        if (_mgr == null)
        {
            yield break;
        }

        _shopAdUnitId = _mgr.GetShopRewardedAdUnitId();
        if (string.IsNullOrEmpty(_shopAdUnitId))
        {
            yield break;
        }

        _mgr.PlacementBecameReady += OnPlacementReady;

        wait = 0f;
        while (!_mgr.IsInitialized && wait < 45f)
        {
            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!_mgr.IsInitialized)
        {
            yield break;
        }

        if (_mgr.IsPlacementReady(_shopAdUnitId))
            SetButtonInteractable(true);
        else
            _mgr.LoadPlacement(_shopAdUnitId);
    }

    void SetButtonInteractable(bool value)
    {
        if (targetButton != null)
            targetButton.interactable = value;
    }

    public void OnWatchAdClicked()
    {
        var ads = UnityRewardedAdsManager.Instance;
        string id = ads != null ? ads.GetShopRewardedAdUnitId() : null;
        if (ads == null || string.IsNullOrEmpty(id) || !ads.IsPlacementReady(id))
        {
            return;
        }

        SetButtonInteractable(false);
        ads.ShowRewarded(id, OnShopAdClosed);
    }

    void OnShopAdClosed(bool earned)
    {
        if (earned && diamondReward > 0 && shopManager != null)
        {
            shopManager.AddDiamonds(diamondReward);
            shopManager.UpdateShopUI();
            var homeCurrency = FindObjectOfType<HomeScreenCurrencyDisplay>();
            if (homeCurrency != null)
                homeCurrency.RefreshCurrencyDisplay();
        }

        var ads = UnityRewardedAdsManager.Instance;
        string id = ads != null ? ads.GetShopRewardedAdUnitId() : null;
        if (ads != null && !string.IsNullOrEmpty(id) && ads.IsPlacementReady(id))
            SetButtonInteractable(true);
        else
            SetButtonInteractable(false);
    }
}
