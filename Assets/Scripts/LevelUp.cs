using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelDisplay : MonoBehaviour
{
    public Transform player; // Assign your player or target here
    public TextMeshProUGUI levelText;   // Assign the UI Text object that will show the level
    public int levelThreshold = 100; // Distance in Y units to level up
    public float displayTime = 2f;   // How long to show the text

    private int currentLevel = 0;
    private bool isShowing = false;

    void Update()
    {
        int calculatedLevel = Mathf.FloorToInt(player.position.y / levelThreshold);

        if (calculatedLevel > currentLevel)
        {
            currentLevel = calculatedLevel;
            StartCoroutine(ShowLevelText(currentLevel));
        }
    }

    private IEnumerator ShowLevelText(int level)
    {
        if (isShowing) yield break;

        isShowing = true;
        level += 1;
        levelText.text = "Level " + level;
        levelText.gameObject.SetActive(true);

        yield return new WaitForSeconds(displayTime);

        levelText.gameObject.SetActive(false);
        isShowing = false;
    }
}
