using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HomeScreenUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Button playButton;
    public Button settingsButton;
    public Button shopButton;
    public Button buyDiamondButton;
    public Button buyGoldButton;

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
        
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsButtonClick);
        }
        
        if (shopButton != null)
        {
            shopButton.onClick.AddListener(OnShopButtonClick);
        }
        
        // Buy diamond / buy gold shortcuts only open the shop for now (same as shop button).
        if (buyDiamondButton != null)
        {
            buyDiamondButton.onClick.AddListener(OnShopButtonClick);
        }

        if (buyGoldButton != null)
        {
            buyGoldButton.onClick.AddListener(OnShopButtonClick);
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

    void OnSettingsButtonClick()
    {
        Debug.Log("Settings button clicked - TODO: Implement settings panel");
        // TODO: Implement settings (audio, etc.)
    }

    void OnShopButtonClick()
    {
        Debug.Log("Shop button clicked - Opening shop panel");
        
        if (shopManager != null)
        {
            shopManager.OpenShop();
        }
        else
        {
            Debug.LogError("Shop Manager not found! Please assign it in the inspector.");
        }
    }

    // Mock amount for IAP path (replace with real purchase payload when ready).
    const int MockDiamondsFromIAP = 50;

    /// <summary>Call this from an IAP success handler or a separate "Buy with real money" button.</summary>
    public void OnBuyDiamondsWithRealMoney()
    {
        if (shopManager == null) return;
        shopManager.MockPurchaseDiamondsWithRealMoney(MockDiamondsFromIAP);
        var currencyDisplay = FindObjectOfType<HomeScreenCurrencyDisplay>();
        if (currencyDisplay != null) currencyDisplay.RefreshCurrencyDisplay();
    }
}
