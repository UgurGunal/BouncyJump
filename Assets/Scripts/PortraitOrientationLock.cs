using UnityEngine;

/// <summary>
/// Keeps the game in upright portrait on phones. Uses AutoRotation (not locked Portrait)
/// so Android picks the correct natural portrait direction on each device.
/// </summary>
public static class PortraitOrientationLock
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply()
    {
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        Screen.orientation = ScreenOrientation.AutoRotation;
    }
}
