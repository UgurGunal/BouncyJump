using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Buy-diamond pack button. Set <see cref="productId"/> to a store product
/// (e.g. com.rugustudios.gems50). Purchase is handled by <see cref="IAPManager"/>.
/// </summary>
[DefaultExecutionOrder(50)]
public class BuyDiamondButton : MonoBehaviour
{
    [Header("IAP")]
    [Tooltip("Store product id, e.g. com.rugustudios.gems50")]
    public string productId = IAPManager.ProductGems50;

    [Tooltip("Fallback diamond amount if IAP catalog mapping is missing (should match the product).")]
    public int diamondAmount = 50;

    Button _button;

    void Awake()
    {
        _button = GetComponent<Button>();
        if (_button != null)
            _button.onClick.AddListener(OnClick);
    }

    void Start()
    {
        IAPManager.EnsureExists();
        if (string.IsNullOrEmpty(productId) && diamondAmount > 0)
            productId = GuessProductId(diamondAmount);
    }

    void OnClick()
    {
        IAPManager iap = IAPManager.EnsureExists();
        if (iap == null)
            return;

        if (string.IsNullOrEmpty(productId))
        {
            Debug.LogError("[BuyDiamondButton] productId is empty.");
            return;
        }

        if (!iap.IsReady)
        {
            Debug.LogWarning("[BuyDiamondButton] IAP not ready yet. Try again in a moment.");
            return;
        }

        iap.Buy(productId);
    }

    static string GuessProductId(int amount)
    {
        switch (amount)
        {
            case 50: return IAPManager.ProductGems50;
            case 300: return IAPManager.ProductGems300;
            case 2000: return IAPManager.ProductGems2000;
            default: return null;
        }
    }
}
