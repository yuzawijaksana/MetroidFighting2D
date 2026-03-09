using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Input")]
    private GameInputs controls; 

    [Header("Camera References")]
    [SerializeField] private CinemachineCamera cinemachineCamera;
    private CinemachineGroupFraming groupFraming;
    private CinemachineBasicMultiChannelPerlin perlinNoise;

    [Header("Serialized Offsets")]
    [SerializeField] private Vector2 defaultOffset = Vector2.zero; 
    [SerializeField] private float lookDownY = 2.0f;  
    [SerializeField] private float lookUpY = -2.5f;   
    
    [Header("Timing & Smoothness")]
    [SerializeField] private float waitTime = 0.5f; 
    [SerializeField] private float shiftSpeed = 4f; 

    private float holdTimer;
    private float targetY;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        controls = new GameInputs();

        if (cinemachineCamera == null) cinemachineCamera = GetComponent<CinemachineCamera>();
        
        if (cinemachineCamera != null)
        {
            groupFraming = cinemachineCamera.GetComponent<CinemachineGroupFraming>();
            perlinNoise = cinemachineCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
        }
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Start()
    {
        if (groupFraming != null)
        {
            groupFraming.CenterOffset = new Vector3(defaultOffset.x, defaultOffset.y, 0);
        }
        targetY = defaultOffset.y;
    }

    private void Update()
    {
        HandleCameraShift();
    }

    private void HandleCameraShift()
    {
        if (groupFraming == null) return;

        Vector2 moveValue = controls.Player.Movement.ReadValue<Vector2>();

        // HOLLOW KNIGHT LOGIC: 
        // 1. If we are moving horizontally (X != 0), cancel looking immediately.
        // 2. If we are NOT holding Up/Down (Y is near 0), reset.
        if (Mathf.Abs(moveValue.x) > 0.1f || Mathf.Abs(moveValue.y) < 0.7f) 
        {
            holdTimer = 0;
            targetY = defaultOffset.y;
        }
        else 
        {
            // Player is standing still horizontally and holding Up or Down
            holdTimer += Time.deltaTime;
            if (holdTimer >= waitTime)
            {
                targetY = (moveValue.y > 0) ? lookUpY : lookDownY;
            }
        }

        // Apply smooth transition
        Vector3 currentOffset = groupFraming.CenterOffset;
        float newY = Mathf.Lerp(currentOffset.y, targetY, Time.deltaTime * shiftSpeed);
        groupFraming.CenterOffset = new Vector3(defaultOffset.x, newY, 0);
    }

    public void Shake(float intensity, float duration)
    {
        if (perlinNoise == null) perlinNoise = GetComponentInChildren<CinemachineBasicMultiChannelPerlin>();
        if (perlinNoise == null) return;
        
        StopCoroutine(nameof(StopShakeRoutine)); 
        perlinNoise.AmplitudeGain = intensity;
        StartCoroutine(StopShakeRoutine(duration));
    }

    private IEnumerator StopShakeRoutine(float duration)
    {
        yield return new WaitForSecondsRealtime(duration); 
        if (perlinNoise != null) perlinNoise.AmplitudeGain = 0f;
    }
}