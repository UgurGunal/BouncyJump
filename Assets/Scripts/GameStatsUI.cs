using UnityEngine;
using TMPro; // Assuming you are using TextMeshPro for UI text

public class GameStatsUI : MonoBehaviour
{
    [Header("UI Text References (Optional)")]
    public TextMeshProUGUI heightText;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI gemsText;
    public TextMeshProUGUI timeText;
    [Tooltip("Optional: hide the whole timer row (icon + label). If empty, only timeText is shown/hidden.")]
    public GameObject timeDisplayRoot;
    public TextMeshProUGUI levelText; // New: Reference for Level Text

    [Header("Level Display")]
    [Tooltip("HUD level label format. {0} = current level number (e.g. Level {0} -> Level 1).")]
    public string levelTextFormat = "Level {0}";

    [Header("Update Settings")]
    public float updateInterval = 0.1f; // How often to update the UI (e.g., 10 times per second)

    private float _lastUpdateTime;
    private bool _lastShowRunTimer;

    void OnEnable()
    {
        // Force one apply so default-off matches hidden HUD on first run.
        _lastShowRunTimer = !GameplayDisplaySettings.ShowRunTimer;
        ApplyRunTimerVisibility();
    }

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

        ApplyRunTimerVisibility();

        if (GameplayDisplaySettings.ShowRunTimer && timeText != null)
            timeText.text = $"{PointsManager.Instance.SessionDuration:F1}";

        if (levelText != null && !IsLevelChangePopupActive())
            levelText.text = string.Format(levelTextFormat, PointsManager.Instance.CurrentLevel);
    }

    static bool IsLevelChangePopupActive()
    {
        return LevelChangeUI.Instance != null
            && LevelChangeUI.Instance.IsShowing
            && LevelChangeUI.Instance.levelText != null;
    }

    void ApplyRunTimerVisibility()
    {
        bool show = GameplayDisplaySettings.ShowRunTimer;
        if (show == _lastShowRunTimer)
            return;

        _lastShowRunTimer = show;

        if (timeDisplayRoot != null)
            timeDisplayRoot.SetActive(show);
        else if (timeText != null)
            timeText.gameObject.SetActive(show);
    }
}
