using UnityEngine;

/// <summary>
/// Tutorial-only pickup. Reports collection to <see cref="TutorialController"/> then disables itself.
/// Attach to the collectible GameObject that stays inactive until phase 3.
/// </summary>
public class TutorialCollectible : MonoBehaviour
{
    bool collected;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || !other.CompareTag("Player"))
            return;

        collected = true;

        if (TutorialController.Instance != null)
            TutorialController.Instance.NotifyCollectibleCollected();

        gameObject.SetActive(false);
    }
}
