using UnityEngine;
using UnityEngine.InputSystem; // Use the new Input System
using System.Collections;

public class GlobalSettings : MonoBehaviour
{
    public static GlobalSettings Instance { get; private set; }

    [Header("Game Settings")]
    public float globalVolume = 1.0f; // Example: Global volume setting
    public float globalGravity = 1f;
    public bool debugMode = false; // Example: Enable/disable debug mode

    [Header("Slow Motion Settings")]
    public float slowMotionFactor = 0.5f;
    private bool isSlowMotionActive = false;

    [Header("Hit Pause Settings")]
    public float defaultHitPauseDuration = 0.1f; // Default duration for hit pause
    private bool isHitPaused = false; // Flag to track if the game is currently paused

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Ensure only one instance exists
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scenes
    }

    private void Update()
    {
        // Handle slow motion toggle when BackQuote key is pressed using the new Input System
        if (Keyboard.current.backquoteKey.wasPressedThisFrame)
        {
            ToggleSlowMotion();
        }
    }

    public void ToggleSlowMotion()
    {
        if (isSlowMotionActive)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            isSlowMotionActive = false;
        }
        else
        {
            Time.timeScale = slowMotionFactor;
            Time.fixedDeltaTime = Time.timeScale * 0.02f;
            isSlowMotionActive = true;
        }

        Debug.Log($"Slow motion toggled. Active: {isSlowMotionActive}, Factor: {slowMotionFactor}");
    }

    public bool IsSlowMotionActive()
    {
        return isSlowMotionActive;
    }

    public void TriggerHitPause(float duration = -1f)
    {
        if (!isHitPaused)
        {
            StartCoroutine(HitPause(duration > 0 ? duration : defaultHitPauseDuration));
        }
    }

    private IEnumerator HitPause(float duration)
    {
        isHitPaused = true;

        // Pause the game by setting time scale to 0
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // Wait for the specified duration in real time
        yield return new WaitForSecondsRealtime(duration);

        // Resume the game by restoring the original time scale
        Time.timeScale = originalTimeScale;

        isHitPaused = false; // Reset the flag
    }
}
