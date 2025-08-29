using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class TowerShopManager : MonoBehaviour
{
    [Header("Shop UI References")]
    public GameObject shopPanel;
    public Transform towerShopContainer;
    public GameObject towerShopItemPrefab;
    public Button closeShopButton;
    
    [Header("Currency Display")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI diamondText;
    
    [Header("Tower Manager Reference")]
    public SimpleTowerManager simpleTowerManager;
    
    [Header("Shop Settings")]
    public List<TowerShopItem> availableTowers = new List<TowerShopItem>();
    
    private List<GameObject> shopItems = new List<GameObject>();
    
    void Start()
    {
        // Get reference to simple tower manager
        if (simpleTowerManager == null)
        {
            simpleTowerManager = SimpleTowerManager.Instance;
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
            InitializeShop();
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
    
    void InitializeShop()
    {
        // Clear existing shop items
        foreach (var item in shopItems)
        {
            if (item != null)
                Destroy(item);
        }
        shopItems.Clear();
        
        // Create shop items for each available tower
        foreach (var towerItem in availableTowers)
        {
            CreateShopItem(towerItem);
        }
    }
    
    void CreateShopItem(TowerShopItem towerItem)
    {
        // Since using manual shop buttons, this method is no longer needed
        // Shop items are manually created and use ManualTowerBuyButton script
        Debug.Log($"CreateShopItem called for {towerItem.towerName} - but using manual shop setup");
    }
    
    public void PurchaseTower(TowerShopItem towerItem)
    {
        if (towerItem == null || simpleTowerManager == null) return;
        
        // Find the corresponding SimpleTower to get pricing
        SimpleTower tower = null;
        for (int i = 0; i < simpleTowerManager.allTowers.Length; i++)
        {
            if (simpleTowerManager.allTowers[i].towerName == towerItem.towerName)
            {
                tower = simpleTowerManager.allTowers[i];
                break;
            }
        }
        
        if (tower == null)
        {
            Debug.LogError($"Tower {towerItem.towerName} not found in SimpleTowerManager!");
            return;
        }
        
        // Check if player has enough currency
        int currentGold = PlayerPrefs.GetInt("PlayerGold", 0);
        int currentDiamonds = PlayerPrefs.GetInt("PlayerDiamonds", 0);
        
        if (currentGold >= tower.goldPrice && currentDiamonds >= tower.diamondPrice)
        {
            // Use SimpleTowerManager's purchase method which handles currency deduction
            simpleTowerManager.PurchaseTower(towerItem.towerName);
            
            // Update UI
            UpdateShopUI();
            
            Debug.Log($"Purchased {towerItem.towerName} for {tower.goldPrice} gold and {tower.diamondPrice} diamonds!");
        }
        else
        {
            Debug.Log($"Not enough currency! Need {tower.goldPrice} gold and {tower.diamondPrice} diamonds");
        }
    }
    
    public void SelectTower(TowerShopItem towerItem)
    {
        if (towerItem == null) return;
        
        // Check if tower is purchased
        if (IsTowerPurchased(towerItem))
        {
            // Set as current tower
            if (simpleTowerManager != null)
            {
                simpleTowerManager.SetCurrentTower(towerItem.towerName);
                
                // Update shop UI to reflect the new selection
                UpdateShopUI();
                
                Debug.Log($"Selected tower: {towerItem.towerName}");
            }
        }
        else
        {
            Debug.Log("You need to purchase this tower first!");
        }
    }
    
    public bool IsTowerPurchased(TowerShopItem towerItem)
    {
        if (simpleTowerManager != null)
        {
            return simpleTowerManager.IsTowerPurchased(towerItem.towerName);
        }
        return false;
    }
    
    public void UpdateShopUI()
    {
        // Since using manual shop buttons with ManualTowerBuyButton script,
        // those buttons update themselves automatically
        
        // Update currency display
        UpdateCurrencyDisplay();
        
        // Refresh all ManualTowerBuyButton scripts in the scene
        RefreshAllManualTowerButtons();
    }
    
    void RefreshAllManualTowerButtons()
    {
        // Find all ManualTowerBuyButton scripts and refresh them
        ManualTowerBuyButton[] manualButtons = FindObjectsOfType<ManualTowerBuyButton>();
        foreach (var button in manualButtons)
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

[System.Serializable]
public class TowerShopItem
{
    [Header("Tower Information")]
    public string towerName = "New Tower";
    public string description = "A new tower to play with";
    
    [Header("Shop Settings")]
    public int price = 1000;
    public Sprite towerShopImage; // Complete shop image with tower details
    public Sprite buyButtonImage; // Button image showing cost
    public bool isUnlockedByDefault = false;
}
