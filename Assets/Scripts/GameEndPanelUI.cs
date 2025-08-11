using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameEndPanelUI : MonoBehaviour
{
    public static GameEndPanelUI Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject panelObject; // The parent GameObject for the entire panel
    public TextMeshProUGUI totalCoinsText;
    public TextMeshProUGUI totalGemsText;
    public TextMeshProUGUI maxHeightText;
    public TextMeshProUGUI maxLevelText;
    public TextMeshProUGUI totalEarnedCoinsText;
    public Button mainMenuButton;
    public Button restartButton;
    public Button exitGameButton; // Renamed from 'exitButton' to avoid confusion with RevivePanel's exit

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        panelObject.SetActive(false); // Start inactive
    }

    void Start()
    {
        mainMenuButton.onClick.AddListener(OnMainMenuClick);
        restartButton.onClick.AddListener(OnRestartClick);
        exitGameButton.onClick.AddListener(OnRestartClick); // Both restart and exit game reload the scene
    }

    public void ShowGameEndPanel()
    {
        panelObject.SetActive(true);
        PopulateStats();
        // Time.timeScale should already be 0f from RevivePanelUI
    }

    void HideGameEndPanel()
    {
        panelObject.SetActive(false);
    }

    void PopulateStats()
    {
        if (PointsManager.Instance == null)
        {
            
            return;
        }

        totalCoinsText.text = PointsManager.Instance.CoinsCollected.ToString();
        totalGemsText.text = PointsManager.Instance.GemsCollected.ToString();
        maxHeightText.text = PointsManager.Instance.HighestHeightReached.ToString("F2");

        if (LevelManager.Instance != null)
        {
            int maxReachedLevel = Mathf.CeilToInt(PointsManager.Instance.HighestHeightReached / LevelManager.Instance.levelHeight);
            maxLevelText.text = Mathf.Max(1, maxReachedLevel).ToString();
        }
        else
        {
            maxLevelText.text = "1";
        }

        totalEarnedCoinsText.text = PointsManager.Instance.TotalEarnedCoins.ToString();
    }

    void OnMainMenuClick()
    {
        HideGameEndPanel();
        Time.timeScale = 1f; // Resume time before loading new scene
        SceneManager.LoadScene("MainMenu"); // Load the MainMenu scene
    }

    void OnRestartClick()
    {
        HideGameEndPanel();
        Time.timeScale = 1f; // Resume time before loading new scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Reload current scene
    }
}
