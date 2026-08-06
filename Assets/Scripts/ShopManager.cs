using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("Shop UI References")]
    public GameObject shopPanel;
    public Button closeShopButton;

    [Header("Shop scroll")]
    [Tooltip("The scrollable Content RectTransform - its Anchored Position Y is what actually changes when you scroll.")]
    public RectTransform shopScrollContent;
    [Tooltip("Usually the Viewport (Content's parent). If empty, Content's parent RectTransform is used for height.")]
    public RectTransform shopScrollViewport;
    [Tooltip("Optional: same ScrollRect as in the Inspector. Used only to sync the scrollbar after we set Content Y.")]
    public ScrollRect shopScrollRect;

    [Header("Shop panel - scroll to section (while shop is open)")]
    [Tooltip("Optional: Buy Gold button inside the shop; scrolls Content over time (no shop open).")]
    public Button shopPanelBuyGoldButton;
    [Tooltip("Optional: Buy Diamond button inside the shop.")]
    public Button shopPanelBuyDiamondButton;
    [Tooltip("Target for this section (same rules as home: positive = distance down, negative = exact Content Y). Animates from wherever Content is now, not from 0.")]
    public float shopPanelBuyGoldScrollY;
    public float shopPanelBuyDiamondScrollY;
    [Tooltip("In-shop animated scroll speed (anchored Y units per second). Duration = distance / speed.")]
    public float shopPanelScrollSpeed = 4000f;

    Coroutine _animatedScrollRoutine;

    [Header("Currency Display")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI diamondText;
    
    [Header("Tower Manager Reference")]
    public TowerManager towerManager;

    void Awake()
    {
        DestroyStalePersistedShops();

        // Same-frame order with BuyGoldButton (DefaultExecutionOrder 50): scroll runs first, then purchase.
        if (shopPanelBuyGoldButton != null)
            shopPanelBuyGoldButton.onClick.AddListener(OnShopPanelBuyGoldScrollClicked);
        if (shopPanelBuyDiamondButton != null)
            shopPanelBuyDiamondButton.onClick.AddListener(OnShopPanelBuyDiamondScrollClicked);
    }

    /// <summary>
    /// Older builds DontDestroyOnLoad'd this object with IAPManager, leaving a shop with a dead panel.
    /// Remove those leftovers so the live HomeScene shop is the one buttons use.
    /// </summary>
    void DestroyStalePersistedShops()
    {
        ShopManager[] all = FindObjectsOfType<ShopManager>(true);
        for (int i = 0; i < all.Length; i++)
        {
            ShopManager other = all[i];
            if (other == null || other == this)
                continue;
            if (other.shopPanel != null)
                continue;

            // Keep the live IAP singleton if it still shares that leftover object.
            IAPManager iap = other.GetComponent<IAPManager>();
            if (iap != null && IAPManager.Instance == iap)
                Destroy(other);
            else
                Destroy(other.gameObject);
        }
    }
    
    void Start()
    {
        if (towerManager == null)
            towerManager = TowerManager.Instance;

        if (towerManager == null)
            towerManager = TowerManager.FindInLoadedScenes();
        
        // Setup close button
        if (closeShopButton != null)
        {
            closeShopButton.onClick.AddListener(CloseShop);
        }
        
        // Hide shop initially
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }

    void OnDisable()
    {
        if (_animatedScrollRoutine != null)
        {
            StopCoroutine(_animatedScrollRoutine);
            _animatedScrollRoutine = null;
        }
    }

    void OnShopPanelBuyGoldScrollClicked()
    {
        ScrollShopContentToAnchoredYAnimated(shopPanelBuyGoldScrollY, shopPanelScrollSpeed);
    }

    void OnShopPanelBuyDiamondScrollClicked()
    {
        ScrollShopContentToAnchoredYAnimated(shopPanelBuyDiamondScrollY, shopPanelScrollSpeed);
    }
    
    /// <summary>True when the shop panel GameObject is active in the hierarchy.</summary>
    public bool IsShopOpen => shopPanel != null && shopPanel.activeInHierarchy;

    /// <summary>Open the shop without changing scroll.</summary>
    public void OpenShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            UpdateCurrencyDisplay();
        }
    }

    /// <summary>Set Content scroll first, then open the panel; a short coroutine re-applies Y after layout.</summary>
    public void OpenShop(float contentAnchoredY)
    {
        TryAutoWireShopScroll();
        if (GetShopScrollContent() == null)
        {
            OpenShop();
            return;
        }

        ApplyShopContentY(contentAnchoredY);
        OpenShop();

        HomeScreenUI homeUi = FindObjectOfType<HomeScreenUI>();
        if (homeUi != null && homeUi.isActiveAndEnabled)
            homeUi.StartCoroutine(ScrollContentYAfterShopOpen(contentAnchoredY));
        else if (isActiveAndEnabled)
            StartCoroutine(ScrollContentYAfterShopOpen(contentAnchoredY));
    }

    /// <summary>Fills missing references from the first ScrollRect under the shop panel (include inactive).</summary>
    void TryAutoWireShopScroll()
    {
        if (shopPanel == null)
            return;
        if (shopScrollContent != null && shopScrollViewport != null && shopScrollRect != null)
            return;

        ScrollRect sr = shopPanel.GetComponentInChildren<ScrollRect>(true);
        if (sr == null)
            return;

        if (shopScrollRect == null)
            shopScrollRect = sr;
        if (shopScrollContent == null && sr.content != null)
            shopScrollContent = sr.content;
        if (shopScrollViewport == null && sr.viewport != null)
            shopScrollViewport = sr.viewport;
    }

    /// <summary>After the shop is active, re-apply Y once layout knows real Content/Viewport sizes.</summary>
    public IEnumerator ScrollContentYAfterShopOpen(float targetAnchoredY)
    {
        TryAutoWireShopScroll();
        for (int i = 0; i < 5; i++)
            yield return null;

        for (int attempt = 0; attempt < 12; attempt++)
        {
            RebuildShopLayout();
            if (GetShopScrollableHeight() > 0.001f || attempt >= 11)
            {
                ApplyShopContentY(targetAnchoredY);
                break;
            }
            yield return null;
        }

        yield return new WaitForEndOfFrame();
        ApplyShopContentY(targetAnchoredY);
        yield return null;
        ApplyShopContentY(targetAnchoredY);
    }

    float GetShopScrollableHeight()
    {
        RectTransform content = GetShopScrollContent();
        RectTransform viewport = GetShopViewport();
        if (content == null || viewport == null)
            return 0f;
        return Mathf.Max(0f, content.rect.height - viewport.rect.height);
    }

    RectTransform GetShopScrollContent()
    {
        if (shopScrollContent != null)
            return shopScrollContent;
        if (shopScrollRect != null && shopScrollRect.content != null)
            return shopScrollRect.content;
        return null;
    }

    RectTransform GetShopViewport()
    {
        if (shopScrollViewport != null)
            return shopScrollViewport;
        RectTransform content = GetShopScrollContent();
        if (content != null && content.parent is RectTransform parentRt)
            return parentRt;
        if (shopScrollRect != null && shopScrollRect.viewport != null)
            return shopScrollRect.viewport;
        return null;
    }

    void RebuildShopLayout()
    {
        RectTransform content = GetShopScrollContent();
        if (content == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        if (shopPanel != null)
        {
            RectTransform panelRt = shopPanel.GetComponent<RectTransform>();
            if (panelRt != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(panelRt);
        }

        Canvas.ForceUpdateCanvases();
    }

    /// <summary>Set scroll by writing Content's anchoredPosition.y (typical: 0 = top, negative = scrolled down).</summary>
    public void ScrollShopContentToAnchoredY(float targetAnchoredY)
    {
        ApplyShopContentY(targetAnchoredY);
    }

    /// <summary>Smoothly scroll Content from the current Y. Same target rules as <see cref="ScrollShopContentToAnchoredY"/>. Duration = distance / speed.</summary>
    public void ScrollShopContentToAnchoredYAnimated(float targetAnchoredY, float scrollSpeedPerSecond = 4000f)
    {
        TryAutoWireShopScroll();
        RectTransform content = GetShopScrollContent();
        if (content == null)
            return;

        if (_animatedScrollRoutine != null)
            StopCoroutine(_animatedScrollRoutine);

        RectTransform viewport = GetShopViewport();
        float scrollable = 0f;
        if (viewport != null)
            scrollable = Mathf.Max(0f, content.rect.height - viewport.rect.height);

        // ScrollRect owns scroll state; anchoredPosition.y is often wrong on the click frame â€” use normalized position (inverse of ApplyShopContentY).
        float startY = (shopScrollRect != null && scrollable > 0.0001f)
            ? scrollable * (shopScrollRect.verticalNormalizedPosition - 1f)
            : content.anchoredPosition.y;

        if (scrollSpeedPerSecond <= 0f)
        {
            ApplyShopContentY(targetAnchoredY);
            return;
        }

        if (!isActiveAndEnabled)
            return;

        // ScrollRect fights direct anchoredPosition changes â€” lock normalized pos from computed startY first.
        if (shopScrollRect != null && scrollable > 0.0001f)
        {
            shopScrollRect.StopMovement();
            shopScrollRect.verticalNormalizedPosition = Mathf.Clamp01(1f + startY / scrollable);
        }

        _animatedScrollRoutine = StartCoroutine(ScrollShopContentAnimatedRoutine(targetAnchoredY, scrollSpeedPerSecond, startY));
    }

    IEnumerator ScrollShopContentAnimatedRoutine(float targetAnchoredY, float scrollSpeedPerSecond, float startY)
    {
        try
        {
            RectTransform content = GetShopScrollContent();
            RectTransform viewport = GetShopViewport();
            if (content == null || viewport == null)
                yield break;

            float scrollable = Mathf.Max(0f, content.rect.height - viewport.rect.height);
            float endY = ComputeShopContentY(targetAnchoredY, scrollable);

            float distance = Mathf.Abs(endY - startY);
            if (distance < 0.0001f)
            {
                ApplyShopContentY(targetAnchoredY);
                yield break;
            }

            float durationSeconds = distance / scrollSpeedPerSecond;
            float elapsed = 0f;

            // Drive scroll via ScrollRect so Content is not snapped to 0 by internal layout.
            if (shopScrollRect != null && scrollable > 0.0001f)
            {
                float startNorm = Mathf.Clamp01(1f + startY / scrollable);
                float endNorm = Mathf.Clamp01(1f + endY / scrollable);

                while (elapsed < durationSeconds)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / durationSeconds);
                    float norm = Mathf.Lerp(startNorm, endNorm, t);
                    shopScrollRect.verticalNormalizedPosition = norm;
                    yield return null;
                }
            }
            else
            {
                while (elapsed < durationSeconds)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / durationSeconds);
                    float y = Mathf.Lerp(startY, endY, t);
                    Vector2 ap = content.anchoredPosition;
                    content.anchoredPosition = new Vector2(ap.x, y);
                    yield return null;
                }
            }

            ApplyShopContentY(targetAnchoredY);
        }
        finally
        {
            _animatedScrollRoutine = null;
        }
    }

    /// <summary>
    /// Maps target to Content Y. When <paramref name="scrollable"/> is unknown (~0 while panel inactive), positive targets become negative Y without clamp.
    /// </summary>
    float ComputeShopContentY(float targetAnchoredY, float scrollable)
    {
        if (scrollable < 0.0001f)
        {
            if (targetAnchoredY <= 0f)
                return targetAnchoredY;
            return -targetAnchoredY;
        }
        if (targetAnchoredY <= 0f)
            return Mathf.Clamp(targetAnchoredY, -scrollable, 0f);
        return -Mathf.Clamp(targetAnchoredY, 0f, scrollable);
    }

    void ApplyShopContentY(float targetAnchoredY)
    {
        TryAutoWireShopScroll();
        RectTransform content = GetShopScrollContent();
        RectTransform viewport = GetShopViewport();
        if (content == null || viewport == null)
            return;

        float scrollable = Mathf.Max(0f, content.rect.height - viewport.rect.height);
        float y = ComputeShopContentY(targetAnchoredY, scrollable);

        Vector2 ap = content.anchoredPosition;
        content.anchoredPosition = new Vector2(ap.x, y);

        if (shopScrollRect != null && scrollable > 0.0001f)
        {
            shopScrollRect.StopMovement();
            shopScrollRect.verticalNormalizedPosition = Mathf.Clamp01(1f + y / scrollable);
        }
    }
    
    public void CloseShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }
    
    public void UpdateShopUI()
    {
        UpdateCurrencyDisplay();
        RefreshAllItemButtons();
    }
    
    void RefreshAllItemButtons()
    {
        // Refresh tower buttons
        TowerBuyButton[] towerButtons = FindObjectsOfType<TowerBuyButton>();
        foreach (var button in towerButtons)
        {
            button.RefreshButton();
        }

        // Refresh ball buttons
        BallBuyButton[] ballButtons = FindObjectsOfType<BallBuyButton>();
        foreach (var button in ballButtons)
        {
            button.RefreshButton();
        }
    }
    
    public int GetPlayerCurrency()
    {
        GameSaveService.EnsureLoaded();
        return GameSaveService.GetGold();
    }
    
    public void AddCurrency(int amount)
    {
        AddGold(amount);
    }
    
    public int GetPlayerGold()
    {
        return GameSaveService.GetGold();
    }
    
    public int GetPlayerDiamonds()
    {
        return GetSavedDiamondBalance();
    }

    public static int GetSavedDiamondBalance()
    {
        return GameSaveService.GetDiamonds();
    }
    
    public void AddGold(int amount)
    {
        GameSaveService.AddGold(amount);
        UpdateCurrencyDisplay();
    }
    
    public void AddDiamonds(int amount)
    {
        GameSaveService.AddDiamonds(amount);
        UpdateCurrencyDisplay();
    }

    /// <summary>Spend diamonds to get gold. Returns true if player had enough diamonds.</summary>
    public bool TrySpendDiamonds(int amount)
    {
        return TrySpendSavedDiamonds(amount);
    }

    public static bool TrySpendSavedDiamonds(int amount)
    {
        if (!GameSaveService.TrySpendDiamonds(amount))
            return false;

        ShopManager shop = Object.FindObjectOfType<ShopManager>(true);
        if (shop != null)
            shop.UpdateCurrencyDisplay();

        HomeScreenCurrencyDisplay display = Object.FindObjectOfType<HomeScreenCurrencyDisplay>(true);
        if (display != null)
            display.RefreshCurrencyDisplay();

        return true;
    }

    /// <summary>Google Play / App Store diamond purchase via IAPManager.</summary>
    public static void OpenInGameDiamondPurchase()
    {
        IAPManager iap = IAPManager.EnsureExists();
        if (iap != null)
            iap.Buy(IAPManager.ProductGems50);
    }

    /// <summary>Buy gold by spending diamonds. Exchange rate is configurable.</summary>
    public bool TryBuyGoldWithDiamonds(int diamondCost, int goldReward)
    {
        if (!TrySpendDiamonds(diamondCost)) return false;
        AddGold(goldReward);
        PlayShopPurchaseSound();
        return true;
    }

    /// <summary>Called by IAPManager after a successful real-money purchase is pending confirmation.</summary>
    public void GrantDiamondsFromIAP(int amount)
    {
        AddDiamonds(amount);
        PlayShopPurchaseSound();
        if (CurrencyFlyFeedback.Instance != null)
            CurrencyFlyFeedback.Instance.PlayDiamonds(amount);
        else
            UpdateCurrencyDisplay();
    }

    /// <summary>Deprecated mock path — routes to real IAP for the matching pack size.</summary>
    public void MockPurchaseDiamondsWithRealMoney(int amount)
    {
        IAPManager iap = IAPManager.EnsureExists();
        if (iap == null)
            return;

        string productId = null;
        if (amount == 50) productId = IAPManager.ProductGems50;
        else if (amount == 300) productId = IAPManager.ProductGems300;
        else if (amount == 2000) productId = IAPManager.ProductGems2000;

        if (productId != null)
            iap.Buy(productId);
        else
            Debug.LogWarning($"[ShopManager] No IAP product mapped for {amount} diamonds.");
    }

    /// <summary>Mock: grant diamonds after watching an ad. Replace with real rewarded ad later.</summary>
    public void MockGrantDiamondsFromAd(int amount)
    {
        AddDiamonds(amount);
    }

    static void PlayShopPurchaseSound()
    {
        if (SoundEffectsManager.Instance != null)
            SoundEffectsManager.Instance.PlayShopPurchaseSound();
    }

    void UpdateCurrencyDisplay()
    {
        CurrencyFlyFeedback fly = CurrencyFlyFeedback.Instance;

        if (goldText != null)
        {
            int gold;
            if (fly != null && fly.TryGetDisplayedGold(out gold))
                goldText.text = FormatCurrency(gold);
            else
                goldText.text = FormatCurrency(GetPlayerGold());
        }

        if (diamondText != null)
        {
            int diamonds;
            if (fly != null && fly.TryGetDisplayedDiamonds(out diamonds))
                diamondText.text = FormatCurrency(diamonds);
            else
                diamondText.text = FormatCurrency(GetPlayerDiamonds());
        }
    }
    
    // Method to format currency with commas
    string FormatCurrency(int amount)
    {
        // Use specific culture to ensure commas (not periods)
        return amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
    }
}
