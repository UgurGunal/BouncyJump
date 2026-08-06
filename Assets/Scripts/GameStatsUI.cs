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
    private int _lastHeight = int.MinValue;
    private int _lastCoins = int.MinValue;
    private int _lastGems = int.MinValue;
    private int _lastLevel = int.MinValue;
    private string _lastTimeText;
    private string _lastLevelText;

    void OnEnable()
    {
        // Force one apply so default-off matches hidden HUD on first run.
        _lastShowRunTimer = !GameplayDisplaySettings.ShowRunTimer;
        _lastHeight = int.MinValue;
        _lastCoins = int.MinValue;
        _lastGems = int.MinValue;
        _lastLevel = int.MinValue;
        _lastTimeText = null;
        _lastLevelText = null;
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
            if (heightValue != _lastHeight)
            {
                _lastHeight = heightValue;
                heightText.text = $"{heightValue:N0}";
            }
        }

        // Update Coins Text
        if (coinsText != null)
        {
            int coins = PointsManager.Instance.CoinsCollected;
            if (coins != _lastCoins)
            {
                _lastCoins = coins;
                coinsText.text = $"{coins:N0}";
            }
        }

        // Update Gems Text
        if (gemsText != null)
        {
            int gems = PointsManager.Instance.GemsCollected;
            if (gems != _lastGems)
            {
                _lastGems = gems;
                gemsText.text = $"{gems}";
            }
        }

        ApplyRunTimerVisibility();

        if (GameplayDisplaySettings.ShowRunTimer && timeText != null)
        {
            string timeString = $"{PointsManager.Instance.SessionDuration:F1}";
            if (timeString != _lastTimeText)
            {
                _lastTimeText = timeString;
                timeText.text = timeString;
            }
        }

        if (levelText != null && !IsLevelChangePopupActive())
        {
            int level = PointsManager.Instance.CurrentLevel;
            if (level != _lastLevel)
            {
                _lastLevel = level;
                string levelString = string.Format(levelTextFormat, level);
                if (levelString != _lastLevelText)
                {
                    _lastLevelText = levelString;
                    levelText.text = levelString;
                }
            }
        }
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
