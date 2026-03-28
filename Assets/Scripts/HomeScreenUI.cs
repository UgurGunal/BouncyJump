using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HomeScreenUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Button playButton;
    public Button settingsButton;
    [Tooltip("Panel shown when Settings is pressed. Assign your settings root GameObject; keep it disabled in the scene if it should start hidden.")]
    public GameObject settingsPanel;
    [Tooltip("Optional: close button on the settings panel. You can also leave this empty and use the Button’s On Click → HomeScreenUI.CloseSettingsPanel.")]
    public Button settingsCloseButton;
    public Button shopButton;
    public Button buyDiamondButton;
    public Button buyGoldButton;

    [Header("Shop Integration")]
    public ShopManager shopManager;
    [Tooltip("Scroll amount: positive = how far down to scroll (e.g. 2450), OR use negative for exact Content anchored Y (e.g. -2450).")]
    public float buyGoldShopContentAnchoredY;
    [Tooltip("Scroll amount: positive = how far down to scroll, or negative = exact Content Y.")]
    public float buyDiamondShopContentAnchoredY;
    
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

        if (settingsCloseButton != null)
            settingsCloseButton.onClick.AddListener(OnSettingsCloseClick);
        
        if (shopButton != null)
        {
            shopButton.onClick.AddListener(OnShopButtonClick);
        }
        
        if (buyDiamondButton != null)
            buyDiamondButton.onClick.AddListener(OnBuyDiamondShopClick);

        if (buyGoldButton != null)
            buyGoldButton.onClick.AddListener(OnBuyGoldShopClick);

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
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
        else
            Debug.LogWarning("HomeScreenUI: Assign Settings Panel to show the settings menu.");
    }

    void OnSettingsCloseClick()
    {
        CloseSettingsPanel();
    }

    /// <summary>Hides the settings panel. Safe to call from the close button’s On Click () in the Inspector.</summary>
    public void CloseSettingsPanel()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
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

    void OnBuyGoldShopClick()
    {
        if (shopManager == null)
        {
            Debug.LogError("Shop Manager not found! Please assign it in the inspector.");
            return;
        }
        shopManager.OpenShop(buyGoldShopContentAnchoredY);
    }

    void OnBuyDiamondShopClick()
    {
        if (shopManager == null)
        {
            Debug.LogError("Shop Manager not found! Please assign it in the inspector.");
            return;
        }
        shopManager.OpenShop(buyDiamondShopContentAnchoredY);
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
