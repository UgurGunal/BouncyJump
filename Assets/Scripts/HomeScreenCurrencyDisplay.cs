using UnityEngine;
using TMPro;

public class HomeScreenCurrencyDisplay : MonoBehaviour
{
    [Header("Currency Display")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI diamondText;
    
    [Header("Update Settings")]
    public bool updateEveryFrame = true; // Set to false if you want manual updates only
    public float updateInterval = 1f; // Update every second if not updating every frame
    
    private float lastUpdateTime;
    private ShopManager shopManager;
    
    void Start()
    {
        // Find the shop manager to get currency methods
        shopManager = FindObjectOfType<ShopManager>();
        
        if (shopManager == null)
        {
        }
        
        // Initial update
        UpdateCurrencyDisplay();
    }
    
    void Update()
    {
        if (updateEveryFrame)
        {
            // Update every frame
            UpdateCurrencyDisplay();
        }
        else
        {
            // Update at intervals
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateCurrencyDisplay();
                lastUpdateTime = Time.time;
            }
        }
    }
    
    void UpdateCurrencyDisplay()
    {
        if (shopManager == null) return;
        
        // Update gold display with comma formatting
        if (goldText != null)
        {
            int gold = shopManager.GetPlayerGold();
            goldText.text = FormatCurrency(gold);
        }
        
        // Update diamond display with comma formatting
        if (diamondText != null)
        {
            int diamonds = shopManager.GetPlayerDiamonds();
            diamondText.text = FormatCurrency(diamonds);
        }
    }
    
    // Method to format currency with commas
    string FormatCurrency(int amount)
    {
        // Use specific culture to ensure commas (not periods)
        return amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
    }
    
    // Method to manually refresh currency display (call this after purchases/additions)
    public void RefreshCurrencyDisplay()
    {
        UpdateCurrencyDisplay();
    }
}
