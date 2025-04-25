using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameObject mainGround;
    public Transform target;

    public float towerHeight = 3000f; // Height limit for tower generation
    private bool gameHasEnded = false;

    // First platform attributes
    private GameObject firstPlatformShortPrefab;
    private GameObject firstPlatformLongPrefab;
    private float firstPlatformMinXScale;
    private float firstPlatformMaxXScale;
    private float firstPlatformYScale;
    private float firstPlatformSpawnRate;

    // Second platform attributes
    private GameObject secondPlatformPrefab;
    private float secondPlatformMinXScale;
    private float secondPlatformMaxXScale;
    private float secondPlatformYScale;
    private float secondPlatformSpawnRate;

    private float firstPlatformProbability;
    private float powerUpSpawnRate;
    private int levelChangeThreshold;

    public GameObject powerUpPrefab;

    public Transform generationReference; // Reference to control spawn range

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        levelChangeThreshold = LevelManager.Instance.nextLevelThreshold;
        instantiatePlatformsAndPowerUps();
    }

    void Update()
    {
        if (target != null && mainGround != null && target.position.y > 20f)
        {
            Destroy(mainGround, 5f);
        }
    }

    public void instantiatePlatformsAndPowerUps()
    {
        StartCoroutine(GeneratePlatformsCoroutine());
    }

    private IEnumerator GeneratePlatformsCoroutine()
    {
        Vector3 spawnPosition = new Vector3(0f, -4.5f, 0f);
        Vector3 lastPlatformPosition = spawnPosition;
        int lastLevel = -1;
        int currentLevel = 0;

        // Generate platforms until the tower height limit is reached
        while (spawnPosition.y <= towerHeight)
        {
            // Only proceed if within the range of the generationReference
            if (generationReference != null && generationReference.position.y + 20f >= spawnPosition.y)
            {
                spawnPosition.y += Random.Range(4.5f, 5.5f);
                spawnPosition.x = Random.Range(-4f, 4f);

                currentLevel = Mathf.Max(((int)spawnPosition.y / levelChangeThreshold), 0);
                if (currentLevel != lastLevel)
                {
                    SetLevelSettings(currentLevel);
                }
                lastLevel = currentLevel;

                GameObject selectedPlatformPrefab;
                float scaleX;
                float scaleY;

                if (Random.Range(0f, 1f) < firstPlatformProbability)
                {
                    bool useShort = Random.value < 0.5f;
                    selectedPlatformPrefab = useShort ? firstPlatformShortPrefab : firstPlatformLongPrefab;
                    scaleX = Random.Range(firstPlatformMinXScale, firstPlatformMaxXScale);
                    scaleY = firstPlatformYScale;
                }
                else
                {
                    selectedPlatformPrefab = secondPlatformPrefab;
                    scaleX = Random.Range(secondPlatformMinXScale, secondPlatformMaxXScale);
                    scaleY = secondPlatformYScale;
                }

                GameObject platform = Instantiate(selectedPlatformPrefab, spawnPosition, Quaternion.identity);
                ObjectDestroyMonitor.Instance.AddPlatform(platform);
                platform.transform.localScale = new Vector3(scaleX, scaleY, 1);
                platform.transform.SetParent(transform);

                if (powerUpPrefab != null && Random.Range(0f, 1f) < powerUpSpawnRate)
                {
                    SpawnPowerUp(lastPlatformPosition, spawnPosition);
                }

                lastPlatformPosition = spawnPosition;
            }

            yield return null; // Wait for the next frame to prevent blocking
        }
    }

    private void SpawnPowerUp(Vector3 lastPlatformPosition, Vector3 currentPlatformPosition)
    {
        Vector3 powerUpPosition = new Vector3(
            Random.Range(-3.8f, 3.8f),
            (lastPlatformPosition.y + currentPlatformPosition.y) / 2,
            0f
        );

        GameObject powerUp = Instantiate(powerUpPrefab, powerUpPosition, Quaternion.identity);
        ObjectDestroyMonitor.Instance.AddCollectible(powerUp);
    }

    public void SetLevelSettings(int currentLevelIndex)
    {

        var currentLevelConfig = LevelManager.Instance.levels[currentLevelIndex];

        // First platform attributes
        firstPlatformShortPrefab = currentLevelConfig.firstPlatformConfig.platformShortPrefab;
        firstPlatformLongPrefab = currentLevelConfig.firstPlatformConfig.platformLongPrefab;
        firstPlatformMinXScale = currentLevelConfig.firstPlatformConfig.platformMinXScale;
        firstPlatformMaxXScale = currentLevelConfig.firstPlatformConfig.platformMaxXScale;
        firstPlatformYScale = currentLevelConfig.firstPlatformConfig.platformYScale;

        // Second platform attributes
        secondPlatformPrefab = currentLevelConfig.secondPlatformConfig.platformPrefab;
        secondPlatformMinXScale = currentLevelConfig.secondPlatformConfig.platformMinXScale;
        secondPlatformMaxXScale = currentLevelConfig.secondPlatformConfig.platformMaxXScale;
        secondPlatformYScale = currentLevelConfig.secondPlatformConfig.platformYScale;

        // Spawn probabilities
        firstPlatformSpawnRate = currentLevelConfig.firstPlatformConfig.platformSpawnRate;
        secondPlatformSpawnRate = currentLevelConfig.secondPlatformConfig.platformSpawnRate;
        firstPlatformProbability = firstPlatformSpawnRate / (firstPlatformSpawnRate + secondPlatformSpawnRate);

        // Power-up spawn rate
        powerUpSpawnRate = currentLevelConfig.powerUpSpawnRate;
    }

    public void endGame()
    {
        if (!gameHasEnded)
        {
            gameHasEnded = true;
            Debug.Log("GAME OVER");
            restartGame();
        }
    }

    public void restartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}