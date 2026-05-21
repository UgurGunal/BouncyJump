using UnityEngine;

public class GemCollectable : MonoBehaviour
{
    public int gemValue = 1;
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
        if (!other.CompareTag("Player"))
            return;

        if (PointsManager.Instance != null)
            PointsManager.Instance.AddGem(gemValue);

        if (SoundEffectsManager.Instance != null)
            SoundEffectsManager.Instance.PlayDiamondSound(-1f);

        PooledInstance.ReleaseOrDestroy(gameObject);
    }
}
