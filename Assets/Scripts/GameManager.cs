using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject basicPlatformPrefab;
    public GameObject bouncyPlatformPrefab;
    public GameObject powerUpPrefab;
    public Transform target;
    public GameObject mainGround;
    public int numberOfPlatforms = 300;
    public float powerUpSpawnRate = 0.02f;

    bool gameHasEnded = false;
    // Start is called before the first frame update
    void Start()
    {
        instantiatePlatformsAndPowerUps();
    }

    // Update is called once per frame
    void Update()
    {
        if (target != null && mainGround != null && target.position.y > 20f)
        {
            Destroy(mainGround, 5f);
        }
    }


    public void instantiatePlatformsAndPowerUps()
    {
        Vector3 spawnPosition = new Vector3(0f, -4.5f, 0f);
        Vector3 lastPlatformPosition = spawnPosition; // Store last platform position

        for (int i = 0; i < numberOfPlatforms; i++)
        {
            spawnPosition.y += Random.Range(4.5f, 5.5f);
            float scaleX;
            float scaleY;
            GameObject platformToSpawn;

            if (Random.Range(0, 11) < 10)
            {
                platformToSpawn = basicPlatformPrefab;
                scaleX = Random.Range(2.8f, 6.4f);
                scaleY = 1;
                spawnPosition.x = Random.Range(-4.4f, 4.4f);
            }
            else
            {
                platformToSpawn = bouncyPlatformPrefab;
                scaleX = Random.Range(3f, 6f);
                scaleY = 0.5f;
                spawnPosition.x = Random.Range(-4f, 4f);
            }

            GameObject instantiated = Instantiate(platformToSpawn, spawnPosition, Quaternion.identity);
            instantiated.transform.localScale = new Vector3(scaleX, scaleY, 1);
            instantiated.transform.SetParent(transform);

            // Random chance to spawn a power-up
            if (Random.Range(0f, 1f) < powerUpSpawnRate) // 5% chance
            {
                Vector3 powerUpPosition = new Vector3(
                    Random.Range(-4.2f, 4.2f), // Random X
                    (lastPlatformPosition.y + spawnPosition.y) / 2, // Y is between last and current platform
                    0f
                );

                Instantiate(powerUpPrefab, powerUpPosition, Quaternion.identity);
            }

            lastPlatformPosition = spawnPosition; // Update last platform position
        }
    }

    public void endGame()
    {
        if(gameHasEnded == false)
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
