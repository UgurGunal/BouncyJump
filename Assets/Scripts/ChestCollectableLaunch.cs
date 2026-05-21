using System.Collections;
using UnityEngine;

/// <summary>
/// Runs chest coin arc on the collectable itself so chest StopAllCoroutines cannot leave colliders disabled.
/// </summary>
public class ChestCollectableLaunch : MonoBehaviour
{
    const float MoveDuration = 0.9f;
    const float ArcHeight = 2.6f;

    Coroutine launchRoutine;

    public void BeginLaunch(Vector3 startPosition, Vector3 targetPosition)
    {
        StopLaunch();
        transform.position = startPosition;
        launchRoutine = StartCoroutine(LaunchSequence(startPosition, targetPosition));
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

    IEnumerator LaunchSequence(Vector3 startPos, Vector3 endPos)
    {
        Collider2D coinCollider = GetComponent<Collider2D>();
        if (coinCollider != null)
            coinCollider.enabled = false;

        try
        {
            yield return MoveToPosition(startPos, endPos);
        }
        finally
        {
            if (this != null && gameObject != null)
            {
                EnsureColliderEnabled();
                CollectableSpawnHelper.EnableDistanceDestroy(gameObject);
            }
        }
    }

    IEnumerator MoveToPosition(Vector3 startPos, Vector3 endPos)
    {
        float startTime = Time.time;
        Vector3 horizontalPos = startPos;

        while (Time.time < startTime + MoveDuration)
        {
            float progress = Mathf.Clamp01((Time.time - startTime) / MoveDuration);

            float speedCurve;
            if (progress < 0.3f)
                speedCurve = Mathf.Pow(progress / 0.3f, 1.5f) * 0.3f;
            else if (progress < 0.7f)
                speedCurve = 0.3f + Mathf.Pow((progress - 0.3f) / 0.4f, 0.7f) * 0.4f;
            else
                speedCurve = 0.7f + Mathf.Pow((progress - 0.7f) / 0.3f, 0.6f) * 0.3f;

            horizontalPos = Vector3.Lerp(startPos, endPos, speedCurve);
            float height = Mathf.Sin(speedCurve * Mathf.PI) * ArcHeight;
            horizontalPos.y += height;

            transform.position = horizontalPos;
            yield return null;
        }

        Vector3 finalPosition = horizontalPos;
        finalPosition.x = Mathf.Clamp(finalPosition.x, -1.5f, 1.5f);
        transform.position = finalPosition;
    }
}
