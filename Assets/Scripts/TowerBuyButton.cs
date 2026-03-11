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
    
    [Header("Visual Effects")]
    public Color selectedTintColor = new Color(0.7f, 0.7f, 0.7f, 1f); // Darker tint for selected button
    public Color normalTintColor = Color.white; // Normal color
    
    private TowerManager towerManager;
    private TowerShopManager shopManager;
    private Image buttonImage;
    
    void Start()
    {
        towerManager = TowerManager.Instance;
        shopManager = FindObjectOfType<TowerShopManager>();
        
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
            Debug.LogError("TowerManager not found!");
            return;
        }
        
        bool isBought = towerManager.IsTowerBought(towerIndex);
        bool isSelected = (towerManager.currentTowerIndex == towerIndex);
        
        if (!isBought)
        {
            towerManager.BuyTower(towerIndex);
            Debug.Log($"Attempting to buy {towerName}");
        }
        else if (isBought && !isSelected)
        {
            towerManager.SetCurrentTower(towerIndex);
            Debug.Log($"Selected {towerName}");
        }
        else if (isSelected)
        {
            Debug.Log($"{towerName} is already selected!");
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
                buttonImage.sprite = selectedSprite;
                buttonImage.color = selectedTintColor;
                SetNativeSize(selectedSprite);
                buyButton.interactable = true;
            }
            else if (isBought && !isSelected && selectSprite != null)
            {
                buttonImage.sprite = selectSprite;
                buttonImage.color = normalTintColor;
                SetNativeSize(selectSprite);
                buyButton.interactable = true;
            }
            else if (!isBought && buySprite != null)
            {
                buttonImage.sprite = buySprite;
                buttonImage.color = normalTintColor;
                SetNativeSize(buySprite);
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
    
    void SetNativeSize(Sprite sprite)
    {
        if (buttonImage != null && sprite != null)
        {
            buttonImage.SetNativeSize();
        }
    }
}
