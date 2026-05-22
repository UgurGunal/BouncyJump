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
    [Tooltip("The scrollable Content RectTransform â€” its Anchored Position Y is what actually changes when you scroll.")]
    public RectTransform shopScrollContent;
    [Tooltip("Usually the Viewport (Content's parent). If empty, Content's parent RectTransform is used for height.")]
    public RectTransform shopScrollViewport;
    [Tooltip("Optional: same ScrollRect as in the Inspector. Used only to sync the scrollbar after we set Content Y.")]
    public ScrollRect shopScrollRect;

    [Header("Shop panel â€” scroll to section (while shop is open)")]
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
        // Same-frame order with BuyGoldButton (DefaultExecutionOrder 50): scroll runs first, then purchase.
        if (shopPanelBuyGoldButton != null)
            shopPanelBuyGoldButton.onClick.AddListener(OnShopPanelBuyGoldScrollClicked);
        if (shopPanelBuyDiamondButton != null)
            shopPanelBuyDiamondButton.onClick.AddListener(OnShopPanelBuyDiamondScrollClicked);
    }
    
    void Start()
    {
        if (towerManager == null)
        {
            towerManager = TowerManager.Instance;
        }
        
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

        // Refresh buy-gold buttons (enable/disable based on diamond count)
        BuyGoldButton[] goldButtons = FindObjectsOfType<BuyGoldButton>();
        foreach (var button in goldButtons)
        {
            button.RefreshButton();
        }
    }
    
    public int GetPlayerCurrency()
    {
        return PlayerPrefs.GetInt("PlayerCurrency", 0);
    }
    
    public void AddCurrency(int amount)
    {
        int currentCurrency = GetPlayerCurrency();
        PlayerPrefs.SetInt("PlayerCurrency", currentCurrency + amount);
        PlayerPrefs.Save();
    }
    
    public int GetPlayerGold()
    {
        return PlayerPrefs.GetInt("PlayerGold", 0);
    }
    
    public int GetPlayerDiamonds()
    {
        return PlayerPrefs.GetInt("PlayerDiamonds", 0);
    }
    
    public void AddGold(int amount)
    {
        int currentGold = GetPlayerGold();
        PlayerPrefs.SetInt("PlayerGold", currentGold + amount);
        PlayerPrefs.Save();
        UpdateCurrencyDisplay();
    }
    
    public void AddDiamonds(int amount)
    {
        int currentDiamonds = GetPlayerDiamonds();
        PlayerPrefs.SetInt("PlayerDiamonds", currentDiamonds + amount);
        PlayerPrefs.Save();
        UpdateCurrencyDisplay();
    }

    /// <summary>Spend diamonds to get gold. Returns true if player had enough diamonds.</summary>
    public bool TrySpendDiamonds(int amount)
    {
        int current = GetPlayerDiamonds();
        if (current < amount) return false;
        PlayerPrefs.SetInt("PlayerDiamonds", current - amount);
        PlayerPrefs.Save();
        UpdateCurrencyDisplay();
        return true;
    }

    /// <summary>Buy gold by spending diamonds. Exchange rate is configurable.</summary>
    public bool TryBuyGoldWithDiamonds(int diamondCost, int goldReward)
    {
        if (!TrySpendDiamonds(diamondCost)) return false;
        AddGold(goldReward);
        return true;
    }

    /// <summary>Mock: purchase diamonds with real money (IAP). Replace with real IAP later.</summary>
    public void MockPurchaseDiamondsWithRealMoney(int amount)
    {
        AddDiamonds(amount);
    }

    /// <summary>Mock: grant diamonds after watching an ad. Replace with real rewarded ad later.</summary>
    public void MockGrantDiamondsFromAd(int amount)
    {
        AddDiamonds(amount);
    }

    void UpdateCurrencyDisplay()
    {
        if (goldText != null)
        {
            goldText.text = FormatCurrency(GetPlayerGold());
        }
        
        if (diamondText != null)
        {
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
