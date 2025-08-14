using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HomeScreenUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Button playButton;
    public Button shopButton;
    public Button towersButton;
    public Button buyGoldButton;
    public Button buyDiamondButton;

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
        
        if (buyGoldButton != null)
        {
            buyGoldButton.onClick.AddListener(OnBuyGoldButtonClick);
        }
        
        if (buyDiamondButton != null)
        {
            buyDiamondButton.onClick.AddListener(OnBuyDiamondButtonClick);
        }
    }

    void OnPlayButtonClick()
    {
        Debug.Log("Play button clicked - Loading game scene");
        
        // Try to load the game scene - you may need to adjust the scene name
        // based on your actual scene setup in Build Settings
        try
        {
            SceneManager.LoadScene("GameScene");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load GameScene: {e.Message}");
            Debug.LogWarning("Please check your Build Settings and ensure 'GameScene' is added to the build");
            
            // Fallback: try to load scene by index 1 (assuming it's the game scene)
            // You can change this index based on your build settings
            try
            {
                SceneManager.LoadScene(1);
            }
            catch (System.Exception e2)
            {
                Debug.LogError($"Failed to load scene by index 1: {e2.Message}");
            }
        }
    }

    void OnShopButtonClick()
    {
        Debug.Log("Shop button clicked - TODO: Implement shop functionality");
        // TODO: Implement shop functionality
    }

    void OnTowersButtonClick()
    {
        Debug.Log("Towers button clicked - TODO: Implement towers functionality");
        // TODO: Implement towers functionality
    }

    void OnBuyGoldButtonClick()
    {
        Debug.Log("Buy Gold button clicked - TODO: Implement gold purchase");
        // TODO: Implement gold purchase functionality
    }

    void OnBuyDiamondButtonClick()
    {
        Debug.Log("Buy Diamond button clicked - TODO: Implement diamond purchase");
        // TODO: Implement diamond purchase functionality
    }
}
