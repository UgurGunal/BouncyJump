using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class TowerShopManager : MonoBehaviour
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
        RefreshAllTowerButtons();
    }
    
    void RefreshAllTowerButtons()
    {
        TowerBuyButton[] buttons = FindObjectsOfType<TowerBuyButton>();
        foreach (var button in buttons)
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
