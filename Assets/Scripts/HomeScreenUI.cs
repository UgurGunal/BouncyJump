using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HomeScreenUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Button playButton;
    public Button shopButton;
    public Button towersButton;
    public Button buyDiamondButton;

    [Header("Shop Integration")]
    public ShopManager shopManager;
    
    [Header("Tower Integration")]
    public TowerManager towerManager;

    void Start()
    {
        // Set up button listeners
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayButtonClick);
        }
        
        if (shopButton != null)
        {
            shopButton.onClick.AddListener(OnShopButtonClick);
        }
        
        if (towersButton != null)
        {
            towersButton.onClick.AddListener(OnTowersButtonClick);
        }
        
        if (buyDiamondButton != null)
        {
            buyDiamondButton.onClick.AddListener(OnBuyDiamondButtonClick);
        }

        // Initialize managers if not assigned (search inactive so we find manager on disabled shop panel)
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
        
        if (towerManager == null)
        {
            towerManager = TowerManager.Instance;
        }
    }

    void OnPlayButtonClick()
    {
        Debug.Log("Play button clicked - Loading current tower scene");
        
        if (towerManager != null)
        {
            string sceneToLoad = towerManager.GetCurrentTowerSceneName();
            Debug.Log($"Loading scene: {sceneToLoad}");
            
            try
            {
                SceneManager.LoadScene(sceneToLoad);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load scene '{sceneToLoad}': {e.Message}");
                Debug.LogWarning("Please check your Build Settings and ensure the scene is added to the build");
                
                // Fallback: try to load default scene
                try
                {
                    SceneManager.LoadScene("GameScene");
                }
                catch (System.Exception e2)
                {
                    Debug.LogError($"Failed to load fallback GameScene: {e2.Message}");
                }
            }
        }
        else
        {
            Debug.LogError("TowerManager not found!");
        }
    }

    void OnShopButtonClick()
    {
        Debug.Log("Shop button clicked - TODO: Implement general shop functionality");
        // TODO: Implement general shop functionality (if different from tower shop)
    }

    void OnTowersButtonClick()
    {
        Debug.Log("Towers button clicked - Opening tower shop panel");
        
        if (shopManager != null)
        {
            shopManager.OpenShop();
        }
        else
        {
            Debug.LogError("Shop Manager not found! Please assign it in the inspector.");
        }
    }

    // Mock diamond amounts (replace with real IAP / ad rewards later)
    const int MockDiamondsFromAd = 5;
    const int MockDiamondsFromIAP = 50;

    void OnBuyDiamondButtonClick()
    {
        if (shopManager == null) return;
        // Mock: for now grant diamonds as if user watched an ad. Wire IAP to MockPurchaseDiamondsWithRealMoney when ready.
        shopManager.MockGrantDiamondsFromAd(MockDiamondsFromAd);
        var currencyDisplay = FindObjectOfType<HomeScreenCurrencyDisplay>();
        if (currencyDisplay != null) currencyDisplay.RefreshCurrencyDisplay();
    }

    /// <summary>Call this from an IAP success handler or a separate "Buy with real money" button.</summary>
    public void OnBuyDiamondsWithRealMoney()
    {
        if (shopManager == null) return;
        shopManager.MockPurchaseDiamondsWithRealMoney(MockDiamondsFromIAP);
        var currencyDisplay = FindObjectOfType<HomeScreenCurrencyDisplay>();
        if (currencyDisplay != null) currencyDisplay.RefreshCurrencyDisplay();
    }
}
