using UnityEngine;

public class FPSLimiter : MonoBehaviour
{
    [Range(40, 60)]
    public int targetFPS = 60;  // default

    void Awake()
    {
        // Disable VSync so Application.targetFrameRate works
        QualitySettings.vSyncCount = 0;

        // Set initial target
        Application.targetFrameRate = targetFPS;
    }

    void Update()
    {
        // Clamp between 40 and 60 in case you change it in Inspector
        targetFPS = Mathf.Clamp(targetFPS, 40, 60);

        if (Application.targetFrameRate != targetFPS)
            Application.targetFrameRate = targetFPS;
    }
}
