using UnityEngine;

/// <summary>
/// Cached player transform for gameplay systems (avoids per-frame FindGameObjectWithTag).
/// </summary>
public static class GameplayPlayerCache
{
    static Transform cachedPlayer;

    public static Transform Player
    {
        get
        {
            if (cachedPlayer != null)
                return cachedPlayer;

            Resolve();
            return cachedPlayer;
        }
    }

    public static void SetPlayer(Transform player)
    {
        cachedPlayer = player;
    }

    public static void Clear()
    {
        cachedPlayer = null;
    }

    static void Resolve()
    {
        if (CrossSceneReferenceManager.Instance != null)
        {
            Transform fromCrossScene = CrossSceneReferenceManager.Instance.GetPlayer();
            if (fromCrossScene != null)
            {
                cachedPlayer = fromCrossScene;
                return;
            }
        }

        if (LevelManager.Instance != null && LevelManager.Instance.player != null)
        {
            cachedPlayer = LevelManager.Instance.player;
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            cachedPlayer = playerObject.transform;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticData()
    {
        cachedPlayer = null;
    }
}
