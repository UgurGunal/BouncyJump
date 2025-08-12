using UnityEngine;
using TMPro; // Assuming you are using TextMeshPro for UI text

public class GameStatsUI : MonoBehaviour
{
    [Header("UI Text References (Optional)")]
    public TextMeshProUGUI heightText;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI gemsText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI levelText; // New: Reference for Level Text

    [Header("Update Settings")]
    public float updateInterval = 0.1f; // How often to update the UI (e.g., 10 times per second)

    private float _lastUpdateTime;

    void Update()
    {
        if (Time.time - _lastUpdateTime >= updateInterval)
        {
            UpdateUI();
            _lastUpdateTime = Time.time;
        }
    }

    void UpdateUI()
    {
        if (PointsManager.Instance == null)
        {
            //Debug.LogWarning("GameStatsUI: PointsManager.Instance is null. Cannot update UI.");
            return;
        }

        // Update Height Text
        if (heightText != null)
        {
            int heightValue = Mathf.RoundToInt(PointsManager.Instance.HighestHeightReached * 5);
            heightText.text = $"{heightValue:N0}";
        }

        // Update Coins Text
        if (coinsText != null)
        {
            coinsText.text = $"{PointsManager.Instance.CoinsCollected:N0}";
        }

        // Update Gems Text
        if (gemsText != null)
        {
            gemsText.text = $"{PointsManager.Instance.GemsCollected}";
        }

        // Update Time Text
        if (timeText != null)
        {
            timeText.text = $"{PointsManager.Instance.SessionDuration:F1}";
        }

        // Update Level Text (New)
        if (levelText != null)
        {
            levelText.text = $"{PointsManager.Instance.CurrentLevel}";
        }
    }
}
