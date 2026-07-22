using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Three-phase first-run tutorial orchestrator for the Tutorial scene.
/// Phase 1: movement (left/right input + left/right velocity).
/// Then panels 2 + 3 show back-to-back (wall tip, then collect tip) with no play between.
/// After panel 3: bounce both walls, then the collectible activates.
/// </summary>
public class TutorialController : MonoBehaviour
{
    public static TutorialController Instance { get; private set; }

    public enum Phase
    {
        Movement,
        WallBounce,
        Collectible
    }

    [Header("UI")]
    [Tooltip("Root panel for phase 1 tutorial image (shown paused until Continue).")]
    public GameObject phase1Panel;
    [Tooltip("Root panel for phase 2 tutorial image.")]
    public GameObject phase2Panel;
    [Tooltip("Root panel for phase 3 tutorial image.")]
    public GameObject phase3Panel;
    [Tooltip("Play/Continue button on phase 1 panel.")]
    public Button phase1ContinueButton;
    [Tooltip("Play/Continue button on phase 2 panel.")]
    public Button phase2ContinueButton;
    [Tooltip("Play/Continue button on phase 3 panel.")]
    public Button phase3ContinueButton;

    [Header("Scene refs")]
    public PlayerBallController player;
    public SideWall leftWall;
    public SideWall rightWall;
    [Tooltip("Collectible that stays inactive until phase 3 starts.")]
    public GameObject collectibleObject;

    [Header("Timing")]
    public float phaseTransitionDelay = 3f;
    public float collectCompleteDelay = 1f;
    [Tooltip("Ignore progress briefly after dismissing an image so the Continue tap is not counted.")]
    public float postDismissGraceSeconds = 0.2f;
    public float movementVelocityThreshold = 0.5f;

    [Header("Wall tint guide")]
    [Tooltip("How strongly the hinted wall darkens (0 = none, 1 = full black).")]
    [Range(0f, 1f)]
    public float wallTintStrength = 0.28f;

    [Header("Completion")]
    public string firstTowerSceneName = "BasicTower";

    Phase currentPhase = Phase.Movement;
    bool waitingForContinue;
    bool trackingProgress;
    bool phaseCompletePending;
    bool tutorialFinished;
    bool wallTintGuideActive;

    bool pressedLeft;
    bool pressedRight;
    bool movedLeft;
    bool movedRight;
    bool hitLeftWall;
    bool hitRightWall;

    /// <summary>Wall that currently has the hint tint; null means neither.</summary>
    SideWall.WallSide? tintedWallSide;
    /// <summary>Last wall the player hit while the guide is active; used to ignore repeat hits.</summary>
    SideWall.WallSide? lastHitWallForTint;

    SpriteRenderer leftWallRenderer;
    SpriteRenderer rightWallRenderer;
    Color leftWallBaseColor = Color.white;
    Color rightWallBaseColor = Color.white;

    float progressGateTime;
    float savedTimeScale = 1f;

    void Awake()
    {
        Instance = this;

        if (collectibleObject != null)
            collectibleObject.SetActive(false);

        HideAllPanels();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        UnsubscribeGameplay();
        UnbindContinueButtons();
        ClearWallTints();
    }

    void Start()
    {
        if (player == null)
            player = PlayerBallController.Instance;

        CacheWallRenderers();
        EnsureCollectibleSetup();
        ResolveContinueButtons();
        BindContinueButtons();

        SubscribeGameplay();
        StartCoroutine(BeginPhaseSequence());
    }

    void CacheWallRenderers()
    {
        if (leftWall != null)
        {
            leftWallRenderer = leftWall.GetComponent<SpriteRenderer>();
            if (leftWallRenderer == null)
                leftWallRenderer = leftWall.GetComponentInChildren<SpriteRenderer>();
            if (leftWallRenderer != null)
                leftWallBaseColor = leftWallRenderer.color;
        }

        if (rightWall != null)
        {
            rightWallRenderer = rightWall.GetComponent<SpriteRenderer>();
            if (rightWallRenderer == null)
                rightWallRenderer = rightWall.GetComponentInChildren<SpriteRenderer>();
            if (rightWallRenderer != null)
                rightWallBaseColor = rightWallRenderer.color;
        }
    }

    void ResolveContinueButtons()
    {
        if (phase1ContinueButton == null && phase1Panel != null)
            phase1ContinueButton = phase1Panel.GetComponentInChildren<Button>(true);
        if (phase2ContinueButton == null && phase2Panel != null)
            phase2ContinueButton = phase2Panel.GetComponentInChildren<Button>(true);
        if (phase3ContinueButton == null && phase3Panel != null)
            phase3ContinueButton = phase3Panel.GetComponentInChildren<Button>(true);
    }

    void BindContinueButtons()
    {
        if (phase1ContinueButton != null)
            phase1ContinueButton.onClick.AddListener(OnContinueClicked);
        if (phase2ContinueButton != null)
            phase2ContinueButton.onClick.AddListener(OnContinueClicked);
        if (phase3ContinueButton != null)
            phase3ContinueButton.onClick.AddListener(OnContinueClicked);
    }

