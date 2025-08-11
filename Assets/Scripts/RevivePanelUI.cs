using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class RevivePanelUI : MonoBehaviour
{
    public static RevivePanelUI Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject panelObject;
    public Button payDiamondsButton;
    public Button watchAdButton;
    public Button exitButton;
    public Slider countdownSlider;
    public TextMeshProUGUI countdownText;

    [Header("Revive Settings")]
    public int diamondsToRevive = 3;
    public float reviveSkipCountDownDuration = 10f;
    public float reviveYOffset = 0f;

    private PlayerBallController _playerController;
    private CameraFollow _cameraFollow;
    private float _currentCountdownTime;

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
        panelObject.SetActive(false);
    }

    void Start()
    {
        payDiamondsButton.onClick.AddListener(OnPayDiamondsClick);
        watchAdButton.onClick.AddListener(OnWatchAdClick);
        exitButton.onClick.AddListener(OnExitClick);

        _playerController = FindObjectOfType<PlayerBallController>();
        _cameraFollow = FindObjectOfType<CameraFollow>();
    }

    public void ShowRevivePanel()
    {
        panelObject.SetActive(true);
        _currentCountdownTime = reviveSkipCountDownDuration;
        UpdateDiamondButtonState();
        StartCoroutine(CountdownCoroutine());
    }

    void HideRevivePanel()
    {
        panelObject.SetActive(false);
        StopAllCoroutines();
    }

    void UpdateDiamondButtonState()
    {
        if (payDiamondsButton != null)
        {
            bool canAfford = PointsManager.Instance.GemsCollected >= diamondsToRevive;
            
        }
    }

    IEnumerator CountdownCoroutine()
    {
        while (_currentCountdownTime > 0)
        {
            _currentCountdownTime -= Time.unscaledDeltaTime;
            if (countdownSlider != null)
            { 
                countdownSlider.value = _currentCountdownTime / reviveSkipCountDownDuration;
            }
            yield return null;
        }
        OnCountdownFinished();
    }

    void OnPayDiamondsClick()
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

    void OnExitClick()
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
        OnExitClick();
    }

    void RevivePlayer()
    {
        if (_playerController != null)
        {
            float reviveY = (reviveYOffset == 0) ? Camera.main.transform.position.y : reviveYOffset;
            Vector3 newPosition = new Vector3(Camera.main.transform.position.x, reviveY, 0);
            _playerController.Revive(newPosition);
        }
        else
        {
            OnExitClick();
        }
    }

    IEnumerator ReviveCountdown()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }

        RevivePlayer();

        int countdown = 3;
        while (countdown > 0)
        {
            if (countdownText != null)
            {
                countdownText.text = countdown.ToString();
            }
            yield return new WaitForSecondsRealtime(1f);
            countdown--;
        }

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
