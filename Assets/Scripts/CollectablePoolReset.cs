using UnityEngine;

/// <summary>Restores collectable physics/collider state when reused from the object pool.</summary>
public static class CollectablePoolReset
{
    public static void PrepareForSpawn(GameObject collectable)
    {
        if (collectable == null)
            return;

        ChestCollectableLaunch launch = collectable.GetComponent<ChestCollectableLaunch>();
        if (launch != null)
            launch.StopLaunch();

        ChestCollectableLaunch.EnsureColliderEnabled(collectable);
    }

    public static void PrepareForPool(GameObject collectable)
    {
        if (collectable == null)
            return;

        ChestCollectableLaunch launch = collectable.GetComponent<ChestCollectableLaunch>();
        if (launch != null)
            launch.StopLaunch();

        ChestCollectableLaunch.EnsureColliderEnabled(collectable);
        CollectableSpawnHelper.SetDistanceDestroySuppressed(collectable, false);
    }
}
