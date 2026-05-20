using UnityEngine;
using TMPro;
using System.Collections;

public class LevelChangeUI : MonoBehaviour
{
    public static LevelChangeUI Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI levelText;

    [Header("Animation Settings")]
    public float fadeInDuration = 0.5f;
    public float displayDuration = 2f;
    public float fadeOutDuration = 0.5f;

    [Header("Text Settings")]
    public string levelTextFormat = "LEVEL {0}";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Ensure the text is hidden at start
        if (levelText != null)
        {
            levelText.alpha = 0f;
        }
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
        if (levelText != null)
        {
            levelText.text = string.Format(levelTextFormat, level);
            SetLevelTextAlpha(textColor, 0f);
        }

        // Fade in
        if (levelText != null)
        {
            float elapsedTime = 0f;
            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
                SetLevelTextAlpha(textColor, alpha);
                yield return null;
            }
            SetLevelTextAlpha(textColor, 1f);
        }

        yield return new WaitForSeconds(displayDuration);

        // Fade out
        if (levelText != null)
        {
            float elapsedTime = 0f;
            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
                SetLevelTextAlpha(textColor, alpha);
                yield return null;
            }
            SetLevelTextAlpha(textColor, 0f);
        }
    }

    void SetLevelTextAlpha(Color rgb, float alpha)
    {
        if (levelText == null) return;
        levelText.color = new Color(rgb.r, rgb.g, rgb.b, alpha);
    }
}
