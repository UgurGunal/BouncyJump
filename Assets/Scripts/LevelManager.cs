using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public int levelCount = 4;
    public float levelHeight = 200f;

    public GameObject coin1Prefab;
    public GameObject coin2Prefab;
    public GameObject powerupPrefab;
    public GameObject diamondPrefab;

    [System.Serializable]
    public class LevelData
    {
        public float cameraSpeed = 5f;
        public GameObject longPlatformPrefab;
        public GameObject shortPlatformPrefab;
        public GameObject specialPlatformPrefab;
        public float longPlatformSpawnRate = 1f;
        public float shortPlatformSpawnRate = 1f;
        public float specialPlatformSpawnRate = 1f;
        public float coin1SpawnRate = 1f;
        public float coin2SpawnRate = 1f;
        public float powerupSpawnRate = 1f;
        public float diamondSpawnRate = 1f;
        public float emptySpawnRate = 5f;
    }

    public LevelData[] levels;

    void Start()
    {
        if (PointsManager.Instance != null)
        {
            PointsManager.Instance.StartSession();
        }
    }

    public int GetCurrentLevel(float playerY)
    {
        return Mathf.FloorToInt(playerY / levelHeight);
    }

    public LevelData GetLevelData(int level)
    {
        return levels[Mathf.Clamp(level, 0, levels.Length - 1)];
    }
}