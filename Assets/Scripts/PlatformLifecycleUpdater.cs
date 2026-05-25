using UnityEngine;

/// <summary>
/// Single FixedUpdate loop for all platform/chest distance destroy and fall logic.
/// </summary>
[DisallowMultipleComponent]
public class PlatformLifecycleUpdater : MonoBehaviour
{
    void FixedUpdate()
    {
        Transform player = GameplayPlayerCache.Player;
        if (player == null)
            return;

        Platform.ProcessAllLifecycles(player);
        ChestPlatform.ProcessAllLifecycles(player);
    }
}
