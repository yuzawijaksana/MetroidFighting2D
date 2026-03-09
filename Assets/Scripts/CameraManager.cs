using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;
using System.Collections;
using TMPro;

// --- DATA CLASSES ---

[System.Serializable]
public class CameraBoundData
{
    public string boundName = "New Room";
    public Collider2D boundingCollider;
    [Tooltip("The Grid or Parent Object containing this room's tiles/props.")]
    public GameObject mapObject; 
    
    [HideInInspector] public Vector2 zoneMin;
    [HideInInspector] public Vector2 zoneMax;

    public bool IsPositionInZone(Vector3 position)
    {
        if (boundingCollider == null) return false;
        return boundingCollider.OverlapPoint(position);
    }

    public void CalculateZoneFromCollider()
    {
        if (boundingCollider != null)
        {
            var bounds = boundingCollider.bounds;
            zoneMin = bounds.min;
            zoneMax = bounds.max;
        }
    }
}

[System.Serializable]
public class Portal
{
    public Collider2D trigger;
    public CameraBoundData room; 
    public Transform customSpawnPoint;
}

[System.Serializable]
public class PortalConnection
{
    public string connectionName = "New Connection";
    public Portal portalA;
    public Portal portalB;
    public bool useRelativeOffset = true;
}

// --- MAIN MANAGER ---

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("UI & Feedback")]
    [SerializeField] private GameObject dialogCanvas; 
    [SerializeField] private TMP_Text areaNameText;   
    [SerializeField] private float dialogDisplayTime = 2f;
    [SerializeField] private GameObject loadingIcon;

    [Header("References")]
    [SerializeField] private CinemachineConfiner2D confiner2D;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private Transform parentToScan;

    public Transform PlayerTransform 
    {
        get 
        {
            if (_playerTransform == null) ScanForPlayer();
            return _playerTransform;
        }
    }
    
    [Header("Transition Settings")]
    [SerializeField] private List<PortalConnection> portalConnections = new List<PortalConnection>();
    [SerializeField] private float transitionDuration = 0.5f; 
    [SerializeField] private float minLoadingTime = 0.4f; 
    [SerializeField] private float pushForce = 10f; 

    private List<CameraBoundData> discoveredRooms = new List<CameraBoundData>();
    private CameraBoundData currentBound;
    private bool isTransitioning = false;
    private Rigidbody2D playerRB;
    private Collider2D lastArrivalTrigger;
    private Vector2 currentPushDir;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        ScanForPlayer();
    }

    private void Start()
    {
        if (confiner2D == null) confiner2D = FindFirstObjectByType<CinemachineConfiner2D>();
        
        GatherRoomsFromPortals();
        UpdateCameraBound();

        if (currentBound != null && currentBound.mapObject != null)
            currentBound.mapObject.SetActive(true); 
            
        if (loadingIcon != null) loadingIcon.SetActive(false);
    }

    private void GatherRoomsFromPortals()
    {
        discoveredRooms.Clear();
        foreach (var conn in portalConnections)
        {
            if (conn == null) continue;
            TryAddRoom(conn.portalA?.room);
            TryAddRoom(conn.portalB?.room);
        }
    }

    private void TryAddRoom(CameraBoundData room)
    {
        if (room != null && room.boundingCollider != null && !discoveredRooms.Contains(room))
        {
            room.CalculateZoneFromCollider();
            discoveredRooms.Add(room);
        }
    }

    private void FixedUpdate()
    {
        if (PlayerTransform == null) return;

        if (lastArrivalTrigger != null)
        {
            if (lastArrivalTrigger.OverlapPoint(PlayerTransform.position))
            {
                if (playerRB != null)
                {
                    playerRB.linearVelocity = currentPushDir * pushForce;
                }
            }
            else
            {
                lastArrivalTrigger = null;
            }
        }

        if (!isTransitioning && lastArrivalTrigger == null) 
        {
            CheckPortals();
        }
    }

    private void Update()
    {
        if (PlayerTransform == null || isTransitioning) return;
        UpdateCameraBound();
    }

    private void UpdateCameraBound()
    {
        Vector3 pos = PlayerTransform.position;
        if (currentBound != null && currentBound.IsPositionInZone(pos)) return;

        foreach (var room in discoveredRooms)
        {
            if (room.IsPositionInZone(pos))
            {
                ApplyNewBound(room);
                break;
            }
        }
    }

    private void ApplyNewBound(CameraBoundData newBound)
    {
        if (newBound == null) return;
        currentBound = newBound;
        if (confiner2D != null && newBound.boundingCollider != null)
        {
            confiner2D.BoundingShape2D = newBound.boundingCollider;
            confiner2D.InvalidateBoundingShapeCache(); 
        }
    }

    private void CheckPortals()
    {
        Vector3 pos = PlayerTransform.position;
        foreach (var conn in portalConnections)
        {
            if (conn.portalA?.trigger != null && conn.portalA.trigger.OverlapPoint(pos))
            {
                // Capture current position as the "First Touch"
                StartCoroutine(ExecutePortalTransition(conn, conn.portalA, conn.portalB, pos));
                break;
            }
            else if (conn.portalB?.trigger != null && conn.portalB.trigger.OverlapPoint(pos))
            {
                StartCoroutine(ExecutePortalTransition(conn, conn.portalB, conn.portalA, pos));
                break;
            }
        }
    }

    private IEnumerator ExecutePortalTransition(PortalConnection connection, Portal entry, Portal exit, Vector3 firstTouchPos)
    {
        isTransitioning = true;
        if (loadingIcon != null) loadingIcon.SetActive(true);
        
        currentPushDir = (playerRB != null && playerRB.linearVelocity.sqrMagnitude > 0.01f) 
            ? playerRB.linearVelocity.normalized : Vector2.right;

        if (FadeTransition.Instance != null)
        {
            bool faded = false;
            FadeTransition.Instance.FadeOut(() => faded = true); 
            while (!faded) yield return null;
        }

        float loadStartTime = Time.time;

        if (currentBound != null && currentBound.mapObject != null)
            currentBound.mapObject.SetActive(false); 

        if (exit.room.mapObject != null)
            exit.room.mapObject.SetActive(true); 

        // Teleport logic using the 'firstTouchPos' captured earlier
        Vector3 oldPos = PlayerTransform.position;
        Vector3 targetPos = CalculateTeleportPosition(connection, entry, exit, firstTouchPos);
        
        if (playerRB != null) playerRB.simulated = false;
        PlayerTransform.position = targetPos;
        CinemachineCore.OnTargetObjectWarped(PlayerTransform, targetPos - oldPos);

        ApplyNewBound(exit.room);
        ShowAreaDialog(exit.room.boundName);
        
        lastArrivalTrigger = exit.trigger;

        float timeElapsed = Time.time - loadStartTime;
        if (timeElapsed < minLoadingTime)
            yield return new WaitForSeconds(minLoadingTime - timeElapsed);

        if (playerRB != null) playerRB.simulated = true;
        if (loadingIcon != null) loadingIcon.SetActive(false);

        if (FadeTransition.Instance != null) FadeTransition.Instance.FadeIn();
        
        yield return new WaitForSeconds(transitionDuration);
        isTransitioning = false;
    }

    private Vector3 CalculateTeleportPosition(PortalConnection conn, Portal entry, Portal exit, Vector3 entryPos)
    {
        if (!conn.useRelativeOffset && exit.customSpawnPoint != null)
            return exit.customSpawnPoint.position;

        if (conn.useRelativeOffset && entry.trigger != null && exit.trigger != null)
        {
            Bounds s = entry.trigger.bounds;
            Bounds d = exit.trigger.bounds;

            // Use 'entryPos' (the first touch position) instead of current PlayerTransform.position
            float tx = Mathf.Clamp01((entryPos.x - s.min.x) / s.size.x);
            float ty = Mathf.Clamp01((entryPos.y - s.min.y) / s.size.y);

            return new Vector3(
                d.min.x + (tx * d.size.x),
                d.min.y + (ty * d.size.y),
                entryPos.z
            );
        }
        return exit.trigger != null ? exit.trigger.bounds.center : entryPos;
    }

    private void ShowAreaDialog(string name)
    {
        if (dialogCanvas != null)
        {
            if (areaNameText != null) areaNameText.text = name;
            StopCoroutine(nameof(HideDialogRoutine));
            StartCoroutine(HideDialogRoutine());
        }
    }

    private IEnumerator HideDialogRoutine()
    {
        dialogCanvas.SetActive(true);
        yield return new WaitForSeconds(dialogDisplayTime);
        dialogCanvas.SetActive(false);
    }

    public void ScanForPlayer()
    {
        if (parentToScan != null)
        {
            foreach (Transform t in parentToScan.GetComponentsInChildren<Transform>(true))
            {
                if (t.CompareTag("Player")) { SetPlayer(t); return; }
            }
        }
        
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) SetPlayer(p.transform);
    }

    private void SetPlayer(Transform t)
    {
        _playerTransform = t;
        playerRB = t.GetComponent<Rigidbody2D>();
    }
}