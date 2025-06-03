using System.Collections;
using UnityEngine;

public class HitStop : MonoBehaviour
{
    private static bool hitStopActive = false;

    public static bool IsHitStopActive()
    {
        return hitStopActive;
    }

    public void Stop(float duration)
    {
        if (hitStopActive)
            return;
        StartCoroutine(Wait(duration));
    }

    IEnumerator Wait(float duration)
    {
        hitStopActive = true;
        Time.timeScale = 0.0f;
        Debug.Log("[HitStop] Time.timeScale set to 0 (hit pause started)");

        // Prevent hitpause from being interrupted by object disable/destroy
        float timer = 0f;
        while (timer < duration)
        {
            yield return null; // Wait for real frame (not affected by timescale)
            timer += Time.unscaledDeltaTime;
        }

        Time.timeScale = 1.0f;
        hitStopActive = false;
        Debug.Log("[HitStop] Time.timeScale set to 1 (hit pause ended)");
    }
}
