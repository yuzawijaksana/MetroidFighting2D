using UnityEngine;

public class PlayerTriggerTest : MonoBehaviour
{
    private void Start()
    {
        // DETAILED DIAGNOSTIC OF PLAYER SETUP
        Debug.Log($"=== PLAYER SELF-CHECK: '{gameObject.name}' ===");
        Debug.Log($"Player Position: {transform.position}");
        Debug.Log($"Player Tag: '{tag}'");
        Debug.Log($"Player Layer: {LayerMask.LayerToName(gameObject.layer)}");
        Debug.Log($"Player Active: {gameObject.activeInHierarchy}");
        
        Debug.Log($"Components on this GameObject:");
        var components = GetComponents<Component>();
        foreach (var comp in components)
        {
            Debug.Log($"  - {comp.GetType().Name}");
        }
        
        var collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            Debug.Log($"✅ PLAYER COLLIDER FOUND!");
            Debug.Log($"   Collider Type: {collider.GetType().Name}");
            Debug.Log($"   Is Trigger: {collider.isTrigger}");
            Debug.Log($"   Enabled: {collider.enabled}");
            Debug.Log($"   Bounds: {collider.bounds}");
            Debug.Log($"   Bounds Size: {collider.bounds.size}");
        }
        else
        {
            Debug.LogError($"❌ NO COLLIDER on player {gameObject.name}!");
        }
        
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Debug.Log($"✅ RIGIDBODY2D FOUND!");
            Debug.Log($"   Body Type: {rb.bodyType}");
            Debug.Log($"   Simulated: {rb.simulated}");
        }
        else
        {
            Debug.LogError($"❌ NO RIGIDBODY2D on player {gameObject.name}!");
        }
        
        Debug.Log($"=== END PLAYER SELF-CHECK ===");
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"🎮 PLAYER TRIGGER: Player entered trigger with {other.name} (Tag: {other.tag})");
    }
    
    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time % 1f < 0.1f) // Log once per second to avoid spam
        {
            Debug.Log($"🎮 PLAYER STAYING: Player staying in trigger with {other.name}");
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log($"🎮 PLAYER EXIT: Player exited trigger with {other.name}");
    }
    
    private void Update()
    {
        // Log player position every few seconds to track movement
        if (Time.time % 3f < 0.1f)
        {
            Debug.Log($"📍 PLAYER POSITION: {transform.position}");
        }
    }
}