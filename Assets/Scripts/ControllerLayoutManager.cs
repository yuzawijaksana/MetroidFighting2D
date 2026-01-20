using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Manages controller layout display (Xbox vs PlayStation button names)
/// Can auto-detect or manually toggle between layouts
/// </summary>
public class ControllerLayoutManager : MonoBehaviour
{
    public static ControllerLayoutManager Instance { get; private set; }

    public enum ControllerLayout
    {
        Xbox,
        PlayStation
    }

    [Header("Controller Layout Settings")]
    [SerializeField] private ControllerLayout currentLayout = ControllerLayout.Xbox;
    [SerializeField] private bool autoDetect = true;

    [Header("Optional: Display Current Layout")]
    [SerializeField] private TextMeshProUGUI layoutDisplayText;

    private const string LAYOUT_SAVE_KEY = "ControllerLayout";

    void Awake()
    {
        Debug.Log("[ControllerLayout] Awake called");
        
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadLayout();
            
            Debug.Log($"[ControllerLayout] Initialized. Auto-detect: {autoDetect}, Current layout: {currentLayout}");
            
            if (autoDetect)
            {
                DetectControllerLayout();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log($"[ControllerLayout] Start called. Auto-detect: {autoDetect}");
        
        UpdateLayoutDisplay();
        
        if (autoDetect)
        {
            // Listen for input device changes
            InputSystem.onActionChange += OnActionChange;
            
            // Check for controller changes periodically
            InvokeRepeating(nameof(DetectControllerLayout), 0.5f, 0.5f);
            Debug.Log("[ControllerLayout] Detection started - will check every 0.5 seconds");
        }
    }
    
    void OnDestroy()
    {
        if (autoDetect)
        {
            InputSystem.onActionChange -= OnActionChange;
        }
    }
    
    private void OnActionChange(object obj, InputActionChange change)
    {
        if (change == InputActionChange.ActionPerformed)
        {
            Debug.Log("[ControllerLayout] Input detected, checking controller...");
            DetectControllerLayout();
        }
    }

    /// <summary>
    /// Auto-detect controller type from connected gamepad
    /// </summary>
    private void DetectControllerLayout()
    {
        // List all connected gamepads
        var allGamepads = Gamepad.all;
        Debug.Log($"[ControllerLayout] Total gamepads connected: {allGamepads.Count}");
        
        for (int i = 0; i < allGamepads.Count; i++)
        {
            var gp = allGamepads[i];
            Debug.Log($"[ControllerLayout] Gamepad {i}: {gp.device.description.product} (Manufacturer: {gp.device.description.manufacturer})");
        }
        
        var gamepad = Gamepad.current;
        
        Debug.Log($"[ControllerLayout] Gamepad.current = {(gamepad != null ? gamepad.device.description.product : "NULL")}");
        
        if (gamepad != null)
        {
            string interfaceName = gamepad.device.description.interfaceName.ToLower();
            string deviceName = gamepad.device.description.product.ToLower();
            
            Debug.Log($"[ControllerLayout] Interface: '{interfaceName}', Device: '{deviceName}'");
            
            ControllerLayout detectedLayout;
            
            // For third-party controllers, check interface first, then device name
            if (interfaceName.Contains("xinput"))
            {
                // XInput = Xbox-style controller
                detectedLayout = ControllerLayout.Xbox;
                Debug.Log($"[ControllerLayout] XInput detected - Using Xbox layout");
            }
            else if (deviceName.Contains("xbox") || deviceName.Contains("360") || deviceName.Contains("one"))
            {
                // Device name mentions Xbox
                detectedLayout = ControllerLayout.Xbox;
                Debug.Log($"[ControllerLayout] Xbox device name detected - Using Xbox layout");
            }
            else if (deviceName.Contains("playstation") || deviceName.Contains("dualshock") || deviceName.Contains("dualsense") || 
                     deviceName.Contains("ps4") || deviceName.Contains("ps5"))
            {
                // Device name mentions PlayStation
                detectedLayout = ControllerLayout.PlayStation;
                Debug.Log($"[ControllerLayout] PlayStation device name detected - Using PlayStation layout");
            }
            else
            {
                // Unknown third-party controller - default to Xbox (more common)
                // User can manually toggle if needed
                detectedLayout = ControllerLayout.Xbox;
                Debug.Log($"[ControllerLayout] Unknown third-party controller - Defaulting to Xbox layout (use Toggle if incorrect)");
            }
            
            if (detectedLayout != currentLayout)
            {
                Debug.Log($"[ControllerLayout] Layout changed from {currentLayout} to {detectedLayout}");
                currentLayout = detectedLayout;
                UpdateLayoutDisplay();
                RefreshAllControllerUIs();
            }
            else
            {
                Debug.Log($"[ControllerLayout] Layout unchanged: {currentLayout}");
            }
        }
        else
        {
            Debug.Log("[ControllerLayout] No gamepad connected");
        }
    }

    /// <summary>
    /// Manually toggle between Xbox and PlayStation layouts
    /// </summary>
    public void ToggleLayout()
    {
        currentLayout = currentLayout == ControllerLayout.Xbox ? ControllerLayout.PlayStation : ControllerLayout.Xbox;
        SaveLayout();
        UpdateLayoutDisplay();
        RefreshAllControllerUIs();
    }

    /// <summary>
    /// Set specific layout
    /// </summary>
    public void SetLayout(ControllerLayout layout)
    {
        currentLayout = layout;
        SaveLayout();
        UpdateLayoutDisplay();
        RefreshAllControllerUIs();
    }

    /// <summary>
    /// Get the display name for a gamepad button based on current layout
    /// </summary>
    public string GetButtonDisplayName(string bindingPath)
    {
        if (string.IsNullOrEmpty(bindingPath))
            return "Not Bound";

        // Handle different binding path formats
        string path = bindingPath.ToLower();

        if (currentLayout == ControllerLayout.PlayStation)
        {
            // PlayStation button names
            if (path.Contains("buttonsouth")) return "Cross (✕)";
            if (path.Contains("buttoneast")) return "Circle (○)";
            if (path.Contains("buttonwest")) return "Square (□)";
            if (path.Contains("buttonnorth")) return "Triangle (△)";
            if (path.Contains("leftshoulder")) return "L1";
            if (path.Contains("rightshoulder")) return "R1";
            if (path.Contains("lefttrigger")) return "L2";
            if (path.Contains("righttrigger")) return "R2";
            if (path.Contains("leftstickpress")) return "L3";
            if (path.Contains("rightstickpress")) return "R3";
            if (path.Contains("start")) return "Options";
            if (path.Contains("select")) return "Share";
            if (path.Contains("dpad/up")) return "D-Pad Up";
            if (path.Contains("dpad/down")) return "D-Pad Down";
            if (path.Contains("dpad/left")) return "D-Pad Left";
            if (path.Contains("dpad/right")) return "D-Pad Right";
            if (path.Contains("leftstick")) return "Left Stick";
            if (path.Contains("rightstick")) return "Right Stick";
        }
        else // Xbox layout
        {
            // Xbox button names
            if (path.Contains("buttonsouth")) return "A";
            if (path.Contains("buttoneast")) return "B";
            if (path.Contains("buttonwest")) return "X";
            if (path.Contains("buttonnorth")) return "Y";
            if (path.Contains("leftshoulder")) return "LB";
            if (path.Contains("rightshoulder")) return "RB";
            if (path.Contains("lefttrigger")) return "LT";
            if (path.Contains("righttrigger")) return "RT";
            if (path.Contains("leftstickpress")) return "LS";
            if (path.Contains("rightstickpress")) return "RS";
            if (path.Contains("start")) return "Menu";
            if (path.Contains("select")) return "View";
            if (path.Contains("dpad/up")) return "D-Pad Up";
            if (path.Contains("dpad/down")) return "D-Pad Down";
            if (path.Contains("dpad/left")) return "D-Pad Left";
            if (path.Contains("dpad/right")) return "D-Pad Right";
            if (path.Contains("leftstick")) return "Left Stick";
            if (path.Contains("rightstick")) return "Right Stick";
        }

        // Fallback to default display
        return bindingPath;
    }

    /// <summary>
    /// Update the layout display text
    /// </summary>
    private void UpdateLayoutDisplay()
    {
        if (layoutDisplayText != null)
        {
            layoutDisplayText.text = currentLayout == ControllerLayout.Xbox ? "Xbox" : "PlayStation";
        }
    }

    /// <summary>
    /// Refresh all KeyboardBindingUI components to update button names
    /// </summary>
    private void RefreshAllControllerUIs()
    {
        var allBindingUIs = FindObjectsOfType<KeyboardBindingUI>();
        int refreshedCount = 0;
        
        foreach (var bindingUI in allBindingUIs)
        {
            if (bindingUI.isGamepad)
            {
                bindingUI.UpdateKeyDisplay();
                refreshedCount++;
                Debug.Log($"[ControllerLayout] Refreshed gamepad binding: {bindingUI.actionName}");
            }
        }
        
        Debug.Log($"[ControllerLayout] Refreshed {refreshedCount} gamepad bindings (Total UIs: {allBindingUIs.Length})");
    }

    /// <summary>
    /// Save layout preference
    /// </summary>
    private void SaveLayout()
    {
        PlayerPrefs.SetInt(LAYOUT_SAVE_KEY, (int)currentLayout);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Load layout preference
    /// </summary>
    private void LoadLayout()
    {
        if (PlayerPrefs.HasKey(LAYOUT_SAVE_KEY))
        {
            currentLayout = (ControllerLayout)PlayerPrefs.GetInt(LAYOUT_SAVE_KEY);
        }
    }

    public ControllerLayout CurrentLayout => currentLayout;
}
