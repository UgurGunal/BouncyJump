using UnityEngine;
using UnityEngine.UI;

public class ComboBarUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider comboSlider;

    [Header("Smooth Transition Settings")]
    [Tooltip("Higher = snappier bar. ~10 slow, ~24 default, ~40 near-instant.")]
    public float smoothSpeed = 24f;
    public bool useSmoothTransitions = true;

    private ComboManager comboManager;
    private float targetComboValue = 0f;
    private float currentSliderValue = 0f;
    private float lastWrittenSliderValue = float.NaN;
    private float lastUpdateTime = 0f;
    private const float UPDATE_INTERVAL = 1f / 60f; // 60 FPS update interval
    private const float SliderWriteEpsilon = 0.01f;

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
            lastWrittenSliderValue = 0f;
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
            float t = 1f - Mathf.Exp(-smoothSpeed * UPDATE_INTERVAL);
            currentSliderValue = Mathf.Lerp(currentSliderValue, targetComboValue, t);

            // Snap when close enough so we stop dirtying the Canvas every frame.
            if (Mathf.Abs(currentSliderValue - targetComboValue) <= SliderWriteEpsilon)
                currentSliderValue = targetComboValue;

            WriteSliderValue(currentSliderValue);
        }
        else
        {
            currentSliderValue = targetComboValue;
            WriteSliderValue(targetComboValue);
        }
    }

    void WriteSliderValue(float value)
    {
        if (comboSlider == null)
            return;

        if (!float.IsNaN(lastWrittenSliderValue) && Mathf.Abs(value - lastWrittenSliderValue) < SliderWriteEpsilon)
            return;

        comboSlider.value = value;
        lastWrittenSliderValue = value;
    }

    // Public method to force instant update (useful for testing)
    public void ForceUpdate()
    {
        if (comboManager != null && comboSlider != null)
        {
            targetComboValue = comboManager.CurrentCombo;
            currentSliderValue = targetComboValue;
            lastWrittenSliderValue = float.NaN;
            WriteSliderValue(targetComboValue);
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
