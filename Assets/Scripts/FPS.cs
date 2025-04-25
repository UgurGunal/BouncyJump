using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPS : MonoBehaviour
{
    private float fps;
    public TMPro.TextMeshProUGUI fpsCounterText;
    // Start is called before the first frame update
    void Start()
    {
        InvokeRepeating("GetFPS", 1, 1);
    }

    void GetFPS()
    {
        fps = (int)(1f / Time.unscaledDeltaTime);
        fpsCounterText.text = "FPS: " + fps.ToString();
    }
    
}
