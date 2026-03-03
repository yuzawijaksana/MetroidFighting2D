using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private CinemachineVirtualCamera virtualCamera;
    private CinemachineBasicMultiChannelPerlin perlinNoise;

    private void Awake()
    {
        Instance = this;
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        if (virtualCamera != null)
        {
            perlinNoise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        }
    }

    public void Shake(float intensity, float duration)
    {
        if (perlinNoise == null) return;
        perlinNoise.AmplitudeGain = intensity;
        StartCoroutine(StopShake(duration));
    }

    private IEnumerator StopShake(float duration)
    {
        yield return new WaitForSecondsRealtime(duration); // Use Realtime to work with hit pause
        perlinNoise.AmplitudeGain = 0f;
    }
}