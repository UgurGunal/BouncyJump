using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Add this to any "Buy Diamond" pack button. Set diamondAmount in the Inspector.
/// Purchase logic and sound are handled by ShopManager.
/// </summary>
[DefaultExecutionOrder(50)]
public class BuyDiamondButton : MonoBehaviour
{
    [Header("Pack")]
    [Tooltip("Diamonds granted when this pack is purchased")]
    public int diamondAmount = 50;

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

        shopManager.MockPurchaseDiamondsWithRealMoney(diamondAmount);
        RefreshCurrencyDisplay();
    }

    void RefreshCurrencyDisplay()
    {
        var display = FindObjectOfType<HomeScreenCurrencyDisplay>();
        if (display != null)
            display.RefreshCurrencyDisplay();
    }
}
