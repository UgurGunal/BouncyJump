using System.Collections;
using UnityEngine;

/// <summary>
/// Runs chest coin arc on the collectable itself so chest StopAllCoroutines cannot leave colliders disabled.
/// </summary>
public class ChestCollectableLaunch : MonoBehaviour
{
    Coroutine launchRoutine;

    public void BeginLaunch(
        Vector3 startPosition,
        Vector3 targetPosition,
        float launchDuration = 0.9f,
        float colliderEnableDelay = 0.9f,
        float arcHeight = 2.6f)
    {
        StopLaunch();
        transform.position = startPosition;
        launchRoutine = StartCoroutine(LaunchSequence(
            startPosition,
            targetPosition,
            Mathf.Max(0.01f, launchDuration),
            Mathf.Max(0f, colliderEnableDelay),
            arcHeight));
    }

    public void StopLaunch()
    {
        if (launchRoutine != null)
        {
            StopCoroutine(launchRoutine);
            launchRoutine = null;
        }
    }

    void OnDisable()
    {
        StopLaunch();
        EnsureColliderEnabled();
    }

    public static void EnsureColliderEnabled(GameObject collectable)
    {
        if (collectable == null)
            return;

        Collider2D collider = collectable.GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = true;
    }

    void EnsureColliderEnabled()
    {
        EnsureColliderEnabled(gameObject);
    }

    IEnumerator LaunchSequence(Vector3 startPos, Vector3 endPos, float launchDuration, float colliderEnableDelay, float arcHeight)
    {
        Collider2D coinCollider = GetComponent<Collider2D>();
        if (coinCollider != null)
            coinCollider.enabled = false;

        bool colliderEnabled = false;
        float elapsed = 0f;

        try
        {
            Vector3 horizontalPos = startPos;

            while (elapsed < launchDuration)
            {
                float progress = Mathf.Clamp01(elapsed / launchDuration);

                float speedCurve;
                if (progress < 0.3f)
                    speedCurve = Mathf.Pow(progress / 0.3f, 1.5f) * 0.3f;
                else if (progress < 0.7f)
                    speedCurve = 0.3f + Mathf.Pow((progress - 0.3f) / 0.4f, 0.7f) * 0.4f;
                else
                    speedCurve = 0.7f + Mathf.Pow((progress - 0.7f) / 0.3f, 0.6f) * 0.3f;

                horizontalPos = Vector3.Lerp(startPos, endPos, speedCurve);
                float height = Mathf.Sin(speedCurve * Mathf.PI) * arcHeight;
                horizontalPos.y += height;
                transform.position = horizontalPos;

                if (!colliderEnabled && elapsed >= colliderEnableDelay)
                {
                    EnsureColliderEnabled();
                    colliderEnabled = true;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Vector3 finalPosition = horizontalPos;
            finalPosition.x = Mathf.Clamp(finalPosition.x, -1.5f, 1.5f);
            transform.position = finalPosition;
        }
        finally
        {
            if (this != null && gameObject != null)
            {
                if (!colliderEnabled)
                    EnsureColliderEnabled();

                CollectableSpawnHelper.EnableDistanceDestroy(gameObject);
            }
        }
    }
}