    void UnbindContinueButtons()
    {
        if (phase1ContinueButton != null)
            phase1ContinueButton.onClick.RemoveListener(OnContinueClicked);
        if (phase2ContinueButton != null)
            phase2ContinueButton.onClick.RemoveListener(OnContinueClicked);
        if (phase3ContinueButton != null)
            phase3ContinueButton.onClick.RemoveListener(OnContinueClicked);
    }

    /// <summary>
    /// Tutorial scene uses a Diamond/coin prefab that has GemCollectable/CoinCollectable.
    /// Those despawn without notifying us, so swap in TutorialCollectible.
    /// </summary>
    void EnsureCollectibleSetup()
    {
        if (collectibleObject == null)
            return;

        DisableIfPresent(collectibleObject.GetComponent<GemCollectable>());
        DisableIfPresent(collectibleObject.GetComponent<CoinCollectable>());
        DisableIfPresent(collectibleObject.GetComponent<Collectable>());
        DisableIfPresent(collectibleObject.GetComponent<PowerupCollectable>());
        DisableIfPresent(collectibleObject.GetComponent<CollectableDistanceDespawn>());

        if (collectibleObject.GetComponent<TutorialCollectible>() == null)
            collectibleObject.AddComponent<TutorialCollectible>();
    }

    static void DisableIfPresent(Behaviour behaviour)
    {
        if (behaviour != null)
            behaviour.enabled = false;
    }

    void Update()
    {
        if (tutorialFinished || waitingForContinue || !trackingProgress)
            return;

        if (Time.unscaledTime < progressGateTime)
            return;

        if (currentPhase == Phase.Movement)
            TrackMovementProgress();
    }

    void SubscribeGameplay()
    {
        PlayerBallController.OnDirectionalInput += HandleDirectionalInput;
        SideWall.OnPlayerHit += HandleWallHit;
    }

    void UnsubscribeGameplay()
    {
        PlayerBallController.OnDirectionalInput -= HandleDirectionalInput;
        SideWall.OnPlayerHit -= HandleWallHit;
    }

    IEnumerator BeginPhaseSequence()
    {
        // Show the first prompt immediately while the player's start delay is paused.
        yield return ShowPhaseImageAndWait(Phase.Movement);
        trackingProgress = true;
    }

    IEnumerator ShowPhaseImageAndWait(Phase phase)
    {
        currentPhase = phase;
        trackingProgress = false;
        waitingForContinue = true;
        phaseCompletePending = false;

        HideAllPanels();
        GameObject panel = GetPanelForPhase(phase);
        if (panel != null)
            panel.SetActive(true);

        Button continueButton = GetContinueButtonForPhase(phase);
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.interactable = true;
        }

        PauseGameplay();

        while (waitingForContinue)
            yield return null;

        ResumeGameplay();
        HideAllPanels();

