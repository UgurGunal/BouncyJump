using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class HomeScreenUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Button playButton;
    public Button settingsButton;
    [Tooltip("Panel shown when Settings is pressed. Assign your settings root GameObject; keep it disabled in the scene if it should start hidden.")]
    public GameObject settingsPanel;
    [Tooltip("Optional: close button on the settings panel. You can also leave this empty and use the Button's On Click -> HomeScreenUI.CloseSettingsPanel.")]
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

    [Header("Settings - volume (0-1, saved for all scenes)")]
    [Tooltip("Maps to MusicManager master volume. Works even when the manager is in another scene (saved in PlayerPrefs).")]
    public Slider musicVolumeSlider;
    [Tooltip("Maps to SoundEffectsManager master volume.")]
    public Slider sfxVolumeSlider;

    [Header("Settings - in-game HUD")]
    [Tooltip("Your Settings panel Toggle only. Saves ON/OFF for the run timer on the gameplay HUD (PersistentScene). Nothing is shown on the home screen.")]
    public Toggle inGameTimerHudToggle;

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

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.wholeNumbers = false;
            musicVolumeSlider.SetValueWithoutNotify(AudioVolumeSettings.GetMusicVolume());
            musicVolumeSlider.onValueChanged.AddListener(AudioVolumeSettings.SetMusicVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.wholeNumbers = false;
            sfxVolumeSlider.SetValueWithoutNotify(AudioVolumeSettings.GetSfxVolume());
            sfxVolumeSlider.onValueChanged.AddListener(AudioVolumeSettings.SetSfxVolume);
        }

        if (inGameTimerHudToggle == null)
            inGameTimerHudToggle = FindInGameTimerHudToggleInSettings();

        WireInGameTimerHudToggle();
        EnsureButtonReceivesClicks(settingsCloseButton);
    }

    Toggle FindInGameTimerHudToggleInSettings()
    {
        if (settingsPanel == null)
            return null;

        Toggle[] toggles = settingsPanel.GetComponentsInChildren<Toggle>(true);
        for (int i = 0; i < toggles.Length; i++)
        {
            if (toggles[i].gameObject.name == "Toggle")
                return toggles[i];
        }

        return toggles.Length > 0 ? toggles[0] : null;
    }

    void WireInGameTimerHudToggle()
    {
        if (inGameTimerHudToggle == null)
            return;

        inGameTimerHudToggle.SetIsOnWithoutNotify(GameplayDisplaySettings.ShowRunTimer);
        inGameTimerHudToggle.onValueChanged.AddListener(OnInGameTimerHudToggleChanged);
        inGameTimerHudToggle.interactable = true;

        // Only the timer row (e.g. TimeDisplayOption), not the whole settings panel — avoids breaking QuitButton.
        DisableLabelRaycastsInRow(inGameTimerHudToggle.transform.parent);

        if (inGameTimerHudToggle.targetGraphic != null)
            inGameTimerHudToggle.targetGraphic.raycastTarget = true;

        inGameTimerHudToggle.transform.SetAsLastSibling();
    }

    /// <summary>Stops "Time Display" text from blocking the toggle. Does not touch other settings UI (quit, sliders).</summary>
    static void DisableLabelRaycastsInRow(Transform row)
    {
        if (row == null)
            return;

        TextMeshProUGUI[] labels = row.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < labels.Length; i++)
            labels[i].raycastTarget = false;
    }

    static void EnsureButtonReceivesClicks(Button button)
    {
        if (button == null)
            return;

        if (button.targetGraphic != null)
        {
            button.targetGraphic.raycastTarget = true;
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image == null)
            return;

        image.raycastTarget = true;
        button.targetGraphic = image;
    }

    void OnInGameTimerHudToggleChanged(bool show)
    {
        GameplayDisplaySettings.SetShowRunTimer(show);
    }

    void OnPlayButtonClick()
    {
        
        if (towerManager != null)
        {
            string sceneToLoad = towerManager.GetCurrentTowerSceneName();
            
            try
            {
                // Ensure PersistentScene reloads after returning from Home (avoids stale load flag).
                PersistentLoader.ResetForRestart();
                SceneManager.LoadScene(sceneToLoad);
            }
            catch (System.Exception e)
            {
                
                // Fallback: try to load default scene
                try
                {
                    SceneManager.LoadScene("GameScene");
                }
                catch (System.Exception e2)
                {
                }
            }
        }
    }

    void OnSettingsButtonClick()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    void OnSettingsCloseClick()
    {
        CloseSettingsPanel();
    }

    /// <summary>Hides the settings panel. Safe to call from the close button's On Click () in the Inspector.</summary>
    public void CloseSettingsPanel()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    void OnShopButtonClick()
    {
        
        if (shopManager != null)
            shopManager.OpenShop();
    }

    void OnBuyGoldShopClick()
    {
        if (shopManager == null)
        {
            return;
        }
        shopManager.OpenShop(buyGoldShopContentAnchoredY);
    }

    void OnBuyDiamondShopClick()
    {
        if (shopManager == null)
        {
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
