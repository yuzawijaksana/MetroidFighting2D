using UnityEngine;
using Unity.Cinemachine;

public class CameraBound : MonoBehaviour
{
    [Header("Camera Bound Settings")]
    [SerializeField] private string boundName = "Camera Bound";
    
    [Header("Transition Settings")]
    [Tooltip("Enable/disable blackout transition when entering this camera bound")]
    [SerializeField] private bool enableBlackoutTransition = true;
    [Tooltip("Small delay before transition starts (in seconds)")]
    [SerializeField] private float transitionDelay = 0.2f;
    
    [Header("References")]
    [SerializeField] private CinemachineConfiner2D confiner2D;
    
    private Collider2D boundingCollider;
    private static CameraBound currentBound;
    private static bool isTransitioning = false;
    
    private void Start()
    {
        // Get the collider on this GameObject
        boundingCollider = GetComponent<Collider2D>();
        
        // DETAILED DIAGNOSTIC OF OWN COLLIDER
        Debug.Log($"=== CAMERA BOUND SELF-CHECK: '{boundName}' ===");
        Debug.Log($"GameObject Name: {gameObject.name}");
        Debug.Log($"Components on this GameObject:");
        
        var components = GetComponents<Component>();
        foreach (var comp in components)
        {
            Debug.Log($"  - {comp.GetType().Name}");
        }
        
        if (boundingCollider != null)
        {
            boundingCollider.isTrigger = true;
            Debug.Log($"✅ COLLIDER FOUND!");
            Debug.Log($"   Collider Type: {boundingCollider.GetType().Name}");
            Debug.Log($"   Is Trigger: {boundingCollider.isTrigger}");
            Debug.Log($"   Enabled: {boundingCollider.enabled}");
            Debug.Log($"   GameObject Active: {boundingCollider.gameObject.activeInHierarchy}");
            Debug.Log($"   Bounds: {boundingCollider.bounds}");
            Debug.Log($"   Bounds Size: {boundingCollider.bounds.size}");
            Debug.Log($"   Bounds Center: {boundingCollider.bounds.center}");
        }
        else
        {
            Debug.LogError($"❌ NO COLLIDER FOUND on {gameObject.name}!");
            Debug.LogError($"   Make sure this GameObject has a Collider2D component!");
        }
        
        // Find the confiner if not assigned
        if (confiner2D == null)
        {
            confiner2D = FindFirstObjectByType<CinemachineConfiner2D>();
            if (confiner2D != null)
            {
                Debug.Log($"✅ Found CinemachineConfiner2D: {confiner2D.name}");
            }
            else
            {
                Debug.LogError($"❌ No CinemachineConfiner2D found in scene!");
            }
        }
        
        Debug.Log($"=== END SELF-CHECK ===");
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"🔥 TRIGGER ENTERED: {other.name} (Tag: '{other.tag}') entered '{boundName}'");
        Debug.Log($"   Other GameObject: {other.gameObject.name}");
        Debug.Log($"   Other Position: {other.transform.position}");
        Debug.Log($"   This Bound Position: {transform.position}");
        Debug.Log($"   This Bound Bounds: {boundingCollider.bounds}");
        
        // Check if it's a player
        if (other.CompareTag("Player"))
        {
            Debug.Log($"🎯 PLAYER DETECTED: {other.name} is a player! Applying camera bound.");
            
            // Check if we're already transitioning to avoid multiple transitions
            if (!isTransitioning && currentBound != this)
            {
                if (enableBlackoutTransition)
                {
                    Debug.Log($"🎬 STARTING BLACKOUT TRANSITION for '{boundName}'");
                    StartTransition();
                }
                else
                {
                    Debug.Log($"⚡ INSTANT CAMERA CHANGE for '{boundName}' (no transition)");
                    ApplyCameraBound();
                }
            }
        }
        else
        {
            Debug.Log($"❌ NOT PLAYER: {other.name} is not tagged as 'Player'");
        }
    }
    
    // Add a method to test if the trigger is working at all
    private void OnTriggerStay2D(Collider2D other)
    {
        // This fires every frame while something is in the trigger
        // We'll only log it once per second to avoid spam
        if (Time.time % 1f < 0.1f) // Log roughly once per second
        {
            Debug.Log($"🔄 TRIGGER STAY: {other.name} is staying in trigger '{boundName}'");
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log($"🚪 TRIGGER EXIT: {other.name} exited trigger '{boundName}'");
    }
    
    private void ApplyCameraBound()
    {
        if (confiner2D != null && boundingCollider != null)
        {
            // Update current bound
            var previousBound = currentBound;
            currentBound = this;
            
            confiner2D.BoundingShape2D = boundingCollider;
            Debug.Log($"✅ SUCCESS: Applied camera bound '{boundName}' to confiner!");
            
            if (previousBound != null)
            {
                Debug.Log($"📷 CAMERA TRANSITION: '{previousBound.boundName}' → '{boundName}'");
            }
        }
        else
        {
            Debug.LogError($"❌ FAILED: confiner2D={confiner2D}, boundingCollider={boundingCollider}");
        }
    }
    
    private void StartTransition()
    {
        isTransitioning = true;
        
        // Start the fade transition
        FadeTransition.QuickFadeTransition(
            onMidFade: () => {
                // This happens when screen is black - apply the camera bound change
                ApplyCameraBound();
            },
            onComplete: () => {
                // This happens when fade in completes
                isTransitioning = false;
                Debug.Log($"🎬 TRANSITION COMPLETE: Now in '{boundName}'");
            }
        );
    }
    
    // Manual method to force apply without transition
    public void ForceApplyBound()
    {
        ApplyCameraBound();
    }
    
    // Manual method to apply with transition
    public void ApplyBoundWithTransition()
    {
        if (!isTransitioning)
        {
            StartTransition();
        }
    }
    
    public static CameraBound GetCurrentBound()
    {
        return currentBound;
    }
    
    public string GetBoundName()
    {
        return boundName;
    }
}
