using UnityEngine;

/// <summary>Helpers for chest-spawned collectables: defer distance despawn until launch animation finishes.</summary>
public static class CollectableSpawnHelper
{
    public static void SetDistanceDestroySuppressed(GameObject collectable, bool suppressed)
    {
        if (collectable == null)
            return;

        CollectableDistanceDespawn despawn = GetDistanceDespawn(collectable);
        if (despawn != null)
            despawn.SetDistanceDestroySuppressed(suppressed);
    }

    public static void EnableDistanceDestroy(GameObject collectable)
    {
        if (collectable == null)
            return;

        CollectableDistanceDespawn despawn = GetDistanceDespawn(collectable);
        if (despawn != null)
            despawn.EnableDistanceDestroyCheck();
    }

    static CollectableDistanceDespawn GetDistanceDespawn(GameObject collectable)
    {
        CollectableDistanceDespawn despawn = collectable.GetComponent<CollectableDistanceDespawn>();
        if (despawn != null)
            return despawn;

        despawn = collectable.AddComponent<CollectableDistanceDespawn>();

        CoinCollectable coin = collectable.GetComponent<CoinCollectable>();
        if (coin != null)
        {
            despawn.yDestroyOffset = coin.yDestroyOffset;
            return despawn;
        }

        GemCollectable gem = collectable.GetComponent<GemCollectable>();
        if (gem != null)
        {
            despawn.yDestroyOffset = gem.yDestroyOffset;
            return despawn;
        }

        PowerupCollectable powerup = collectable.GetComponent<PowerupCollectable>();
        if (powerup != null)
        {
            despawn.yDestroyOffset = powerup.yDestroyOffset;
            return despawn;
        }

        Collectable generic = collectable.GetComponent<Collectable>();
        if (generic != null)
            despawn.yDestroyOffset = generic.yDestroyOffset;

        return despawn;
    }
}
