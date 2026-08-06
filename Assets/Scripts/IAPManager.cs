using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

/// <summary>
/// Unity IAP 5 store connection for consumable diamond packs.
/// Product IDs must match App Store Connect / Google Play Console.
/// </summary>
[DefaultExecutionOrder(-100)]
public class IAPManager : MonoBehaviour
{
    public const string ProductGems50 = "com.rugustudios.gems50";
    public const string ProductGems300 = "com.rugustudios.gems300";
    public const string ProductGems2000 = "com.rugustudios.gems2000";

    const string ProcessedTxPrefsKey = "iap_processed_tx_ids";

    public static IAPManager Instance { get; private set; }

    public bool IsReady { get; private set; }

    public event Action OnReady;
    public event Action<string> OnPurchaseSucceeded;
    public event Action<string> OnPurchaseFailedEvent;

    StoreController _store;
    readonly Dictionary<string, int> _diamondRewards = new Dictionary<string, int>
    {
        { ProductGems50, 50 },
        { ProductGems300, 300 },
        { ProductGems2000, 2000 },
    };

    HashSet<string> _processedTxIds;

    public static IAPManager EnsureExists()
    {
        if (Instance != null)
            return Instance;

        IAPManager existing = FindObjectOfType<IAPManager>(true);
        if (existing != null)
            return existing;

        var go = new GameObject("IAPManager");
        return go.AddComponent<IAPManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Only remove this duplicate component. Destroy(gameObject) would also wipe
            // co-mounted scene objects (e.g. ShopManager on HomeScene) and break the shop
            // after returning from Tutorial / towers.
            Destroy(this);
            return;
        }

        // Never DontDestroyOnLoad a shared HomeScene object — that would persist ShopManager
        // with destroyed UI refs after the scene unloads.
        if (IsMountedWithOtherBehaviours())
        {
            var dedicated = new GameObject("IAPManager");
            dedicated.AddComponent<IAPManager>();
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        _processedTxIds = LoadProcessedTxIds();
        InitializeStore();
    }

