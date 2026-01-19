using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

/// <summary>
/// Handles individual keyboard binding for a specific action
/// Shows current key and allows rebinding
/// </summary>
public class KeyboardBindingUI : MonoBehaviour
{
    [Header("Action to Bind")]
    [Tooltip("The name of the action in GameInputs (e.g., 'Movement', 'Jump', 'Attack')")]
    public string actionName;
    
    [Tooltip("The binding index for this action (usually 0 for keyboard, 1 for gamepad)")]
    public int bindingIndex = 0;
    
    [Tooltip("Is this a gamepad binding? If true, keyboard input will be excluded during rebinding")]
    public bool isGamepad = false;

    [Header("UI References")]
    public TextMeshProUGUI keyDisplayText;
    public GameObject pressAnyKeyUI;

    private InputRebindingManager rebindManager;
    private InputAction currentAction;
    private bool isWaitingForKey = false;

    void Start()
    {
        rebindManager = InputRebindingManager.Instance;
        if (rebindManager == null)
        {
            Debug.LogError("InputRebindingManager not found!");
            return;
        }

        // Hide the "Press Any Key" UI initially
        if (pressAnyKeyUI != null)
        {
            pressAnyKeyUI.SetActive(false);
        }
        
        // Update display after a short delay to ensure GameInputs is initialized
        StartCoroutine(DelayedDisplayUpdate());
    }

    void OnEnable()
    {
        // Update display when menu is enabled
        if (rebindManager != null && rebindManager.gameInputs != null)
        {
            UpdateKeyDisplay();
        }
    }
    
    private IEnumerator DelayedDisplayUpdate()
    {
        // Wait a frame for InputRebindingManager to fully initialize
        yield return null;
        UpdateKeyDisplay();
    }

    /// <summary>
    /// Call this method from Unity Editor (UnityEvent on button press)
    /// Starts the rebinding process
    /// </summary>
    public void StartRebind()
    {
        if (rebindManager == null || isWaitingForKey)
            return;

        StartCoroutine(RebindKey());
    }

    private IEnumerator RebindKey()
    {
        isWaitingForKey = true;

        // Show "Press Any Key to Bind" UI
        if (pressAnyKeyUI != null)
        {
            pressAnyKeyUI.SetActive(true);
        }

        // Get the action
        currentAction = GetInputAction(actionName);
        if (currentAction == null)
        {
            Debug.LogError($"Action {actionName} not found!");
            isWaitingForKey = false;
            if (pressAnyKeyUI != null)
            {
                pressAnyKeyUI.SetActive(false);
            }
            yield break;
        }

        // Disable the action before rebinding
        currentAction.Disable();

        // Start interactive rebinding
        var rebindOperation = currentAction.PerformInteractiveRebinding(bindingIndex);
        
        // Exclude appropriate controls based on binding type
        if (isGamepad)
        {
            rebindOperation.WithControlsExcluding("Keyboard").WithControlsExcluding("Mouse");
        }
        else
        {
            rebindOperation.WithControlsExcluding("Mouse");
        }
        
        rebindOperation.OnMatchWaitForAnother(0.1f)
            .OnComplete(operation =>
            {
                // Get the new binding path that was just assigned
                string newBindingPath = currentAction.bindings[bindingIndex].overridePath;
                if (string.IsNullOrEmpty(newBindingPath))
                {
                    newBindingPath = currentAction.bindings[bindingIndex].path;
                }
                
                Debug.Log($"New binding path for {actionName}: {newBindingPath}");
                
                // Check if this key is already bound to another action and swap if needed
                HandleDuplicateBinding(newBindingPath);
                
                // Save the current binding
                rebindManager.SaveBindingOverride(actionName, bindingIndex, currentAction.bindings[bindingIndex].overridePath);
                
                // Re-enable the action
                currentAction.Enable();
                operation.Dispose();
                
                // Wait a frame then refresh all UIs
                StartCoroutine(RefreshAllUIsDelayed());
                
                // Hide "Press Any Key" UI
                if (pressAnyKeyUI != null)
                {
                    pressAnyKeyUI.SetActive(false);
                }
                
                isWaitingForKey = false;
                
                Debug.Log($"Bound {actionName} to {GetCurrentKeyName()}");
            })
            .OnCancel(operation =>
            {
                currentAction.Enable();
                operation.Dispose();
                
                if (pressAnyKeyUI != null)
                {
                    pressAnyKeyUI.SetActive(false);
                }
                
                isWaitingForKey = false;
            });

        rebindOperation.Start();
        
        yield return null;
    }

