using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor-only cheat controls for shop testing. Hidden and inert in player builds.
/// </summary>
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

    void Awake()
    {
#if !UNITY_EDITOR
        HideTestControls();
        enabled = false;
#endif
    }
    
    void Start()
    {
#if !UNITY_EDITOR
        return;
#else
        shopManager = FindObjectOfType<ShopManager>();
        
        if (addGoldButton != null)
            addGoldButton.onClick.AddListener(AddTestGold);
            
        if (addDiamondsButton != null)
            addDiamondsButton.onClick.AddListener(AddTestDiamonds);
            
        if (resetAllButton != null)
            resetAllButton.onClick.AddListener(ResetAllData);
            
        if (unlockAllTowersButton != null)
            unlockAllTowersButton.onClick.AddListener(UnlockAllTowers);
#endif
    }

    void HideTestControls()
    {
        SetButtonActive(addGoldButton, false);
        SetButtonActive(addDiamondsButton, false);
        SetButtonActive(resetAllButton, false);
        SetButtonActive(unlockAllTowersButton, false);
    }

    static void SetButtonActive(Button button, bool active)
    {
        if (button != null)
            button.gameObject.SetActive(active);
    }
    
    public void AddTestGold()
    {
#if UNITY_EDITOR
        if (shopManager != null)
            shopManager.AddGold(goldToAdd);
#endif
    }
    
    public void AddTestDiamonds()
    {
#if UNITY_EDITOR
        if (shopManager != null)
            shopManager.AddDiamonds(diamondsToAdd);
#endif
    }
    
    public void ResetAllData()
    {
#if UNITY_EDITOR
        GameSaveService.ResetToDefaults();

        TowerManager towerManager = TowerManager.Instance;
        if (towerManager != null)
            towerManager.RefreshTowersBought();

        BallManager ballManager = BallManager.Instance;
        if (ballManager != null)
            ballManager.RefreshBallsBought();

        if (shopManager != null)
            shopManager.UpdateShopUI();
#endif
    }
    
    public void UnlockAllTowers()
    {
#if UNITY_EDITOR
        TowerManager towerManager = TowerManager.Instance;
        if (towerManager != null && towerManager.allTowers != null)
        {
            for (int i = 0; i < towerManager.allTowers.Length; i++)
                GameSaveService.SetTowerPurchased(i, true);

            towerManager.RefreshTowersBought();
        }

        if (shopManager != null)
            shopManager.UpdateShopUI();
#endif
    }
}
