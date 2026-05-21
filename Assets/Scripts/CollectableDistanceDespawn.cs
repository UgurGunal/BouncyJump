using System.Collections;
using UnityEngine;

/// <summary>
/// Despawns collectables when the player is far above their spawn/landing height (not a timer).
/// Chest loot defers checks until <see cref="CollectableSpawnHelper.EnableDistanceDestroy"/>.
/// </summary>
public class CollectableDistanceDespawn : MonoBehaviour
{
    public float yDestroyOffset = 10f;

    Transform playerTransform;
    bool suppressDistanceDestroy;
    float destroyReferenceY;
    Coroutine distanceCheckRoutine;

    public void SetDistanceDestroySuppressed(bool suppressed)
    {
        suppressDistanceDestroy = suppressed;
        if (suppressed)
            StopDistanceCheck();
    }

    public void EnableDistanceDestroyCheck()
    {
        suppressDistanceDestroy = false;
        if (!isActiveAndEnabled)
            return;

        destroyReferenceY = transform.position.y;
        StopDistanceCheck();
        distanceCheckRoutine = StartCoroutine(CheckDistanceToPlayer());
    }

    void OnEnable()
    {
        EnsurePlayerReference();
        if (suppressDistanceDestroy)
            return;

        destroyReferenceY = transform.position.y;
        StopDistanceCheck();
        distanceCheckRoutine = StartCoroutine(CheckDistanceToPlayer());
    }

    void OnDisable()
    {
        StopDistanceCheck();
        suppressDistanceDestroy = false;
    }

    void EnsurePlayerReference()
    {
        if (playerTransform != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            playerTransform = playerObject.transform;
    }

    void StopDistanceCheck()
    {
        if (distanceCheckRoutine != null)
        {
            StopCoroutine(distanceCheckRoutine);
            distanceCheckRoutine = null;
        }
    }

    IEnumerator CheckDistanceToPlayer()
    {
        yield return null;

        while (isActiveAndEnabled)
        {
            if (!suppressDistanceDestroy
                && playerTransform != null
                && playerTransform.position.y - destroyReferenceY > yDestroyOffset)
            {
                PooledInstance.ReleaseOrDestroy(gameObject);
                yield break;
            }

            yield return new WaitForSeconds(1f);
        }
    }
}
