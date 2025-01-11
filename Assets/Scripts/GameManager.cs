using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject platformPrefab;
    public int numberOfPlatforms = 300;
    // Start is called before the first frame update
    void Start()
    {
        Vector3 spawnPosition = new Vector3();
        for (int i = 0; i < numberOfPlatforms; i++)
        {
            spawnPosition.y += Random.Range(5f, 7f);
            spawnPosition.x = Random.Range(-6f, 6f);
            GameObject instantiated = Instantiate(platformPrefab, spawnPosition, Quaternion.identity);
            instantiated.transform.localScale = new Vector3(Random.Range(3f, 8f), 1, 1);
            instantiated.transform.SetParent(transform);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
