using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class RevivePanelUI : MonoBehaviour
{
    public static RevivePanelUI Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject panelObject;
    public GameObject contentContainer; // The content to animate (excludes background)
    public Button pay3DiamondButton; // Pay 3 diamonds to revive
    public Button watchAdButton; // Watch ad to revive
    public Button quitButton; // Quit to game end panel
    public Slider countdownSlider;
    public TextMeshProUGUI countdownText; // Text to display countdown seconds

    [Header("Revive Settings")]
    public int diamondsToRevive = 3;
    public float reviveCountdownDuration = 10f;
    public float reviveYOffset = -0.6f;

    private PlayerBallController _playerController;
    private CameraFollow _cameraFollow;
    private float _currentCountdownTime;

    void Awake()
    {
        panelObject.SetActive(false);
    }

    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("RevivePanelUI Instance set");
        }
        else
        {
            Debug.Log("Duplicate RevivePanelUI found, destroying");
            Destroy(gameObject);
            return;
        }

        pay3DiamondButton.onClick.AddListener(OnPay3DiamondClick);
        watchAdButton.onClick.AddListener(OnWatchAdClick);
        quitButton.onClick.AddListener(OnQuitClick);

        _playerController = FindObjectOfType<PlayerBallController>();
        _cameraFollow = FindObjectOfType<CameraFollow>();
    }

    public void ShowRevivePanel()
    {
        Debug.Log("ShowRevivePanel called");
        panelObject.SetActive(true);
        contentContainer.transform.localScale = Vector3.zero;
        StartCoroutine(ScaleAnimation());
        
        _currentCountdownTime = reviveCountdownDuration;
        UpdateDiamondButtonState();
        StartCoroutine(CountdownCoroutine());
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

    void HideRevivePanel()
    {
        panelObject.SetActive(false);
        StopAllCoroutines();
    }

    void UpdateDiamondButtonState()
    {
        if (pay3DiamondButton != null)
        {
            bool canAfford = PointsManager.Instance.GemsCollected >= diamondsToRevive;
            pay3DiamondButton.interactable = canAfford;
        }
    }

    IEnumerator CountdownCoroutine()
    {
        while (_currentCountdownTime > 0)
        {
            _currentCountdownTime -= Time.unscaledDeltaTime;
            if (countdownSlider != null)
            { 
                countdownSlider.value = _currentCountdownTime / reviveCountdownDuration;
            }
            yield return null;
        }
        OnCountdownFinished();
    }

    void OnPay3DiamondClick()
    {
        if (PointsManager.Instance != null && PointsManager.Instance.GemsCollected >= diamondsToRevive)
        {
            PointsManager.Instance.AddGem(-diamondsToRevive);
            HideRevivePanel();
            StartCoroutine(ReviveCountdown());
        }
    }

    void OnWatchAdClick()
    {
        HideRevivePanel();
        StartCoroutine(ReviveCountdown());
    }

    void OnQuitClick()
    {
        HideRevivePanel();
        if (GameEndPanelUI.Instance != null)
        {
            GameEndPanelUI.Instance.ShowGameEndPanel();
        }
        else
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    void OnCountdownFinished()
    {
        OnQuitClick();
    }

    void RevivePlayer()
    {
        if (_playerController != null)
        {
            float reviveY = Camera.main.transform.position.y + reviveYOffset;
            Vector3 newPosition = new Vector3(Camera.main.transform.position.x, reviveY, 0);
            _playerController.Revive(newPosition);
            
            // Resume the session to continue tracking stats after revive
            if (PointsManager.Instance != null)
            {
                PointsManager.Instance.ResumeSession();
            }
        }
        else
        {
            OnQuitClick();
        }
    }

    IEnumerator ReviveCountdown()
    {
        RevivePlayer();

        int countdown = 3;
        
        // Show countdown text if available
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }
        
        while (countdown > 0)
        {
            // Update countdown text
            if (countdownText != null)
            {
                countdownText.text = countdown.ToString();
            }
            
            yield return new WaitForSecondsRealtime(1f);
            countdown--;
        }
        
        // Hide countdown text when finished
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }

        Time.timeScale = 1f;

        if (_cameraFollow != null)
        {
            _cameraFollow.ResetRestartTrigger();
        }
    }
}
