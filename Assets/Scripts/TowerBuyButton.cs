using UnityEngine;
using UnityEngine.UI;

public class TowerBuyButton : MonoBehaviour
{
    [Header("Tower Settings")]
    public string towerName = "BasicTower"; // Must match tower name in TowerManager
    public int towerIndex = 0; // Index of this tower in TowerManager.allTowers
    
    [Header("UI References")]
    [Tooltip("The buy/select button. Image is auto-resolved from this Button's GameObject.")]
    public Button buyButton;
    
    [Header("Button Images")]
    public Sprite originalBuyButtonSprite; // Original buy button sprite (for not bought towers)
    public Sprite selectButtonSprite; // When tower is bought but not selected
    public Sprite selectedButtonSprite; // When tower is bought and selected

    [Header("Sprite Size")]
    [Tooltip("When true, the button image will resize to the sprite's native size whenever the sprite changes.")]
    public bool resizeToNativeSizeOnSpriteChange = true;
    
    [Header("Visual Effects")]
    public Color selectedTintColor = new Color(0.7f, 0.7f, 0.7f, 1f); // Darker tint for selected button
    public Color normalTintColor = Color.white; // Normal color
    
    private TowerManager towerManager;
    private ShopManager shopManager;
    private Image buttonImage;

    private void ApplySprite(Sprite sprite, Color color)
    {
        if (buttonImage == null) return;

        buttonImage.sprite = sprite;
        buttonImage.color = color;

        if (resizeToNativeSizeOnSpriteChange && sprite != null)
        {
            // Resizes the RectTransform to match the sprite's native pixel size.
            buttonImage.SetNativeSize();
        }
    }
    
    void Start()
    {
        towerManager = TowerManager.Instance;
        shopManager = FindObjectOfType<ShopManager>();
        
        if (buyButton != null)
        {
            buttonImage = buyButton.GetComponent<Image>();
            if (buttonImage == null && buyButton.targetGraphic != null)
                buttonImage = buyButton.targetGraphic as Image;
            if (buttonImage == null)
                buttonImage = buyButton.GetComponentInChildren<Image>();
            if (originalBuyButtonSprite == null && buttonImage != null)
                originalBuyButtonSprite = buttonImage.sprite;
        }
        
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnButtonClicked);
        }
        
        UpdateButtonState();
    }
    
    void OnEnable()
    {
        TowerManager.OnSelectionChanged += RefreshButton;
        TowerManager.OnTowerPurchased += RefreshButton;
    }
    
    void OnDisable()
    {
        TowerManager.OnSelectionChanged -= RefreshButton;
        TowerManager.OnTowerPurchased -= RefreshButton;
    }
    
    void OnButtonClicked()
    {
        if (towerManager == null)
        {
            return;
        }
        
        bool isBought = towerManager.IsTowerBought(towerIndex);
        bool isSelected = (towerManager.currentTowerIndex == towerIndex);
        
        if (!isBought)
        {
            towerManager.BuyTower(towerIndex);
        }
        else if (isBought && !isSelected)
        {
            towerManager.SetCurrentTower(towerIndex);
        }
        else if (isSelected)
        {
            return;
        }
        
        if (shopManager != null)
        {
            shopManager.UpdateShopUI();
        }
    }
    
    void UpdateButtonState()
    {
        if (towerManager == null || buyButton == null || buttonImage == null) return;
        
        bool isBought = towerManager.IsTowerBought(towerIndex);
        bool isSelected = (towerManager.currentTowerIndex == towerIndex);

        Sprite buySprite = originalBuyButtonSprite;
        Sprite selectSprite = selectButtonSprite;
        Sprite selectedSprite = selectedButtonSprite;
        
        if (buttonImage != null)
        {
            if (isBought && isSelected && selectedSprite != null)
            {
                ApplySprite(selectedSprite, selectedTintColor);
                buyButton.interactable = true;
            }
            else if (isBought && !isSelected && selectSprite != null)
            {
                ApplySprite(selectSprite, normalTintColor);
                buyButton.interactable = true;
            }
            else if (!isBought && buySprite != null)
            {
                ApplySprite(buySprite, normalTintColor);
                buyButton.interactable = true;
            }
            else
            {
                buttonImage.color = normalTintColor;
                buyButton.interactable = true;
            }
        }
    }
    
    /// <summary>Called when selection or purchase changes. Used by TowerManager events.</summary>
    public void RefreshButton()
    {
        UpdateButtonState();
    }
}
