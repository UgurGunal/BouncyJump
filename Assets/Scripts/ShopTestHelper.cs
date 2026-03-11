using UnityEngine;
using UnityEngine.UI;

public class ShopTestHelper : MonoBehaviour
{
    [Header("Test Controls")]
    public Button addGoldButton;
    public Button addDiamondsButton;
    public Button resetAllButton;
    public Button unlockAllTowersButton;
    
    [Header("Test Settings")]
    public int goldToAdd = 1000;
    public int diamondsToAdd = 100;
    
    private TowerShopManager shopManager;
    
    void Start()
    {
        // Find shop manager
        shopManager = FindObjectOfType<TowerShopManager>();
        
        // Setup test buttons
        if (addGoldButton != null)
            addGoldButton.onClick.AddListener(AddTestGold);
            
        if (addDiamondsButton != null)
            addDiamondsButton.onClick.AddListener(AddTestDiamonds);
            
        if (resetAllButton != null)
            resetAllButton.onClick.AddListener(ResetAllData);
            
        if (unlockAllTowersButton != null)
            unlockAllTowersButton.onClick.AddListener(UnlockAllTowers);
    }
    
    public void AddTestGold()
    {
        if (shopManager != null)
        {
            shopManager.AddGold(goldToAdd);
            Debug.Log($"Added {goldToAdd} gold. Total: {shopManager.GetPlayerGold()}");
        }
    }
    
    public void AddTestDiamonds()
    {
        if (shopManager != null)
        {
            shopManager.AddDiamonds(diamondsToAdd);
            Debug.Log($"Added {diamondsToAdd} diamonds. Total: {shopManager.GetPlayerDiamonds()}");
        }
    }
    
    public void ResetAllData()
    {
        // Clear all tower purchases using the index-based system
        TowerManager towerManager = TowerManager.Instance;
        if (towerManager != null && towerManager.allTowers != null)
        {
            for (int i = 0; i < towerManager.allTowers.Length; i++)
            {
                PlayerPrefs.DeleteKey($"TowerPurchased_{i}");
            }
        }
        
        // Reset currency
        PlayerPrefs.DeleteKey("PlayerGold");
        PlayerPrefs.DeleteKey("PlayerDiamonds");
        PlayerPrefs.DeleteKey("PlayerCurrency"); // Keep for backwards compatibility
        
        // Reset selected tower
        PlayerPrefs.DeleteKey("CurrentTowerIndex");
        PlayerPrefs.DeleteKey("SelectedTower"); // Legacy key
        
        PlayerPrefs.Save();
        
        Debug.Log("All data reset!");
        
        // Refresh UI
        if (shopManager != null)
        {
            shopManager.UpdateShopUI();
        }
        
        // TowerSelectionManager no longer exists in simplified system
        // Shop UI refresh is sufficient
    }
    
    public void UnlockAllTowers()
    {
        // Buy all towers using the new system
        TowerManager towerManager = TowerManager.Instance;
        if (towerManager != null && towerManager.allTowers != null)
        {
            for (int i = 0; i < towerManager.allTowers.Length; i++)
            {
                // Mark as purchased without deducting currency (for testing)
                PlayerPrefs.SetInt($"TowerPurchased_{i}", 1);
            }
            PlayerPrefs.Save();
            towerManager.RefreshTowersBought();
        }
        
        Debug.Log("All towers unlocked!");
        
        // Refresh UI
        if (shopManager != null)
        {
            shopManager.UpdateShopUI();
        }
        
        // TowerSelectionManager no longer exists in simplified system
        // Shop UI refresh is sufficient
    }
}
