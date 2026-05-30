using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Keeps disabled UI buttons fully opaque and applies a subtle dark tint instead of fading out.
/// Applied automatically to every Button when scenes load; add this component manually to override the tint.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class ButtonDisabledStyle : MonoBehaviour
{
    public static readonly Color DefaultDisabledTint = new Color(0.68f, 0.68f, 0.68f, 1f);

    [SerializeField] Color disabledTint = DefaultDisabledTint;

    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        Apply();
    }

    public void Apply()
    {
        if (button == null)
            button = GetComponent<Button>();
        if (button == null)
            return;

        ColorBlock colors = button.colors;
        colors.disabledColor = disabledTint;
        button.colors = colors;
    }

    public static void Apply(Button target, Color? tintOverride = null)
    {
        if (target == null)
            return;

        Color tint = tintOverride ?? DefaultDisabledTint;
        ColorBlock colors = target.colors;
        colors.disabledColor = tint;
        target.colors = colors;
    }

    static bool bootstrapRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RegisterBootstrap()
    {
        if (bootstrapRegistered)
            return;

        bootstrapRegistered = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyToAllButtons();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyToAllButtons();
    }

    static void ApplyToAllButtons()
    {
        Button[] buttons = Object.FindObjectsOfType<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            ButtonDisabledStyle existingStyle = button.GetComponent<ButtonDisabledStyle>();
            if (existingStyle != null)
            {
                existingStyle.Apply();
                continue;
            }

            Apply(button);
        }
    }
}
