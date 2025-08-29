using UnityEngine;
using UnityEngine.UI;

public class TowerShopItemUI : MonoBehaviour
{
    [Header("UI References")]
    public Image towerShopImage; // Complete tower shop image with name and details
    public Button towerActionButton; // Single button for buy/equip/equipped actions
    public Image buttonImage; // Button image showing cost or status
    public Image backgroundImage;
    
    [Header("UI States")]
    public Color purchasedColor = Color.green;
    public Color lockedColor = Color.gray;
    public Color selectedColor = Color.blue;
    
    [Header("Button Images")]
    public Sprite buyButtonImage; // Image for buy state
    public Sprite equipButtonImage; // Image for equip state  
    public Sprite equippedButtonImage; // Image for equipped state
    
    private TowerShopItem towerItem;
    private TowerShopManager shopManager;
    
    public void Initialize(TowerShopItem item, TowerShopManager manager)
    {
        towerItem = item;
        shopManager = manager;
        
        // Set up tower shop image
        if (towerShopImage != null && towerItem.towerShopImage != null)
            towerShopImage.sprite = towerItem.towerShopImage;
        
        // Set up button listener
        if (towerActionButton != null)
            towerActionButton.onClick.AddListener(OnActionButtonClicked);
        
        // Update UI state
        UpdateUI();
    }
    
    public void UpdateUI()
    {
        if (towerItem == null || shopManager == null) return;
        
        bool isAvailable = shopManager.IsTowerPurchased(towerItem); // Available = purchased/bought
        bool isSelected = (shopManager.simpleTowerManager?.GetCurrentTower()?.towerName == towerItem.towerName);
        
        // Update action button based on state
        if (towerActionButton != null && buttonImage != null)
        {
            if (!isAvailable)
            {
                // Tower is not available (not bought yet) - Show buy button with cost image
                buttonImage.sprite = towerItem.buyButtonImage ?? buyButtonImage;
                towerActionButton.interactable = true;
            }
            else if (isAvailable && !isSelected)
            {
                // Tower is available (bought) but not selected - Show select/equip button
                buttonImage.sprite = equipButtonImage;
                towerActionButton.interactable = true;
            }
            else if (isSelected)
            {
                // Tower is available and currently selected - Show selected button (disabled)
                buttonImage.sprite = equippedButtonImage;
                towerActionButton.interactable = false;
            }
        }
        
        // Update background color based on availability and selection
        if (backgroundImage != null)
        {
            if (isSelected)
            {
                backgroundImage.color = selectedColor; // Currently selected tower
            }
            else if (isAvailable)
            {
                backgroundImage.color = purchasedColor; // Available tower (bought)
            }
            else
            {
                backgroundImage.color = lockedColor; // Non-available tower (not bought yet)
            }
        }
    }
    
    void OnActionButtonClicked()
    {
        if (shopManager == null || towerItem == null) return;
        
        bool isAvailable = shopManager.IsTowerPurchased(towerItem); // Available = purchased/bought
        bool isSelected = (shopManager.simpleTowerManager?.GetCurrentTower()?.towerName == towerItem.towerName);
        
        if (!isAvailable)
        {
            // Tower is not available yet - Buy the tower to make it available
            shopManager.PurchaseTower(towerItem);
        }
        else if (isAvailable && !isSelected)
        {
            // Tower is available but not selected - Select/Equip the tower
            shopManager.SelectTower(towerItem);
        }
        // If already selected, button should be disabled so this shouldn't be called
    }
}
