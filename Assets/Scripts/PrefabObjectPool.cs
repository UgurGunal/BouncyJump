using System;
using System.Collections.Generic;
using UnityEngine;

public class PrefabObjectPool
{
    readonly Transform inactiveRoot;
    readonly Dictionary<int, Queue<GameObject>> pools = new Dictionary<int, Queue<GameObject>>();
    readonly Action<GameObject, GameObject> releaseHandler;

    public PrefabObjectPool(Transform inactiveRoot, Action<GameObject, GameObject> onRelease)
    {
        this.inactiveRoot = inactiveRoot;
        releaseHandler = onRelease;
    }

    public GameObject Get(GameObject prefab, Transform parent)
    {
        if (prefab == null)
            return null;

        int key = prefab.GetInstanceID();
        GameObject instance;

        if (pools.TryGetValue(key, out Queue<GameObject> queue) && queue.Count > 0)
        {
            instance = queue.Dequeue();
            instance.transform.SetParent(parent, false);
            instance.SetActive(true);
        }
        else
        {
            instance = UnityEngine.Object.Instantiate(prefab, parent);
            PooledInstance pooled = instance.GetComponent<PooledInstance>();
            if (pooled == null)
                pooled = instance.AddComponent<PooledInstance>();
            pooled.Initialize(prefab, releaseHandler);
        }

        return instance;
    }

    public void Return(GameObject prefab, GameObject instance)
    {
        if (prefab == null || instance == null)
            return;

        instance.SetActive(false);
        instance.transform.SetParent(inactiveRoot, false);

        int key = prefab.GetInstanceID();
        if (!pools.TryGetValue(key, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            pools.Add(key, queue);
        }

        queue.Enqueue(instance);
    }
}
