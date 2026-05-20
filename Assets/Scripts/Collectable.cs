using UnityEngine;
using System.Collections;

public class Collectable : MonoBehaviour
{
    public float yDestroyOffset = 10f;
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
            PooledInstance.ReleaseOrDestroy(gameObject);
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
