using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectDestroyMonitor : MonoBehaviour
{
    public static ObjectDestroyMonitor Instance;

    [SerializeField] private Transform target;
    [SerializeField] private float destroyThreshold = 20f;
    [SerializeField] private float checkInterval = 0.5f;
    [SerializeField] private Camera mainCamera;

    private List<GameObject> platforms = new List<GameObject>();
    private List<GameObject> collectibles = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        StartCoroutine(CheckDestroyConditionsCoroutine());
    }

    public void AddPlatform(GameObject platform)
    {
        if (!platforms.Contains(platform)) platforms.Add(platform);
    }

    public void AddCollectible(GameObject collectible)
    {
        if (!collectibles.Contains(collectible)) collectibles.Add(collectible);
    }

    private IEnumerator CheckDestroyConditionsCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            if (target != null && target.position.y > destroyThreshold)
            {
                CheckCollectibles();
                CheckPlatforms();
            }
        }
    }

    private void CheckCollectibles()
    {
        for (int i = collectibles.Count - 1; i >= 0; i--)
        {
            GameObject obj = collectibles[i];
            if (obj == null || !IsVisible(obj))
            {
                if (obj != null) Destroy(obj);
                collectibles.RemoveAt(i);
            }
        }
    }

    private void CheckPlatforms()
    {
        for (int i = platforms.Count - 1; i >= 0; i--)
        {
            GameObject obj = platforms[i];
            if (obj == null)
            {
                platforms.RemoveAt(i);
                continue;
            }

            if (!IsVisible(obj))
            {
                Destroy(obj);
                platforms.RemoveAt(i);
            }
            else if (target != null && obj.transform.position.y < target.position.y)
            {
                var behavior = obj.GetComponent<MonoBehaviour>();
                if (behavior != null)
                {
                    behavior.StartCoroutine("DestroyPlatformWithShake");
                }
                platforms.RemoveAt(i);
            }
        }
    }

    //IS ABOVE CAMERAS bottom edge Y OR NOT 
    private bool IsVisible(GameObject obj)
    {
        if (mainCamera == null) return false;

        // Get the camera's orthographic size
        float cameraBottom = mainCamera.transform.position.y - mainCamera.orthographicSize;

        // Check if the object's Y position is below the camera's bottom border in world space
        return obj.transform.position.y > cameraBottom;
    }


}
