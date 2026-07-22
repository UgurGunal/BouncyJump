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
    [Tooltip("Optional: best height ever for the tower you played (persisted). Same number scale as max height (×5). Leave unassigned if unused.")]
    public TextMeshProUGUI towerBestHeightText;
    public TextMeshProUGUI maxLevelText;
    public TextMeshProUGUI totalEarnedCoinsText;

    [Header("Endgame count sequence")]
    [Tooltip("Fixed order: coins → diamonds → max level → max height → tower best (only if new record) → (if level bonus) glow then coins to total. Uses unscaled time.")]
    public bool enableEndgameCountAnimations = true;
    [Tooltip("Count duration for session coins (skipped if coins are 0).")]
    public float countDurationCoins = 0.9f;
    [Tooltip("Count duration for gems (skipped if gems are 0).")]
    public float countDurationDiamonds = 0.9f;
    [Tooltip("Count duration for X 1 → X max level (skipped if max level is 1).")]
    public float countDurationMaxLevel = 0.9f;
    [Tooltip("Count duration for height (skipped if displayed height is 0).")]
    public float countDurationMaxHeight = 0.9f;
    [Tooltip("Count duration for tower best height (only when this run sets a new per-tower record).")]
    public float countDurationTowerBestHeight = 0.9f;
    [Tooltip("Count duration for coins rising to total earned after level bonus (skipped if no bonus).")]
    public float countDurationCoinLevelBonus = 0.9f;
    [Tooltip("Pause (unscaled seconds) only after a step that ran a count animation, before the next step. Skipped when a step only sets text (e.g. coins = 0).")]
    public float pauseBetweenEndgameCountSteps = 0.2f;
    [Tooltip("Wait (unscaled seconds) before the first count step (session coins). 0 = no lead-in.")]
    public float waitBeforeFirstEndgameCountStep = 0f;
    [Tooltip("When the displayed number difference for a step is exactly 1, count duration is multiplied by this (e.g. 0.5 = half duration).")]
    [Range(0.05f, 1f)]
    public float countDurationScaleWhenDifferenceIsOne = 0.5f;
    [Tooltip("When difference is exactly 2, count duration is multiplied by this (default 2/3).")]
    [Range(0.05f, 1f)]
    public float countDurationScaleWhenDifferenceIsTwo = 2f / 3f;

    [Header("Max Level Glow Effect (Optional)")]
    [Tooltip("Animate max level text Glow Outer from min to max and back.")]
    public bool animateMaxLevelGlow = false;
    [Tooltip("Minimum Glow Outer value.")]
    public float maxLevelGlowOuterMin = 0f;
    [Tooltip("Maximum Glow Outer value.")]
    public float maxLevelGlowOuterMax = 0.7f;
    [Tooltip("Delay before starting the glow pulse (seconds, unscaled time).")]
    public float maxLevelGlowStartDelay = 1f;
    [Tooltip("Duration of one full glow pulse (min -> max -> min), in seconds.")]
    public float maxLevelGlowPulseDuration = 1.5f;
    [Tooltip("Minimum scale multiplier for max level text during the glow pulse.")]
    public float maxLevelScaleMin = 1f;
    [Tooltip("Maximum scale multiplier for max level text during the glow pulse.")]
    public float maxLevelScaleMax = 1.1f;

    [Header("Count Sound (SoundEffectsManager)")]
    [Tooltip("Master switch: play endgame count ticks during number animations (see repeat rules on each stat).")]
    public bool playCountSoundDuringNumberAnimations = true;
    [Tooltip("Only play when (display end - display start) >= this. E.g. 2 skips X 1→2 (span 1). 0 = no span filter.")]
    public int minCountSpanForEndgameSound = 0;
    public bool playCountSoundForCoins = true;
    public bool playCountSoundForDiamonds = true;
    public bool playCountSoundForMaxHeight = true;
    public bool playCountSoundForTowerBestHeight = true;
    public bool playCountSoundForMaxLevel = false;
    [Range(0f, 1f)]
    [Tooltip("Volume while counting; 0 means full volume (1).")]
    public float countSoundVolume = 1f;

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
            return;
        }

        if (panelObject != null)
            panelObject.SetActive(false);
    }

    void Start()
    {
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClick);
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClick);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnRestartClick); // Both restart and quit reload the scene
    }

    public void ShowGameEndPanel()
    {
        if (panelObject == null)
            return;

        // Prevent overlapping coroutines if the panel is opened multiple times.
        StopAllCoroutines();

        panelObject.SetActive(true);
        if (contentContainer != null)
            contentContainer.transform.localScale = Vector3.zero;
        StartCoroutine(ScaleAnimation());
        PopulateStats();

        if (MusicManager.Instance != null)
            MusicManager.Instance.StopMusic();

        if (PausePanelUI.Instance != null)
            PausePanelUI.Instance.SetPauseOpenAllowed(false);

        // Time.timeScale should already be 0f from RevivePanelUI
    }

    private IEnumerator ScaleAnimation()
    {
        if (contentContainer == null)
            yield break;

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
        StopAllCoroutines();
        if (panelObject != null)
            panelObject.SetActive(false);
    }

    private IEnumerator AnimateMaxLevelGlow()
    {
        // Run one full pulse only: min -> max -> min.
        if (panelObject == null || !panelObject.activeInHierarchy || maxLevelText == null)
            yield break;

        float minGlow = Mathf.Min(maxLevelGlowOuterMin, maxLevelGlowOuterMax);
        float maxGlow = Mathf.Max(maxLevelGlowOuterMin, maxLevelGlowOuterMax);
        float minScale = Mathf.Min(maxLevelScaleMin, maxLevelScaleMax);
        float maxScale = Mathf.Max(maxLevelScaleMin, maxLevelScaleMax);
        float startDelay = Mathf.Max(0f, maxLevelGlowStartDelay);
        float pulseDuration = Mathf.Max(0.01f, maxLevelGlowPulseDuration);
        Vector3 originalScale = maxLevelText.rectTransform.localScale;

        if (startDelay > 0f)
            yield return new WaitForSecondsRealtime(startDelay);

        float elapsed = 0f;
        while (elapsed < pulseDuration && panelObject.activeInHierarchy && maxLevelText != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / pulseDuration);

            // 0->1->0 curve over a single cycle.
            float pingPong = 1f - Mathf.Abs((progress * 2f) - 1f);
            float glowOuter = Mathf.Lerp(minGlow, maxGlow, pingPong);
            maxLevelText.fontMaterial.SetFloat(ShaderUtilities.ID_GlowOuter, glowOuter);
            float scaleMultiplier = Mathf.Lerp(minScale, maxScale, pingPong);
            maxLevelText.rectTransform.localScale = originalScale * scaleMultiplier;
            yield return null;
        }

        if (maxLevelText != null)
        {
            maxLevelText.fontMaterial.SetFloat(ShaderUtilities.ID_GlowOuter, minGlow);
            maxLevelText.rectTransform.localScale = originalScale * minScale;
        }
    }

    void PopulateStats()
    {
        if (PointsManager.Instance == null)
        {
            return;
        }

        // Display collected coins and diamonds
        int coinsCollected = PointsManager.Instance.CoinsCollected;
        int gemsCollected = PointsManager.Instance.GemsCollected;
        
        // Display max reached height this session (multiplied by 5 as per your UI format)
        int displayHeight = Mathf.RoundToInt(PointsManager.Instance.HighestHeightReached * 5);
        if (LevelManager.Instance != null)
        {
            int maxDisplayHeight = Mathf.RoundToInt(LevelManager.Instance.GetMaxTowerWorldY() * 5);
            displayHeight = Mathf.Min(displayHeight, maxDisplayHeight);
        }

        // Per-tower best height (persisted): read old best, then save if this run beat the record
        int towerIndex = TowerHeightHighScore.GetCurrentTowerIndexFromSave();
        int previousBestTowerDisplay = TowerHeightHighScore.GetBestDisplayHeight(towerIndex);
        bool isNewTowerBest = TowerHeightHighScore.TryRecordHeight(towerIndex, PointsManager.Instance.HighestHeightReached);
        int bestTowerDisplayHeight = TowerHeightHighScore.GetBestDisplayHeight(towerIndex);

        // Display max reached level (1-based), capped to this tower's configured level count
        int maxReachedLevel = 1;
        if (LevelManager.Instance != null)
        {
            maxReachedLevel = LevelManager.Instance.GetCurrentLevel(PointsManager.Instance.HighestHeightReached);
            maxReachedLevel = LevelManager.Instance.ClampLevel(maxReachedLevel);
        }

        // Display total earned coins (max level * coins collected)
        int totalEarnedCoins = 0;
        if (LevelManager.Instance != null)
        {
            totalEarnedCoins = maxReachedLevel * PointsManager.Instance.CoinsCollected;
        }
        
        // Accumulate this session's currency into secure save
        if (PointsManager.Instance != null)
        {
            PointsManager.Instance.AccumulateSessionCurrency();
        }

        if (towerBestHeightText != null)
        {
            if (enableEndgameCountAnimations && isNewTowerBest)
                towerBestHeightText.text = previousBestTowerDisplay.ToString("N0");
            else
                towerBestHeightText.text = bestTowerDisplayHeight.ToString("N0");
        }

        if (totalEarnedCoinsText != null)
            totalEarnedCoinsText.text = totalEarnedCoins.ToString("N0");

        if (enableEndgameCountAnimations)
            StartCoroutine(RunEndgameStatsCountSequence(coinsCollected, gemsCollected, displayHeight, maxReachedLevel, totalEarnedCoins, isNewTowerBest, previousBestTowerDisplay, bestTowerDisplayHeight));
        else
            ApplyAllEndgameStatsFinalTexts(coinsCollected, gemsCollected, displayHeight, maxReachedLevel, totalEarnedCoins, bestTowerDisplayHeight);
    }

    private void ApplyAllEndgameStatsFinalTexts(int coinsCollected, int gemsCollected, int displayHeight, int maxReachedLevel, int totalEarnedCoins, int bestTowerDisplayHeight)
    {
        if (coinsText != null)
            coinsText.text = totalEarnedCoins.ToString();
        if (totalDiamondsText != null)
            totalDiamondsText.text = gemsCollected.ToString();
        if (maxLevelText != null)
            maxLevelText.text = $"X {maxReachedLevel}";
        if (maxHeightText != null)
            maxHeightText.text = displayHeight.ToString("N0");
        if (towerBestHeightText != null)
            towerBestHeightText.text = bestTowerDisplayHeight.ToString("N0");
    }

    private IEnumerator RunEndgameStatsCountSequence(int coinsCollected, int gemsCollected, int displayHeight, int maxReachedLevel, int totalEarnedCoins, bool isNewTowerBest, int previousBestTowerDisplay, int bestTowerDisplayHeight)
    {
        if (waitBeforeFirstEndgameCountStep > 0f)
            yield return new WaitForSecondsRealtime(waitBeforeFirstEndgameCountStep);

        bool playedCountAnim = false;

        // 1) Coins (skip count if 0)
        if (coinsText != null)
        {
            if (coinsCollected > 0)
            {
                yield return AnimateIntText(coinsText, coinsCollected, false, "", 0,
                    ShouldPlayEndgameCountSound(playCountSoundForCoins, 0, coinsCollected),
                    GetEffectiveCountDurationForDifference(countDurationCoins, coinsCollected));
                playedCountAnim = true;
            }
            else
                coinsText.text = "0";
        }

        if (playedCountAnim && pauseBetweenEndgameCountSteps > 0f)
            yield return new WaitForSecondsRealtime(pauseBetweenEndgameCountSteps);

        playedCountAnim = false;

        // 2) Diamonds (skip if 0)
        if (totalDiamondsText != null)
        {
            if (gemsCollected > 0)
            {
                yield return AnimateIntText(totalDiamondsText, gemsCollected, false, "", 0,
                    ShouldPlayEndgameCountSound(playCountSoundForDiamonds, 0, gemsCollected),
                    GetEffectiveCountDurationForDifference(countDurationDiamonds, gemsCollected));
                playedCountAnim = true;
            }
            else
                totalDiamondsText.text = "0";
        }

        if (playedCountAnim && pauseBetweenEndgameCountSteps > 0f)
            yield return new WaitForSecondsRealtime(pauseBetweenEndgameCountSteps);

        playedCountAnim = false;

        // 3) Max level (skip if level is 1 — no span)
        if (maxLevelText != null)
        {
            if (maxReachedLevel > 1)
            {
                int levelSpan = maxReachedLevel - 1;
                yield return AnimateIntText(maxLevelText, maxReachedLevel, false, "X ", 1,
                    ShouldPlayEndgameCountSound(playCountSoundForMaxLevel, 1, maxReachedLevel),
                    GetEffectiveCountDurationForDifference(countDurationMaxLevel, levelSpan));
                playedCountAnim = true;
            }
            else
                maxLevelText.text = $"X {maxReachedLevel}";
        }

        if (playedCountAnim && pauseBetweenEndgameCountSteps > 0f)
            yield return new WaitForSecondsRealtime(pauseBetweenEndgameCountSteps);

        playedCountAnim = false;

        // 4) Max height (skip if 0)
        if (maxHeightText != null)
        {
            if (displayHeight > 0)
            {
                yield return AnimateIntText(maxHeightText, displayHeight, true, "", 0,
                    ShouldPlayEndgameCountSound(playCountSoundForMaxHeight, 0, displayHeight),
                    GetEffectiveCountDurationForDifference(countDurationMaxHeight, displayHeight));
                playedCountAnim = true;
            }
            else
                maxHeightText.text = "0";
        }

        if (playedCountAnim && pauseBetweenEndgameCountSteps > 0f)
            yield return new WaitForSecondsRealtime(pauseBetweenEndgameCountSteps);

        playedCountAnim = false;

        // 4b) Tower best height — count only when this session set a new per-tower record
        if (towerBestHeightText != null && isNewTowerBest)
        {
            int towerBestSpan = bestTowerDisplayHeight - previousBestTowerDisplay;
            if (towerBestSpan > 0)
            {
                yield return AnimateIntText(towerBestHeightText, bestTowerDisplayHeight, true, "", previousBestTowerDisplay,
                    ShouldPlayEndgameCountSound(playCountSoundForTowerBestHeight, previousBestTowerDisplay, bestTowerDisplayHeight),
                    GetEffectiveCountDurationForDifference(countDurationTowerBestHeight, towerBestSpan));
                playedCountAnim = true;
            }
            else
                towerBestHeightText.text = bestTowerDisplayHeight.ToString("N0");
        }

        if (playedCountAnim && pauseBetweenEndgameCountSteps > 0f)
            yield return new WaitForSecondsRealtime(pauseBetweenEndgameCountSteps);

        // 5) Level bonus: glow then coins → total earned (only if multiplier > 1 and total differs)
        if (maxReachedLevel > 1 && coinsText != null && totalEarnedCoins > coinsCollected)
        {
            if (animateMaxLevelGlow && maxLevelText != null)
                yield return StartCoroutine(AnimateMaxLevelGlow());

            bool bumpSound = ShouldPlayEndgameCountSound(playCountSoundForCoins, coinsCollected, totalEarnedCoins);
            int coinBumpSpan = totalEarnedCoins - coinsCollected;
            yield return AnimateIntText(coinsText, totalEarnedCoins, false, "", coinsCollected, bumpSound,
                GetEffectiveCountDurationForDifference(countDurationCoinLevelBonus, coinBumpSpan));
        }
    }

    /// <summary>
    /// Uses configured duration, or scales when end − start is 1 or 2 (see inspector scale fields).
    /// </summary>
    private float GetEffectiveCountDurationForDifference(float configuredDuration, int difference)
    {
        float d = Mathf.Max(0.01f, configuredDuration);
        if (difference == 2)
            return Mathf.Max(0.01f, d * countDurationScaleWhenDifferenceIsTwo);
        if (difference == 1)
            return Mathf.Max(0.01f, d * countDurationScaleWhenDifferenceIsOne);
        return d;
    }

    private bool ShouldPlayEndgameCountSound(bool enabledForThisStat, int startValue, int targetValue)
    {
        if (!playCountSoundDuringNumberAnimations || !enabledForThisStat)
            return false;
        if (SoundEffectsManager.Instance == null || !SoundEffectsManager.Instance.HasEndgameCountdownClip())
            return false;

        int s = Mathf.Max(0, startValue);
        int t = Mathf.Max(s, targetValue);
        int span = t - s;
        return span >= Mathf.Max(0, minCountSpanForEndgameSound);
    }

    /// <summary>
    /// Sound ticks: span 1..4 → 1..4 times; span &gt; 4 → max(5, floor(log2(span))).
    /// Ticks are spread evenly across the step duration.
    /// </summary>
    private static int GetEndgameCountSoundRepeatCount(int span)
    {
        if (span <= 0) return 0;
        if (span <= 4) return span;
        int log2Floor = Mathf.FloorToInt(Mathf.Log(span, 2f));
        return Mathf.Max(4, log2Floor);
    }

    /// <param name="soundRepeatSpanOverride">If &gt;= 0, tick count uses this span instead of (target − start). Otherwise uses the animated numeric span.</param>
    private IEnumerator AnimateIntText(TextMeshProUGUI tmp, int targetValue, bool useThousandsSeparator, string prefix, int startValue, bool useEndgameCountSound, float duration, int soundRepeatSpanOverride = -1)
    {
        if (tmp == null) yield break;

        int safeStart = Mathf.Max(0, startValue);
        int safeTarget = Mathf.Max(safeStart, targetValue);

        tmp.text = prefix + (useThousandsSeparator ? safeStart.ToString("N0") : safeStart.ToString());

        int span = safeTarget - safeStart;
        int spanForSound = soundRepeatSpanOverride >= 0 ? soundRepeatSpanOverride : span;
        int repeats = useEndgameCountSound ? GetEndgameCountSoundRepeatCount(spanForSound) : 0;

        duration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        int soundsPlayed = 0;
        float interval = repeats > 0 ? duration / repeats : duration;
        float vol = countSoundVolume > 0f ? countSoundVolume : -1f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            int current = Mathf.RoundToInt(Mathf.Lerp(safeStart, safeTarget, t));
            tmp.text = prefix + (useThousandsSeparator ? current.ToString("N0") : current.ToString());

            while (soundsPlayed < repeats && elapsed >= soundsPlayed * interval)
            {
                SoundEffectsManager.Instance?.PlayEndgameCountdownOneShot(vol);
                soundsPlayed++;
            }

            yield return null;
        }

        tmp.text = prefix + (useThousandsSeparator ? safeTarget.ToString("N0") : safeTarget.ToString());

        while (soundsPlayed < repeats)
        {
            SoundEffectsManager.Instance?.PlayEndgameCountdownOneShot(vol);
            soundsPlayed++;
        }
    }

    void OnMainMenuClick()
    {
        HideGameEndPanel();
        Time.timeScale = 1f; // Resume time before loading new scene
        
        // Reset the persistent loader flag since we're leaving the game
        PersistentLoader.ResetForRestart();
        
        SceneManager.LoadScene("HomeScene"); // Load the HomeScene
    }

    void OnRestartClick()
    {
        HideGameEndPanel();
        Time.timeScale = 1f; // Resume time before loading new scene

        if (PointsManager.Instance != null)
            PointsManager.Instance.StartSession();

        // Music restarts once when the scene finishes loading (MusicManager.OnSceneLoaded).
        // Reset the persistent loader flag and reload scene
        PersistentLoader.ResetForRestart();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
