using UnityEngine;

public class Collectable : MonoBehaviour
{
    public float yDestroyOffset = 10f;

    CollectableDistanceDespawn distanceDespawn;

    void Awake()
    {
        distanceDespawn = GetComponent<CollectableDistanceDespawn>();
        if (distanceDespawn == null)
            distanceDespawn = gameObject.AddComponent<CollectableDistanceDespawn>();

        distanceDespawn.yDestroyOffset = yDestroyOffset;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            PooledInstance.ReleaseOrDestroy(gameObject);
    }
}
