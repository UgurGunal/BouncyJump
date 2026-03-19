using UnityEngine;
using System.Collections;

public class CoinCollectable : MonoBehaviour
{
    public int coinValue = 1; // Default value for the coin
    public float yDestroyOffset = 10f; // Offset for destruction below player
    private Transform playerTransform;

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            StartCoroutine(CheckDistanceToPlayer());
        }
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
            
            Destroy(gameObject);
        }
    }

    private IEnumerator CheckDistanceToPlayer()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f); // Check every second

            if (playerTransform != null && playerTransform.position.y - transform.position.y > yDestroyOffset)
            {
                Destroy(gameObject);
            }
        }
    }
}