using UnityEngine;
using UnityEngine.UI;

public class ComboBarUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider comboSlider;

    [Header("Smooth Transition Settings")]
    public float smoothSpeed = 5f; // How fast the slider catches up to actual combo
    public bool useSmoothTransitions = true; // Toggle for smooth vs instant updates

    private ComboManager comboManager;
    private float targetComboValue = 0f;
    private float currentSliderValue = 0f;
    private float lastUpdateTime = 0f;
    private const float UPDATE_INTERVAL = 1f / 60f; // 60 FPS update interval

    void Start()
    {
        // Find ComboManager
        comboManager = ComboManager.Instance;
        
        if (comboManager == null)
        {
            return;
        }

        // Initialize slider with maxCombo from ComboManager
        if (comboSlider != null)
        {
            comboSlider.minValue = 0f;
            comboSlider.maxValue = comboManager.maxCombo; // Automatically fetch maxCombo
            comboSlider.value = 0f;
            currentSliderValue = 0f;
        }
    }

    void Update()
    {
        if (comboManager == null) return;

        // Update at 60 FPS for better performance
        if (Time.time - lastUpdateTime >= UPDATE_INTERVAL)
        {
            UpdateSmoothTransition();
            lastUpdateTime = Time.time;
        }
    }

    void UpdateSmoothTransition()
    {
        // Get the target combo value
        targetComboValue = comboManager.CurrentCombo;

        if (useSmoothTransitions)
        {
            // Smooth transition using Lerp with fixed time step
            currentSliderValue = Mathf.Lerp(currentSliderValue, targetComboValue, smoothSpeed * UPDATE_INTERVAL);
            
            // Update slider with smooth value
            if (comboSlider != null)
            {
                comboSlider.value = currentSliderValue;
            }
        }
        else
        {
            // Instant update (original behavior)
            if (comboSlider != null)
            {
                comboSlider.value = targetComboValue;
            }
        }
    }

    // Public method to force instant update (useful for testing)
    public void ForceUpdate()
    {
        if (comboManager != null && comboSlider != null)
        {
            targetComboValue = comboManager.CurrentCombo;
            currentSliderValue = targetComboValue;
            comboSlider.value = targetComboValue;
        }
    }

    // Public method to toggle smooth transitions
    public void SetSmoothTransitions(bool smooth)
    {
        useSmoothTransitions = smooth;
    }

    // Public method to adjust smooth speed
    public void SetSmoothSpeed(float speed)
    {
        smoothSpeed = Mathf.Max(0.1f, speed);
    }
} 
