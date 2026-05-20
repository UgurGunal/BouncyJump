using UnityEngine;
using System.Collections;

public class PowerupCollectable : MonoBehaviour
{
    public float yDestroyOffset = 10f; // Offset for destruction below player
    public float powerupDuration = 5f;
    public float powerupPerSecond = 100f;
    private Transform playerTransform;

    void OnEnable()
    {
        EnsurePlayerReference();
        StopAllCoroutines();
        StartCoroutine(CheckDistanceToPlayer());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    void EnsurePlayerReference()
    {
        if (playerTransform != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            playerTransform = playerObject.transform;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (PointsManager.Instance != null)
            {
                PointsManager.Instance.AddPowerup();
            }
            if (ComboManager.Instance != null)
            {
                ComboManager.Instance.ApplyComboPowerup(powerupDuration, powerupPerSecond);
            }
            PooledInstance.ReleaseOrDestroy(gameObject);
        }
    }

    private IEnumerator CheckDistanceToPlayer()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            if (playerTransform != null && playerTransform.position.y - transform.position.y > yDestroyOffset)
                PooledInstance.ReleaseOrDestroy(gameObject);
        }
    }
}