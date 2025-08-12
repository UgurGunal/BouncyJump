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
        StopAllCoroutines();
        StartCoroutine(AnimateLevelChange(level));
    }

    private IEnumerator AnimateLevelChange(int level)
    {
        // Set the text
        if (levelText != null)
        {
            levelText.text = string.Format(levelTextFormat, level);
        }

        // Fade in
        if (levelText != null)
        {
            float elapsedTime = 0f;
            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                levelText.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
                yield return null;
            }
            levelText.alpha = 1f;
        }

        // Display duration
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        if (levelText != null)
        {
            float elapsedTime = 0f;
            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                levelText.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
                yield return null;
            }
            levelText.alpha = 0f;
        }
    }
}
