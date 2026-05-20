using System;
using UnityEngine;

/// <summary>Tracks pool ownership so instances return to the correct prefab pool instead of Destroy.</summary>
public class PooledInstance : MonoBehaviour
{
    GameObject sourcePrefab;
    Action<GameObject, GameObject> releaseHandler;

    public GameObject SourcePrefab => sourcePrefab;

    public void Initialize(GameObject prefab, Action<GameObject, GameObject> onRelease)
    {
        sourcePrefab = prefab;
        releaseHandler = onRelease;
    }

    public void Release()
    {
        if (sourcePrefab == null || releaseHandler == null)
        {
            Destroy(gameObject);
            return;
        }

        releaseHandler(sourcePrefab, gameObject);
    }

    public static void ReleaseOrDestroy(GameObject target)
    {
        if (target == null)
            return;

        PooledInstance pooled = target.GetComponent<PooledInstance>();
        if (pooled != null)
            pooled.Release();
        else
            Destroy(target);
    }
}
