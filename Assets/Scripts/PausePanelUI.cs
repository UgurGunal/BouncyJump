using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// In-game pause: freezes time, optional music pause; resume with 3-2-1 countdown (unscaled time, like revive);
/// home applies run rewards via <see cref="PointsManager.FinalizeRunRewardsForMenuExit"/> then loads HomeScene.
/// </summary>
public class PausePanelUI : MonoBehaviour
{
    public static PausePanelUI Instance { get; private set; }

    [Header("UI")]
    public GameObject panelObject;
    public Button resumeButton;
    public Button homeButton;
    [Tooltip("Optional: opens pause (same as ShowPausePanel). Assign your HUD pause button here.")]
    public Button pauseOpenButton;

    [Header("Resume countdown (optional)")]
    [Tooltip("Usually placed outside the pause panel so it stays visible after the panel is closed.")]
    public TextMeshProUGUI resumeCountdownText;

    Coroutine _resumeCoroutine;
    bool _pauseOpenAllowed = true;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (panelObject != null)
            panelObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClicked);
        if (homeButton != null)
            homeButton.onClick.AddListener(OnHomeClicked);
        if (pauseOpenButton != null)
            pauseOpenButton.onClick.AddListener(ShowPausePanel);

        SetPauseOpenAllowed(true);
    }

    /// <summary>HUD pause button — off after death until revive finishes or level restarts.</summary>
    public void SetPauseOpenAllowed(bool allowed)
    {
        _pauseOpenAllowed = allowed;

        if (pauseOpenButton != null)
            pauseOpenButton.interactable = allowed;

        if (!allowed && panelObject != null && panelObject.activeSelf)
            panelObject.SetActive(false);
    }

    /// <summary>Opens pause panel and freezes gameplay (Time.timeScale = 0).</summary>
    public void ShowPausePanel()
    {
        if (!_pauseOpenAllowed)
            return;

        if (panelObject == null)
            return;
        if (panelObject.activeSelf)
            return;

        Time.timeScale = 0f;
        if (MusicManager.Instance != null)
            MusicManager.Instance.PauseMusic();
        if (PointsManager.Instance != null)
            PointsManager.Instance.PauseSession();

        panelObject.SetActive(true);
    }

    void OnResumeClicked()
    {
        if (panelObject != null)
            panelObject.SetActive(false);

        if (_resumeCoroutine != null)
            StopCoroutine(_resumeCoroutine);
        _resumeCoroutine = StartCoroutine(ResumeCountdownCoroutine());
    }

    IEnumerator ResumeCountdownCoroutine()
    {
        int countdown = 3;

        if (resumeCountdownText != null)
            resumeCountdownText.gameObject.SetActive(true);

        while (countdown > 0)
        {
            if (resumeCountdownText != null)
                resumeCountdownText.text = countdown.ToString();
            yield return new WaitForSecondsRealtime(1f);
            countdown--;
        }

        if (resumeCountdownText != null)
            resumeCountdownText.gameObject.SetActive(false);

        Time.timeScale = 1f;

        if (MusicManager.Instance != null)
            MusicManager.Instance.ResumeMusic();
        if (PointsManager.Instance != null)
            PointsManager.Instance.ResumeSession();

        _resumeCoroutine = null;
    }

    void OnHomeClicked()
    {
        if (_resumeCoroutine != null)
        {
            StopCoroutine(_resumeCoroutine);
            _resumeCoroutine = null;
        }

        if (panelObject != null)
            panelObject.SetActive(false);

        if (PointsManager.Instance != null)
            PointsManager.Instance.FinalizeRunRewardsForMenuExit();

        Time.timeScale = 1f;

        PersistentLoader.ResetForRestart();
        SceneManager.LoadScene("HomeScene");
    }
}
