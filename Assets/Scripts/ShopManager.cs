using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("Shop UI References")]
    public GameObject shopPanel;
    public Button closeShopButton;
    
    [Header("Currency Display")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI diamondText;
    
    [Header("Tower Manager Reference")]
    public TowerManager towerManager;
    
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
    
    public void OpenShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            UpdateCurrencyDisplay();
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
        Debug.Log($"Bought {goldReward} gold for {diamondCost} diamonds.");
        return true;
    }

    /// <summary>Mock: purchase diamonds with real money (IAP). Replace with real IAP later.</summary>
    public void MockPurchaseDiamondsWithRealMoney(int amount)
    {
        AddDiamonds(amount);
        Debug.Log($"[MOCK IAP] Purchased {amount} diamonds with real money.");
    }

    /// <summary>Mock: grant diamonds after watching an ad. Replace with real rewarded ad later.</summary>
    public void MockGrantDiamondsFromAd(int amount)
    {
        AddDiamonds(amount);
        Debug.Log($"[MOCK AD] Granted {amount} diamonds from watching ad.");
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
