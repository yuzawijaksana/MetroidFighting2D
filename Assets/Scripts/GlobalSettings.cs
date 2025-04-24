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
    public float defaultHitPauseDuration = 0.1f;
    private bool isHitPaused = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
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

        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = originalTimeScale;
        isHitPaused = false;
    }
}
