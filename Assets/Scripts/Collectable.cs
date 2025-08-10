using UnityEngine;
using System.Collections;

public class Collectable : MonoBehaviour
{
    public float yDestroyOffset = 10f;
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
            Destroy(gameObject);
        }
    }

    private IEnumerator CheckDistanceToPlayer()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            if (playerTransform != null && playerTransform.position.y - transform.position.y > yDestroyOffset)
            {
                Destroy(gameObject);
            }
        }
    }
}
