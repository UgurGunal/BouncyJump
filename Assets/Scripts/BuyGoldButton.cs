using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Add this to any "Buy Gold" button. Set diamondCost and goldAmount in the Inspector
/// (or use multiple instances for different packs). Purchase logic is in ShopManager.
/// </summary>
public class BuyGoldButton : MonoBehaviour
{
    [Header("Pack")]
    [Tooltip("Diamonds required to buy this gold pack")]
    public int diamondCost = 1;
    [Tooltip("Gold granted when purchase succeeds")]
    public int goldAmount = 100;

    ShopManager shopManager;

    void Awake()
    {
        Button button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    void Start()
    {
        if (shopManager == null)
        {
            shopManager = FindObjectOfType<ShopManager>();
            if (shopManager == null)
            {
                ShopManager[] found = FindObjectsOfType<ShopManager>(true);
                if (found != null && found.Length > 0)
                    shopManager = found[0];
            }
        }
    }

    void OnClick()
    {
        if (shopManager == null)
            shopManager = FindObjectOfType<ShopManager>(true);
        if (shopManager == null) return;

        if (shopManager.TryBuyGoldWithDiamonds(diamondCost, goldAmount))
        {
            Debug.Log($"Bought {goldAmount} gold for {diamondCost} diamond(s).");
            RefreshCurrencyDisplay();
        }
        else
        {
            Debug.Log("Not enough diamonds to buy this gold pack.");
        }
    }

    void RefreshCurrencyDisplay()
    {
        var display = FindObjectOfType<HomeScreenCurrencyDisplay>();
        if (display != null)
            display.RefreshCurrencyDisplay();
    }

    /// <summary>Call from ShopManager.UpdateShopUI so button is disabled when player can't afford this pack.</summary>
    public void RefreshButton()
    {
        if (shopManager == null)
        {
            shopManager = FindObjectOfType<ShopManager>(true);
            if (shopManager == null) return;
        }
        Button button = GetComponent<Button>();
        if (button != null)
            button.interactable = shopManager.GetPlayerDiamonds() >= diamondCost;
    }
}
