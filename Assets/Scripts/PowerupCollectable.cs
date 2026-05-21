using UnityEngine;

public class PowerupCollectable : MonoBehaviour
{
    public float yDestroyOffset = 10f;
    public float powerupDuration = 5f;
    public float powerupPerSecond = 100f;

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
            PointsManager.Instance.AddPowerup();

        if (ComboManager.Instance != null)
            ComboManager.Instance.ApplyComboPowerup(powerupDuration, powerupPerSecond);

        PooledInstance.ReleaseOrDestroy(gameObject);
    }
}
