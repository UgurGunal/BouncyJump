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
    
    private ShopManager shopManager;
    
    void Start()
    {
        shopManager = FindObjectOfType<ShopManager>();
        
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
            shopManager.AddGold(goldToAdd);
    }
    
    public void AddTestDiamonds()
    {
        if (shopManager != null)
            shopManager.AddDiamonds(diamondsToAdd);
    }
    
    public void ResetAllData()
    {
        GameSaveService.ResetToDefaults();

        TowerManager towerManager = TowerManager.Instance;
        if (towerManager != null)
            towerManager.RefreshTowersBought();

        BallManager ballManager = BallManager.Instance;
        if (ballManager != null)
            ballManager.RefreshBallsBought();

        if (shopManager != null)
            shopManager.UpdateShopUI();
    }
    
    public void UnlockAllTowers()
    {
        TowerManager towerManager = TowerManager.Instance;
        if (towerManager != null && towerManager.allTowers != null)
        {
            for (int i = 0; i < towerManager.allTowers.Length; i++)
                GameSaveService.SetTowerPurchased(i, true);

            towerManager.RefreshTowersBought();
        }

        if (shopManager != null)
            shopManager.UpdateShopUI();
    }
}