    bool IsMountedWithOtherBehaviours()
    {
        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null && behaviours[i] != this)
                return true;
        }
        return false;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (_store == null)
            return;

        _store.OnStoreConnected -= OnStoreConnected;
        _store.OnStoreDisconnected -= OnStoreDisconnected;
        _store.OnProductsFetched -= OnProductsFetched;
        _store.OnProductsFetchFailed -= OnProductsFetchFailed;
        _store.OnPurchasePending -= OnPurchasePending;
        _store.OnPurchaseConfirmed -= OnPurchaseConfirmed;
        _store.OnPurchaseFailed -= OnPurchaseFailed;
        _store.OnPurchaseDeferred -= OnPurchaseDeferred;
    }

    async void InitializeStore()
    {
        try
        {
            _store = UnityIAPServices.StoreController();

            _store.OnStoreConnected += OnStoreConnected;
            _store.OnStoreDisconnected += OnStoreDisconnected;
            _store.OnProductsFetched += OnProductsFetched;
            _store.OnProductsFetchFailed += OnProductsFetchFailed;
            _store.OnPurchasePending += OnPurchasePending;
            _store.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _store.OnPurchaseFailed += OnPurchaseFailed;
            _store.OnPurchaseDeferred += OnPurchaseDeferred;

            await _store.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[IAPManager] Init failed: {ex.Message}");
            IsReady = false;
        }
    }

    void OnStoreConnected()
    {
        var products = new List<ProductDefinition>
        {
            new ProductDefinition(ProductGems50, ProductType.Consumable),
            new ProductDefinition(ProductGems300, ProductType.Consumable),
            new ProductDefinition(ProductGems2000, ProductType.Consumable),
        };
        _store.FetchProducts(products);
    }

    void OnStoreDisconnected(StoreConnectionFailureDescription failure)
    {
        IsReady = false;
        Debug.LogWarning($"[IAPManager] Store disconnected: {failure.Message}");
    }

    void OnProductsFetched(List<Product> products)
    {
        IsReady = true;
        Debug.Log($"[IAPManager] Products ready ({products?.Count ?? 0}).");
        OnReady?.Invoke();
    }

    void OnProductsFetchFailed(ProductFetchFailed failure)
    {
        IsReady = false;
        Debug.LogError($"[IAPManager] Product fetch failed: {failure.FailureReason}");
    }

    /// <summary>Starts store purchase for a catalog product id.</summary>
    public bool Buy(string productId)
    {
        if (_store == null || !IsReady)
        {
            Debug.LogWarning("[IAPManager] Store not ready.");
            OnPurchaseFailedEvent?.Invoke("Store not ready");
            return false;
        }

        if (!_diamondRewards.ContainsKey(productId))
        {
            Debug.LogError($"[IAPManager] Unknown product: {productId}");
            OnPurchaseFailedEvent?.Invoke("Unknown product");
            return false;
        }

        Product product = _store.GetProductById(productId);
        if (product == null)
        {
            Debug.LogError($"[IAPManager] Product not fetched from store: {productId}");
            OnPurchaseFailedEvent?.Invoke("Product unavailable");
            return false;
        }

        _store.PurchaseProduct(product);
        return true;
    }

    public bool TryGetDiamondReward(string productId, out int diamonds)
    {
        return _diamondRewards.TryGetValue(productId, out diamonds);
    }

    public string GetLocalizedPriceString(string productId)
    {
        if (_store == null)
            return null;
        Product product = _store.GetProductById(productId);
        return product?.metadata?.localizedPriceString;
    }

    void OnPurchasePending(PendingOrder pendingOrder)
    {
        string txId = pendingOrder?.Info?.TransactionID;
        if (!string.IsNullOrEmpty(txId) && _processedTxIds.Contains(txId))
        {
            _store.ConfirmPurchase(pendingOrder);
            return;
        }

        Product product = pendingOrder.CartOrdered?.Items()?.FirstOrDefault()?.Product;
        string productId = product?.definition?.storeSpecificId ?? product?.definition?.id;
        if (string.IsNullOrEmpty(productId) || !_diamondRewards.TryGetValue(productId, out int diamonds))
        {
            Debug.LogError($"[IAPManager] Pending order missing known product. id={productId}");
            return;
        }

        // Grant + save before confirm so a crash still redelivers the pending order.
        ShopManager shop = FindObjectOfType<ShopManager>(true);
        if (shop != null)
            shop.GrantDiamondsFromIAP(diamonds);
        else
            GameSaveService.AddDiamonds(diamonds);

        if (!string.IsNullOrEmpty(txId))
        {
            _processedTxIds.Add(txId);
            SaveProcessedTxIds();
        }

        _store.ConfirmPurchase(pendingOrder);
        OnPurchaseSucceeded?.Invoke(productId);
    }

    void OnPurchaseConfirmed(Order order)
    {
        if (order is FailedOrder failed)
            Debug.LogError($"[IAPManager] Confirm failed: {failed.FailureReason} - {failed.Details}");
        else if (order is ConfirmedOrder)
            Debug.Log("[IAPManager] Purchase confirmed.");
    }

    void OnPurchaseFailed(FailedOrder failed)
    {
        string details = failed != null ? $"{failed.FailureReason} - {failed.Details}" : "unknown";
        Debug.LogWarning($"[IAPManager] Purchase failed: {details}");
        OnPurchaseFailedEvent?.Invoke(details);
    }

    void OnPurchaseDeferred(DeferredOrder deferred)
    {
        Debug.Log("[IAPManager] Purchase deferred (e.g. Ask to Buy). Waiting for approval.");
    }

    HashSet<string> LoadProcessedTxIds()
    {
        string raw = PlayerPrefs.GetString(ProcessedTxPrefsKey, "");
        if (string.IsNullOrEmpty(raw))
            return new HashSet<string>();
        return new HashSet<string>(raw.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
    }

    void SaveProcessedTxIds()
    {
        // Keep the set bounded.
        if (_processedTxIds.Count > 200)
        {
            _processedTxIds = new HashSet<string>(_processedTxIds.Skip(_processedTxIds.Count - 150));
        }
        PlayerPrefs.SetString(ProcessedTxPrefsKey, string.Join("|", _processedTxIds));
        PlayerPrefs.Save();
    }
}
