using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RevivePanelUI : MonoBehaviour
{
    public static RevivePanelUI Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject panelObject;
    public Button payDiamondsButton;
    public Button watchAdButton;
    public Button exitButton;
    public Slider countdownSlider;

    [Header("Revive Settings")]
    public int diamondsToRevive = 3;
    public float reviveCountdownDuration = 10f;

    private PlayerBallController _playerController;
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
    }

    public void ShowRevivePanel()
    {
        panelObject.SetActive(true);
        _currentCountdownTime = reviveCountdownDuration;
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
                countdownSlider.value = _currentCountdownTime / reviveCountdownDuration;
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
            RevivePlayer();
            HideRevivePanel();
            Time.timeScale = 1f;
        }
    }

    void OnWatchAdClick()
    {
        
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
            _playerController.Revive(transform.position + new Vector3(0, 2f, 0));
        }
        else
        {
            OnExitClick();
        }
    }
}
