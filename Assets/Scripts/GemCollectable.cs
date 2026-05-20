using UnityEngine;
using System.Collections;

public class GemCollectable : MonoBehaviour
{
    public int gemValue = 1; // Default value for the gem
    public float yDestroyOffset = 10f; // Offset for destruction below player
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
                PointsManager.Instance.AddGem(gemValue);
            }

            if (SoundEffectsManager.Instance != null)
                SoundEffectsManager.Instance.PlayDiamondSound(-1f);

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