    /// <summary>
    /// Check if the new binding is already used by another action
    /// If so, swap the bindings
    /// </summary>
    private void HandleDuplicateBinding(string newBindingPath)
    {
        if (rebindManager == null || rebindManager.gameInputs == null)
            return;

        // Get the old binding path before we changed it
        string oldBindingPath = currentAction.bindings[bindingIndex].path;

        // Get all actions from the Player action map
        var playerActions = rebindManager.gameInputs.Player.Get();
        
        foreach (var action in playerActions)
        {
            // Check all bindings of this action
            for (int i = 0; i < action.bindings.Count; i++)
            {
                // Skip composite bindings (they don't have actual controls)
                if (action.bindings[i].isComposite)
                    continue;
                
                // Skip the exact binding we just changed
                if (action.name == actionName && i == bindingIndex)
                    continue;
                
                // Get the effective path of this binding
                string existingPath = action.bindings[i].effectivePath;
                
                // Check if this binding path matches our new binding
                if (existingPath == newBindingPath)
                {
                    // Found a duplicate! Swap the bindings
                    Debug.Log($"Duplicate found! Swapping {newBindingPath} with {oldBindingPath} on {action.name} binding {i}");
                    
                    // Give the old binding to the conflicting action
                    action.ApplyBindingOverride(i, oldBindingPath);
                    rebindManager.SaveBindingOverride(action.name, i, oldBindingPath);
                    
                    Debug.Log($"Swapped: {actionName} now has {newBindingPath}, {action.name} now has {oldBindingPath}");
                    return; // Only swap with first match
                }
            }
        }
    }
    
    /// <summary>
    /// Wait a frame then refresh all UI displays
    /// </summary>
    private IEnumerator RefreshAllUIsDelayed()
    {
        yield return null; // Wait one frame
        
        // Update display for this binding
        UpdateKeyDisplay();
        
        // Refresh ALL binding UIs
        RefreshAllBindingUIs();
    }

    /// <summary>
    /// Refresh all KeyboardBindingUI components in the scene
    /// </summary>
    private void RefreshAllBindingUIs()
    {
        var allBindingUIs = FindObjectsOfType<KeyboardBindingUI>();
        
        foreach (var bindingUI in allBindingUIs)
        {
            bindingUI.UpdateKeyDisplay();
        }
        
        Debug.Log($"Refreshed {allBindingUIs.Length} binding UIs");
    }

    /// <summary>
    /// Update the UI of another KeyboardBindingUI component
    /// </summary>
    private void UpdateOtherBindingUI(string otherActionName, int otherBindingIndex)
    {
        // Find all KeyboardBindingUI components in the scene
        var allBindingUIs = FindObjectsOfType<KeyboardBindingUI>();
        
        foreach (var bindingUI in allBindingUIs)
        {
            // Skip ourselves
            if (bindingUI == this)
                continue;
            
            // Find the matching binding UI and update it
            if (bindingUI.actionName == otherActionName && bindingUI.bindingIndex == otherBindingIndex)
            {
                bindingUI.UpdateKeyDisplay();
                Debug.Log($"Updated UI for {otherActionName} binding index {otherBindingIndex}");
                break;
            }
        }
    }

    /// <summary>
    /// Update the text display with the current key binding
    /// </summary>
    public void UpdateKeyDisplay()
    {
        if (keyDisplayText == null)
            return;

        string keyName = GetCurrentKeyName();
        keyDisplayText.text = keyName;
    }

    /// <summary>
    /// Get the current key name for this binding
    /// </summary>
    public string GetCurrentKeyName()
    {
        if (rebindManager == null)
            return "???";

        var action = GetInputAction(actionName);
        if (action == null)
            return "???";

        // Get the display string for this binding
        return action.GetBindingDisplayString(bindingIndex);
    }

    /// <summary>
    /// Get an InputAction by name from GameInputs
    /// </summary>
    private InputAction GetInputAction(string name)
    {
        if (rebindManager == null || rebindManager.gameInputs == null)
            return null;

        var action = rebindManager.gameInputs.Player.Get().FindAction(name);
        return action;
    }

    /// <summary>
    /// Reset this binding to default
    /// </summary>
    public void ResetToDefault()
    {
        if (rebindManager == null)
            return;

        rebindManager.ResetBinding(actionName, bindingIndex);
        UpdateKeyDisplay();
    }
    
    /// <summary>
    /// Reset ALL bindings to default and refresh all UIs
    /// Call this from a menu button
    /// </summary>
    public void ResetAllBindingsToDefault()
    {
        if (rebindManager == null)
        {
            Debug.LogError("InputRebindingManager not found!");
            return;
        }
        
        // Reset all bindings in the manager
        rebindManager.ResetAllBindings();
        
        // Refresh all binding UIs
        var allBindingUIs = FindObjectsOfType<KeyboardBindingUI>();
        foreach (var bindingUI in allBindingUIs)
        {
            bindingUI.UpdateKeyDisplay();
        }
        
        Debug.Log("All bindings reset to default!");
    }
}
