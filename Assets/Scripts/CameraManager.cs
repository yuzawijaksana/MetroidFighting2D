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
    
    [Header("Directional Settings")]
    public bool useDirectionalSwitching = false;
    public CameraBoundData leftBound;
    public CameraBoundData rightBound;
    
    [Header("Transition Settings")]
    public bool enableBlackoutTransition = true;
    
    [Header("Zone Definition")]
    public Vector2 zoneMin;
    public Vector2 zoneMax;
    
    // Helper method to check if position is in this zone
    public bool IsPositionInZone(Vector3 position)
    {
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
    
    [Header("Camera Bounds")]
    [SerializeField] private List<CameraBoundData> cameraBounds = new List<CameraBoundData>();
    
    [Header("Directional Switching Settings")]
    [SerializeField] private float directionCheckInterval = 0.1f;
    [SerializeField] private float directionSwitchDelay = 0.3f;
    [SerializeField] private float positionCheckInterval = 0.2f;
    
    // State tracking
    private CameraBoundData currentBound;
    private CameraBoundData activeDirectionalBound;
    private PlayerController playerController;
    private bool isTransitioning = false;
    private float lastDirectionCheck = 0f;
    private float lastPositionCheck = 0f;
    
    private void Start()
    {
        // Find player if not assigned
        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerController = player.GetComponent<PlayerController>();
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
        if (playerTransform == null) return;
        
        // Check position changes less frequently for performance
        if (Time.time - lastPositionCheck >= positionCheckInterval)
        {
            UpdateCameraBound();
            lastPositionCheck = Time.time;
        }
        
        // Handle directional switching
        if (activeDirectionalBound != null && 
            activeDirectionalBound.useDirectionalSwitching &&
            Time.time - lastDirectionCheck >= directionCheckInterval)
        {
            CheckDirectionalSwitching();
            lastDirectionCheck = Time.time;
        }
    }
    
    private void UpdateCameraBound()
    {
        Vector3 playerPos = playerTransform.position;
        CameraBoundData newBound = FindBoundForPosition(playerPos);
        
        if (newBound != null && newBound != currentBound)
        {
            Debug.Log($"🎯 Player moved to new bound: '{newBound.boundName}'");
            
            if (newBound.useDirectionalSwitching)
            {
                StartDirectionalSwitching(newBound);
            }
            else
            {
                StopDirectionalSwitching();
                SetCameraBound(newBound);
            }
        }
        else if (newBound == null && activeDirectionalBound != null)
        {
            // Player left directional zone
            StopDirectionalSwitching();
        }
    }
    
    private CameraBoundData FindBoundForPosition(Vector3 position)
    {
        // Find the most appropriate bound for this position
        // Prioritize directional bounds, then regular bounds
        CameraBoundData bestBound = null;
        
        foreach (var bound in cameraBounds)
        {
            if (bound.IsPositionInZone(position))
            {
                if (bound.useDirectionalSwitching)
                {
                    return bound; // Prioritize directional bounds
                }
                else if (bestBound == null)
                {
                    bestBound = bound;
                }
            }
        }
        
        return bestBound;
    }
    
    private void StartDirectionalSwitching(CameraBoundData directionalBound)
    {
        if (activeDirectionalBound == directionalBound) return;
        
        Debug.Log($"🧭 Starting directional switching for '{directionalBound.boundName}'");
        
        activeDirectionalBound = directionalBound;
        
        // Apply the directional bound immediately
        SetCameraBound(directionalBound);
        
        // Reset direction check timer
        lastDirectionCheck = Time.time;
    }
    
    private void StopDirectionalSwitching()
    {
        if (activeDirectionalBound == null) return;
        
        Debug.Log($"🛑 Stopping directional switching for '{activeDirectionalBound.boundName}'");
        activeDirectionalBound = null;
    }
    
    private void CheckDirectionalSwitching()
    {
        if (playerController == null || activeDirectionalBound == null) return;
        
        // Prevent rapid direction changes
        if (Time.time - lastDirectionCheck < directionSwitchDelay) return;
        
        bool isFacingRight = playerController.isFacingRight;
        CameraBoundData targetBound = null;
        
        if (isFacingRight && activeDirectionalBound.rightBound != null)
        {
            targetBound = activeDirectionalBound.rightBound;
            Debug.Log($"➡️ Player facing RIGHT - switching to '{targetBound.boundName}'");
        }
        else if (!isFacingRight && activeDirectionalBound.leftBound != null)
        {
            targetBound = activeDirectionalBound.leftBound;
            Debug.Log($"⬅️ Player facing LEFT - switching to '{targetBound.boundName}'");
        }
        
        // Switch to target bound if different from current
        if (targetBound != null && targetBound != currentBound && !isTransitioning)
        {
            SetCameraBound(targetBound);
        }
    }
    
    private void SetCameraBound(CameraBoundData newBound)
    {
        if (newBound == null || newBound.boundingCollider == null) return;
        
        var previousBound = currentBound;
        currentBound = newBound;
        
        if (newBound.enableBlackoutTransition && previousBound != null)
        {
            StartTransition(newBound);
        }
        else
        {
            ApplyCameraBound(newBound);
        }
    }
    
    private void ApplyCameraBound(CameraBoundData bound)
    {
        if (confiner2D != null && bound.boundingCollider != null)
        {
            confiner2D.BoundingShape2D = bound.boundingCollider;
            
            // Force camera to recalculate immediately
            confiner2D.InvalidateCache();
            
            Debug.Log($"✅ Applied camera bound: '{bound.boundName}'");
        }
    }
    
    private void StartTransition(CameraBoundData targetBound)
    {
        if (isTransitioning) return;
        
        isTransitioning = true;
        Debug.Log($"🎬 Starting transition to '{targetBound.boundName}'");
        
        FadeTransition.QuickFadeTransition(
            onMidFade: () => {
                ApplyCameraBound(targetBound);
            },
            onComplete: () => {
                isTransitioning = false;
                Debug.Log($"🎬 Transition complete: '{targetBound.boundName}'");
            }
        );
    }
    
    #region Public Methods
    
    public void ForceSetBound(string boundName)
    {
        var bound = cameraBounds.Find(b => b.boundName == boundName);
        if (bound != null)
        {
            StopDirectionalSwitching();
            SetCameraBound(bound);
        }
    }
    
    public CameraBoundData GetCurrentBound()
    {
        return currentBound;
    }
    
    public bool IsInDirectionalMode()
    {
        return activeDirectionalBound != null;
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
            else if (bound == activeDirectionalBound)
            {
                Gizmos.color = Color.blue; // Active directional bound
            }
            else
            {
                Gizmos.color = bound.useDirectionalSwitching ? Color.yellow : Color.green;
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