using UnityEngine;
using UnityEngine.UI;

public class ComboBarUI : MonoBehaviour
{
    [Header("UI References")]
    public Image comboBarFill; // The red fill image
    public Image comboBarBackground; // The background image
    
    private ComboManager comboManager;
    private RectTransform backgroundRect;
    private RectTransform fillRect;
    private float maxComboValue;
    
    void Start()
    {
        // Find ComboManager
        comboManager = ComboManager.Instance;
        
        // Get max combo value from ComboManager
        if (comboManager != null)
        {
            maxComboValue = comboManager.maxCombo;
        }
        else
        {
            maxComboValue = 1000f; // Default fallback
        }
        
        // Get RectTransforms for sizing
        if (comboBarBackground != null)
        {
            backgroundRect = comboBarBackground.GetComponent<RectTransform>();
        }
        if (comboBarFill != null)
        {
            fillRect = comboBarFill.GetComponent<RectTransform>();
        }
        
        // Ensure proper UI setup
        SetupUI();
        
        // Initialize bar
        UpdateComboBar();
    }
    
    void SetupUI()
    {
        if (comboBarFill != null && comboBarBackground != null)
        {
            // Make fill image match background exactly
            if (fillRect != null && backgroundRect != null)
            {
                // Copy background's size and position to fill
                fillRect.sizeDelta = backgroundRect.sizeDelta;
                fillRect.anchoredPosition = backgroundRect.anchoredPosition;
                fillRect.anchorMin = backgroundRect.anchorMin;
                fillRect.anchorMax = backgroundRect.anchorMax;
                fillRect.pivot = backgroundRect.pivot;
                
                // Start with zero width
                fillRect.sizeDelta = new Vector2(0f, fillRect.sizeDelta.y);
            }
            
            //Debug.Log("UI Setup completed - Fill matches background");
        }
    }
    
    void Update()
    {
        UpdateComboBar();
    }
    
    void UpdateComboBar()
    {
        if (comboManager != null && comboBarFill != null && comboBarBackground != null)
        {
            // Get current combo value
            float currentCombo = comboManager.getCombo();
            
            // Calculate fill ratio (0 to 1)
            float fillRatio = currentCombo / maxComboValue;
            fillRatio = Mathf.Clamp01(fillRatio); // Ensure it's between 0 and 1
            
            // Get background width and calculate fill width
            if (backgroundRect != null && fillRect != null)
            {
                float backgroundWidth = backgroundRect.sizeDelta.x;
                float fillWidth = backgroundWidth * fillRatio;
                
                // Update fill width
                fillRect.sizeDelta = new Vector2(fillWidth, fillRect.sizeDelta.y);
                
                // Debug.Log($"Combo: {currentCombo}, Ratio: {fillRatio:F2}, Fill Width: {fillWidth:F1}");
            }
        }
        else
        {
            // No ComboManager or UI elements, hide the bar
            if (fillRect != null)
            {
                fillRect.sizeDelta = new Vector2(0f, fillRect.sizeDelta.y);
            }
            
            //Debug.LogWarning("ComboBarUI: Missing ComboManager or UI elements!");
        }
    }
} 