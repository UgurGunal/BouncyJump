using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameEndPanelUI : MonoBehaviour
{
    public static GameEndPanelUI Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject panelObject; // The parent GameObject for the entire panel
    public GameObject contentContainer; // The content to animate (excludes background)
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI totalDiamondsText;
    public TextMeshProUGUI maxHeightText;
    public TextMeshProUGUI maxLevelText;
    public TextMeshProUGUI totalEarnedCoinsText;
    public Button mainMenuButton;
    public Button restartButton;
    public Button quitButton;

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
        quitButton.onClick.AddListener(OnRestartClick); // Both restart and quit reload the scene
    }

    public void ShowGameEndPanel()
    {
        panelObject.SetActive(true);
        contentContainer.transform.localScale = Vector3.zero;
        StartCoroutine(ScaleAnimation());
        PopulateStats();
        // Time.timeScale should already be 0f from RevivePanelUI
    }

    private IEnumerator ScaleAnimation()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            
            // Smooth ease-out curve (starts fast, slows down at the end)
            float smoothProgress = 1f - Mathf.Pow(1f - progress, 3f);
            
            contentContainer.transform.localScale = Vector3.Lerp(startScale, endScale, smoothProgress);
            yield return null;
        }
        
        contentContainer.transform.localScale = endScale;
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

        // Display collected coins and diamonds
        coinsText.text = PointsManager.Instance.CoinsCollected.ToString();
        totalDiamondsText.text = PointsManager.Instance.GemsCollected.ToString();
        
        // Display max reached height (multiplied by 5 as per your UI format)
        int displayHeight = Mathf.RoundToInt(PointsManager.Instance.HighestHeightReached * 5);
        maxHeightText.text = displayHeight.ToString("N0");

        // Display max reached level (1-based)
        if (LevelManager.Instance != null)
        {
            int maxReachedLevel = LevelManager.Instance.GetCurrentLevel(PointsManager.Instance.HighestHeightReached);
            maxLevelText.text = maxReachedLevel.ToString();
        }
        else
        {
            maxLevelText.text = "1";
        }

        // Display total earned coins (max level * coins collected)
        int totalEarnedCoins = 0;
        if (LevelManager.Instance != null)
        {
            int maxReachedLevel = LevelManager.Instance.GetCurrentLevel(PointsManager.Instance.HighestHeightReached);
            totalEarnedCoins = maxReachedLevel * PointsManager.Instance.CoinsCollected;
        }
        totalEarnedCoinsText.text = totalEarnedCoins.ToString("N0");
    }

    void OnMainMenuClick()
    {
        HideGameEndPanel();
        Time.timeScale = 1f; // Resume time before loading new scene
        SceneManager.LoadScene("HomeScene"); // Load the HomeScene
    }

    void OnRestartClick()
    {
        HideGameEndPanel();
        Time.timeScale = 1f; // Resume time before loading new scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Reload current scene
    }
}