        progressGateTime = Time.unscaledTime + postDismissGraceSeconds;
    }

    void OnContinueClicked()
    {
        if (!waitingForContinue)
            return;

        waitingForContinue = false;
    }

    void HandleDirectionalInput(float direction)
    {
        if (tutorialFinished || waitingForContinue || !trackingProgress)
            return;
        if (currentPhase != Phase.Movement)
            return;
        if (Time.unscaledTime < progressGateTime)
            return;

        if (direction < 0f)
            pressedLeft = true;
        else if (direction > 0f)
            pressedRight = true;

        TryCompleteMovementPhase();
    }

    void TrackMovementProgress()
    {
        if (player == null || player.Rigidbody == null)
            return;

        float vx = player.Rigidbody.velocity.x;
        if (vx < -movementVelocityThreshold)
            movedLeft = true;
        else if (vx > movementVelocityThreshold)
            movedRight = true;

        TryCompleteMovementPhase();
    }

    void TryCompleteMovementPhase()
    {
        if (phaseCompletePending)
            return;

        if (pressedLeft && pressedRight && movedLeft && movedRight)
        {
            phaseCompletePending = true;
            trackingProgress = false;
            StartCoroutine(ShowPanel2ThenPanel3ThenWalls());
        }
    }

    void HandleWallHit(SideWall.WallSide side)
    {
        if (tutorialFinished || waitingForContinue)
            return;
        if (Time.unscaledTime < progressGateTime)
            return;

        // Tint guide runs during wall practice and stays on through collectible phase.
        if (wallTintGuideActive)
            ApplyWallTintGuideOnHit(side);

        if (!trackingProgress || phaseCompletePending)
            return;
        if (currentPhase != Phase.WallBounce)
            return;

        if (side == SideWall.WallSide.Left)
            hitLeftWall = true;
        else if (side == SideWall.WallSide.Right)
            hitRightWall = true;

        if (hitLeftWall && hitRightWall)
        {
            phaseCompletePending = true;
            trackingProgress = false;
            ActivateCollectiblePhase();
        }
    }

    /// <summary>
    /// After movement: wait, show panel 2, then on Play show panel 3 immediately (no gameplay between).
    /// After panel 3 Play: track wall bounces with tint guide, then enable the collectible.
    /// </summary>
    IEnumerator ShowPanel2ThenPanel3ThenWalls()
    {
        yield return new WaitForSeconds(phaseTransitionDelay);
        yield return ShowPhaseImageAndWait(Phase.WallBounce);
        // Consecutive: panel 3 opens right after panel 2's Play, with no practice in between.
        yield return ShowPhaseImageAndWait(Phase.Collectible);

        currentPhase = Phase.WallBounce;
        trackingProgress = true;
        phaseCompletePending = false;
        progressGateTime = Time.unscaledTime + postDismissGraceSeconds;

        // Wall practice starts here (phase after panel 3): begin alternating tint guide.
        EnableWallTintGuide();
    }

    void ActivateCollectiblePhase()
    {
        currentPhase = Phase.Collectible;
        phaseCompletePending = false;
        trackingProgress = true;
        progressGateTime = Time.unscaledTime + postDismissGraceSeconds;

        // Keep / ensure tint guide is active for the whole collectible phase.
        if (!wallTintGuideActive)
            EnableWallTintGuide();

        if (collectibleObject != null)
            collectibleObject.SetActive(true);
    }

    void EnableWallTintGuide()
    {
        wallTintGuideActive = true;
        lastHitWallForTint = null;
        tintedWallSide = null;
        ApplyWallColors();
    }

    void ApplyWallTintGuideOnHit(SideWall.WallSide hitSide)
    {
        // Hitting the same wall again does not change the hint.
        if (lastHitWallForTint.HasValue && lastHitWallForTint.Value == hitSide)
            return;

        lastHitWallForTint = hitSide;
        SideWall.WallSide opposite = hitSide == SideWall.WallSide.Left
            ? SideWall.WallSide.Right
            : SideWall.WallSide.Left;
        SetTintedWall(opposite);
    }

    void SetTintedWall(SideWall.WallSide sideToTint)
    {
        tintedWallSide = sideToTint;
        ApplyWallColors();
    }

    void ClearWallTints()
    {
        wallTintGuideActive = false;
        tintedWallSide = null;
        lastHitWallForTint = null;
        ApplyWallColors();
    }

    void ApplyWallColors()
    {
        if (leftWallRenderer != null)
        {
            leftWallRenderer.color = tintedWallSide == SideWall.WallSide.Left
                ? Color.Lerp(leftWallBaseColor, Color.black, wallTintStrength)
                : leftWallBaseColor;
        }

        if (rightWallRenderer != null)
        {
            rightWallRenderer.color = tintedWallSide == SideWall.WallSide.Right
                ? Color.Lerp(rightWallBaseColor, Color.black, wallTintStrength)
                : rightWallBaseColor;
        }
    }

    /// <summary>Called by <see cref="TutorialCollectible"/> when the player picks up the item.</summary>
    public void NotifyCollectibleCollected()
    {
        if (tutorialFinished || currentPhase != Phase.Collectible)
            return;
        if (waitingForContinue)
            return;

        tutorialFinished = true;
        trackingProgress = false;
        StartCoroutine(FinishTutorial());
    }

    IEnumerator FinishTutorial()
    {
        ClearWallTints();

        // Unscaled wait so a frozen timeScale cannot block scene load.
        Time.timeScale = 1f;
        yield return new WaitForSecondsRealtime(collectCompleteDelay);

        GameSaveService.SetTutorialCompleted(true);
        Time.timeScale = 1f;
        PersistentLoader.ResetForRestart();

        string sceneToLoad = string.IsNullOrEmpty(firstTowerSceneName) ? "BasicTower" : firstTowerSceneName;
        SceneManager.LoadScene(sceneToLoad);
    }

    void PauseGameplay()
    {
        savedTimeScale = Time.timeScale;
        if (savedTimeScale <= 0f)
            savedTimeScale = 1f;
        Time.timeScale = 0f;
    }

    void ResumeGameplay()
    {
        Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;
    }

    void HideAllPanels()
    {
        if (phase1Panel != null) phase1Panel.SetActive(false);
        if (phase2Panel != null) phase2Panel.SetActive(false);
        if (phase3Panel != null) phase3Panel.SetActive(false);
    }

    GameObject GetPanelForPhase(Phase phase)
    {
        switch (phase)
        {
            case Phase.Movement: return phase1Panel;
            case Phase.WallBounce: return phase2Panel;
            case Phase.Collectible: return phase3Panel;
            default: return null;
        }
    }

    Button GetContinueButtonForPhase(Phase phase)
    {
        switch (phase)
        {
            case Phase.Movement: return phase1ContinueButton;
            case Phase.WallBounce: return phase2ContinueButton;
            case Phase.Collectible: return phase3ContinueButton;
            default: return null;
        }
    }
}
