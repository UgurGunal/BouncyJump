using UnityEngine;
using System.Collections;

public class CoinCollectable : MonoBehaviour
{
    public int coinValue = 1; // Default value for the coin
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
                PointsManager.Instance.AddCoin(coinValue);
            }
            
            // Play coin collection sound effect with pitch variance
            if (SoundEffectsManager.Instance != null)
            {
                SoundEffectsManager.Instance.PlayCoinSound(-1f);
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