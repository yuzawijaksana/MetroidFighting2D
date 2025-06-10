using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class GlobalSettings : MonoBehaviour
{
    public static GlobalSettings Instance { get; private set; }

    [Header("Game Settings")]
    public float globalVolume = 1.0f;
    public bool debugMode = false;

    [Header("Slow Motion Settings")]
    public float slowMotionFactor = 0.5f;
    private bool isSlowMotionActive = false;

    [Header("Hit Pause Settings")]
    public float defaultHitPauseDuration = 0.7f;
    private bool isHitPaused = false;

    [Header("Pixel Game Speed")]
    [SerializeField] private float pixelGameTimeScale = 1f; // 1 = normal, <1 = slower, >1 = faster

    private bool isFrameByFramePaused = false;
    private bool advanceOneFrame = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Time.timeScale = pixelGameTimeScale;
        Time.fixedDeltaTime = 0.03f * Time.timeScale;
    }

    private void Update()
    {
        // Toggle frame-by-frame pause with backquote `
        if (Keyboard.current.backquoteKey.wasPressedThisFrame)
        {
            isFrameByFramePaused = !isFrameByFramePaused;
            if (isFrameByFramePaused)
            {
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = pixelGameTimeScale;
            }
        }

        // Advance one frame with period key (>)
        if (isFrameByFramePaused && Keyboard.current.periodKey.wasPressedThisFrame)
        {
            advanceOneFrame = true;
        }

        if (Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
            ToggleSlowMotion();
        }
    }

    private void LateUpdate()
    {
        // If advancing one frame, set timeScale to 1 for one frame, then back to 0
        if (isFrameByFramePaused && advanceOneFrame)
        {
            StartCoroutine(AdvanceOneFrame());
            advanceOneFrame = false;
        }
    }

    private IEnumerator AdvanceOneFrame()
    {
        Time.timeScale = 1f;
        yield return null; // Wait one frame
        Time.timeScale = 0f;
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
    }

    public bool IsSlowMotionActive()
    {
        return isSlowMotionActive;
    }

    // New method to trigger hit pause
    public void TriggerHitPause(float duration = -1f)
    {
        if (isFrameByFramePaused && !advanceOneFrame) // Don't do hit pause if already fully paused by frame-by-frame, unless advancing
        {
            return;
        }

        if (!isHitPaused) // Only start if not already in a hit pause
        {
            StartCoroutine(HitPauseCoroutine(duration >= 0 ? duration : defaultHitPauseDuration));
        }
    }

    // New coroutine to handle the hit pause effect
    private IEnumerator HitPauseCoroutine(float duration)
    {
        isHitPaused = true;
        float originalTimeScale = Time.timeScale;
        
        // If frame-by-frame is active, originalTimeScale would be 0.
        // We want hitpause to effectively "pause" the game at its current speed, then restore.
        // So, if originalTimeScale is 0 due to frame-by-frame, we still set it to 0 for hitpause.
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(duration); // Wait using unscaled time

        // Restore to the time scale that was active before this hitpause,
        // unless frame-by-frame pause wants it to stay 0.
        if (isFrameByFramePaused && !advanceOneFrame)
        {
            Time.timeScale = 0f; // Frame-by-frame pause overrides and keeps it at 0
        }
        else
        {
            Time.timeScale = originalTimeScale; // Restore to whatever it was
        }
        
        isHitPaused = false;
    }
}
