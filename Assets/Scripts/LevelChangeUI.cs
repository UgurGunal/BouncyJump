using UnityEngine;
using TMPro;
using System.Collections;

public class LevelChangeUI : MonoBehaviour
{
    public static LevelChangeUI Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI levelText;

    [Header("Animation Settings")]
    [Tooltip("Total time visible per level change (fade in + hold + fade out).")]
    public float totalDisplayDuration = 2f;
    public float fadeInDuration = 0.2f;
    public float fadeOutDuration = 0.2f;

    [Header("Text Settings")]
    public string levelTextFormat = "LEVEL {0}";

    public bool IsShowing { get; private set; }

    private void Awake()
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

        if (levelText == null)
            levelText = GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        if (levelText != null)
            SetLevelTextAlpha(Color.white, 0f);
    }

    public void ShowLevelChange(int level)
    {
        ShowLevelChange(level, Color.white);
    }

    public void ShowLevelChange(int level, Color textColor)
    {
        StopAllCoroutines();
        StartCoroutine(AnimateLevelChange(level, textColor));
    }

    private IEnumerator AnimateLevelChange(int level, Color textColor)
    {
        IsShowing = true;

        float fadeIn = Mathf.Max(0f, fadeInDuration);
        float fadeOut = Mathf.Max(0f, fadeOutDuration);
        float total = Mathf.Max(0f, totalDisplayDuration);
        float hold = Mathf.Max(0f, total - fadeIn - fadeOut);

        if (levelText != null)
        {
            levelText.text = string.Format(levelTextFormat, level);
            SetLevelTextAlpha(textColor, 0f);
        }

        if (levelText != null && fadeIn > 0f)
        {
            float elapsedTime = 0f;
            while (elapsedTime < fadeIn)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsedTime / fadeIn);
                SetLevelTextAlpha(textColor, alpha);
                yield return null;
            }
        }

        SetLevelTextAlpha(textColor, 1f);

        if (hold > 0f)
            yield return new WaitForSeconds(hold);

        if (levelText != null && fadeOut > 0f)
        {
            float elapsedTime = 0f;
            while (elapsedTime < fadeOut)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOut);
                SetLevelTextAlpha(textColor, alpha);
                yield return null;
            }
        }

        SetLevelTextAlpha(textColor, 0f);
        IsShowing = false;
    }

    void SetLevelTextAlpha(Color rgb, float alpha)
    {
        if (levelText == null) return;
        Color c = new Color(rgb.r, rgb.g, rgb.b, alpha);
        levelText.color = c;
    }
}
