using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class CameraBoundData
{
    [Header("Bound Info")]
    public string boundName = "Camera Bound";
    public Collider2D boundingCollider;
    
    
    [Header("Zone Definition")]
    public Vector2 zoneMin;
    public Vector2 zoneMax;
    
    // Helper method to check if position is in this zone
    public bool IsPositionInZone(Vector3 position)
    {
        // Use collider for accurate shape detection (supports rotation and polygon shapes)
        if (boundingCollider != null)
        {
            return boundingCollider.OverlapPoint(position);
        }
        return position.x >= zoneMin.x && position.x <= zoneMax.x &&
               position.y >= zoneMin.y && position.y <= zoneMax.y;
    }
    
    // Auto-calculate zone from collider bounds
    public void CalculateZoneFromCollider()
    {
        if (boundingCollider != null)
        {
            var bounds = boundingCollider.bounds;
            zoneMin = new Vector2(bounds.min.x, bounds.min.y);
            zoneMax = new Vector2(bounds.max.x, bounds.max.y);
        }
    }
}

public class CameraManager : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private CinemachineConfiner2D confiner2D;
    [SerializeField] private Transform playerTransform;
    [Tooltip("The parent object to scan for a child tagged 'Player'")]
    [SerializeField] private Transform parentToScan;
    
    [Header("Camera Bounds")]
    [SerializeField] private List<CameraBoundData> cameraBounds = new List<CameraBoundData>();
    
    [SerializeField] private float positionCheckInterval = 0.2f;
    
    // State tracking
    private CameraBoundData currentBound;
    private float lastPositionCheck = 0f;
    
    private void Start()
    {
        // Find player if not assigned
        if (playerTransform == null)
        {
            if (parentToScan != null)
            {
                ScanForPlayerInParent();
            }

            if (playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    SetPlayer(player.transform);
                }
            }
        }
        
        // Find confiner if not assigned
        if (confiner2D == null)
        {
            confiner2D = FindFirstObjectByType<CinemachineConfiner2D>();
        }
        
        // Auto-calculate zones from colliders
        foreach (var bound in cameraBounds)
        {
            bound.CalculateZoneFromCollider();
        }
        
        // Set initial camera bound
        UpdateCameraBound();
        
        Debug.Log($"CameraManager initialized with {cameraBounds.Count} bounds");
    }
    
    private void Update()
    {
        if (playerTransform == null)
        {
            if (parentToScan != null)
            {
                ScanForPlayerInParent();
            }
            
            if (playerTransform == null) return;
        }
        
        // Check position changes less frequently for performance
        if (Time.time - lastPositionCheck >= positionCheckInterval)
        {
            UpdateCameraBound();
            lastPositionCheck = Time.time;
        }
    }

    private void ScanForPlayerInParent()
    {
        foreach (Transform child in parentToScan.GetComponentsInChildren<Transform>())
        {
            if (child.CompareTag("Player"))
            {
                SetPlayer(child);
                break;
            }
        }
    }

    private void SetPlayer(Transform player)
    {
        playerTransform = player;
        UpdateCameraBound();
    }
    
    private void UpdateCameraBound()
    {
        Vector3 playerPos = playerTransform.position;
        CameraBoundData newBound = FindBoundForPosition(playerPos);
        
        if (newBound != null && newBound != currentBound)
        {
            Debug.Log($"🎯 Player moved to new bound: '{newBound.boundName}'");
            SetCameraBound(newBound);
        }
    }
    
    private CameraBoundData FindBoundForPosition(Vector3 position)
    {
        // Find the most appropriate bound for this position
        CameraBoundData firstMatch = null;
        bool isCurrentBoundValid = false;
        
        foreach (var bound in cameraBounds)
        {
            if (bound.IsPositionInZone(position))
            {
                if (bound == currentBound)
                {
                    isCurrentBoundValid = true;
                }
                else if (firstMatch == null)
                {
                    firstMatch = bound;
                }
            }
        }
        
        // If we are still inside the current bound, stay there to prevent flickering in overlap zones
        if (isCurrentBoundValid)
        {
            return currentBound;
        }
        
        return firstMatch;
    }
    
    private void SetCameraBound(CameraBoundData newBound)
    {
        if (newBound == null || newBound.boundingCollider == null) return;
        
        var previousBound = currentBound;
        currentBound = newBound;
        
        ApplyCameraBound(newBound);
    }
    
    private void ApplyCameraBound(CameraBoundData bound)
    {
        if (confiner2D != null && bound.boundingCollider != null)
        {
            confiner2D.BoundingShape2D = bound.boundingCollider;
            
            // Force camera to recalculate immediately
            confiner2D.InvalidateBoundingShapeCache();
            
            Debug.Log($"✅ Applied camera bound: '{bound.boundName}'");
        }
    }
    
    #region Public Methods
    
    public void ForceSetBound(string boundName)
    {
        var bound = cameraBounds.Find(b => b.boundName == boundName);
        if (bound != null)
        {
            SetCameraBound(bound);
        }
    }
    
    public CameraBoundData GetCurrentBound()
    {
        return currentBound;
    }
    
    public void AddCameraBound(CameraBoundData newBound)
    {
        if (!cameraBounds.Contains(newBound))
        {
            cameraBounds.Add(newBound);
            newBound.CalculateZoneFromCollider();
        }
    }
    
    public void RemoveCameraBound(CameraBoundData bound)
    {
        cameraBounds.Remove(bound);
    }
    
    #endregion
    
    #region Debug Helpers
    
    private void OnDrawGizmos()
    {
        if (cameraBounds == null) return;
        
        Gizmos.color = Color.green;
        foreach (var bound in cameraBounds)
        {
            if (bound == currentBound)
            {
                Gizmos.color = Color.red; // Current bound
            }
            else
            {
                Gizmos.color = Color.green;
            }
            
            Vector3 center = new Vector3(
                (bound.zoneMin.x + bound.zoneMax.x) / 2f,
                (bound.zoneMin.y + bound.zoneMax.y) / 2f,
                0f
            );
            Vector3 size = new Vector3(
                bound.zoneMax.x - bound.zoneMin.x,
                bound.zoneMax.y - bound.zoneMin.y,
                1f
            );
            
            Gizmos.DrawWireCube(center, size);
        }
    }
    
    #endregion
}