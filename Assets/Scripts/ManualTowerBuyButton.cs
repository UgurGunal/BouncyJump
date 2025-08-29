using UnityEngine;
using UnityEngine.UI;

public class ManualTowerBuyButton : MonoBehaviour
{
    [Header("Tower Settings")]
    public string towerName = "BasicTower"; // Must match tower name in SimpleTowerManager
    public int towerIndex = 0; // Index of this tower in SimpleTowerManager.allTowers
    
    [Header("UI References")]
    public Button buyButton;
    public Image buttonImage; // The Image component of the button to change sprites
    public RectTransform buttonRectTransform; // The RectTransform to resize when changing sprites
    
    [Header("Button Images")]
    public Sprite originalBuyButtonSprite; // Original buy button sprite (for not bought towers)
    public Sprite selectButtonSprite; // When tower is bought but not selected
    public Sprite selectedButtonSprite; // When tower is bought and selected
    
    [Header("Visual Effects")]
    public Color selectedTintColor = new Color(0.7f, 0.7f, 0.7f, 1f); // Darker tint for selected button
    public Color normalTintColor = Color.white; // Normal color
    
    private SimpleTowerManager towerManager;
    private TowerShopManager shopManager;
    
    void Start()
    {
        // Get managers
        towerManager = SimpleTowerManager.Instance;
        shopManager = FindObjectOfType<TowerShopManager>();
        
        // Auto-assign RectTransform if not set
        if (buttonRectTransform == null && buttonImage != null)
        {
            buttonRectTransform = buttonImage.rectTransform;
        }
        
        // Store original sprite if not manually set
        if (originalBuyButtonSprite == null && buttonImage != null)
        {
            originalBuyButtonSprite = buttonImage.sprite;
        }
        
        // Set up button listener
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnButtonClicked);
        }
        
        // Update button state
        UpdateButtonState();
    }
    
    void Update()
    {
        // Update button state every frame (you can optimize this later)
        UpdateButtonState();
    }
    
    void OnButtonClicked()
    {
        if (towerManager == null)
        {
            Debug.LogError("SimpleTowerManager not found!");
            return;
        }
        
        bool isBought = towerManager.IsTowerBought(towerIndex);
        bool isSelected = (towerManager.currentTowerIndex == towerIndex);
        
        if (!isBought)
        {
            // Buy the tower
            towerManager.BuyTower(towerIndex);
            Debug.Log($"Attempting to buy {towerName}");
        }
        else if (isBought && !isSelected)
        {
            // Select the tower
            towerManager.SetCurrentTower(towerIndex);
            Debug.Log($"Selected {towerName}");
        }
        else if (isSelected)
        {
            Debug.Log($"{towerName} is already selected!");
            // Don't do anything when clicking already selected tower
            return;
        }
        
        // Refresh shop UI if available
        if (shopManager != null)
        {
            shopManager.UpdateShopUI();
        }
    }
    
    void UpdateButtonState()
    {
        if (towerManager == null || buyButton == null) return;
        
        bool isBought = towerManager.IsTowerBought(towerIndex);
        bool isSelected = (towerManager.currentTowerIndex == towerIndex);
        
        // Update button image and color based on tower state
        if (buttonImage != null)
        {
            if (isBought && isSelected && selectedButtonSprite != null)
            {
                // Tower is bought and selected - show selected image with darker tint
                buttonImage.sprite = selectedButtonSprite;
                buttonImage.color = selectedTintColor; // Darken the selected button
                SetNativeSize(selectedButtonSprite); // Resize to native sprite size
                buyButton.interactable = true;
            }
            else if (isBought && !isSelected && selectButtonSprite != null)
            {
                // Tower is bought but not selected - show select image with normal color
                buttonImage.sprite = selectButtonSprite;
                buttonImage.color = normalTintColor; // Normal color
                SetNativeSize(selectButtonSprite); // Resize to native sprite size
                buyButton.interactable = true;
            }
            else if (!isBought && originalBuyButtonSprite != null)
            {
                // Tower is not bought - show original buy button sprite
                buttonImage.sprite = originalBuyButtonSprite;
                buttonImage.color = normalTintColor; // Normal color
                SetNativeSize(originalBuyButtonSprite); // Resize to native sprite size
                buyButton.interactable = true;
            }
            else
            {
                // Fallback - just ensure normal color and interactable
                buttonImage.color = normalTintColor;
                buyButton.interactable = true;
            }
        }
    }
    
    // Method to manually refresh the button (call this after purchases)
    public void RefreshButton()
    {
        UpdateButtonState();
    }
    
    // Method to resize button to sprite's native size
    void SetNativeSize(Sprite sprite)
    {
        if (buttonImage != null && sprite != null)
        {
            // Use Unity's built-in SetNativeSize method which handles all the scaling correctly
            buttonImage.SetNativeSize();
        }
    }
}
