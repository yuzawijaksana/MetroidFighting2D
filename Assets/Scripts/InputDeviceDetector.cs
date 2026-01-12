using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public enum InputDeviceType
{
    KeyboardMouse,
    Gamepad
}

public class InputDeviceDetector : MonoBehaviour
{
    public static InputDeviceDetector Instance { get; private set; }
    
    [Header("Current Device")]
    public InputDeviceType CurrentDevice = InputDeviceType.KeyboardMouse;
    
    [Header("Events")]
    public UnityEvent<InputDeviceType> OnDeviceChanged;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Update()
    {
        DetectInputDevice();
    }
    
    private void DetectInputDevice()
    {
        InputDeviceType newDevice = CurrentDevice;
        
        // Check for keyboard/mouse input
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            newDevice = InputDeviceType.KeyboardMouse;
        }
        else if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || 
                                           Mouse.current.rightButton.wasPressedThisFrame ||
                                           Mouse.current.delta.magnitude > 0.1f))
        {
            newDevice = InputDeviceType.KeyboardMouse;
        }
        // Check for gamepad input
        else if (Gamepad.current != null)
        {
            // Check any gamepad button
            if (Gamepad.current.aButton.wasPressedThisFrame ||
                Gamepad.current.bButton.wasPressedThisFrame ||
                Gamepad.current.xButton.wasPressedThisFrame ||
                Gamepad.current.yButton.wasPressedThisFrame ||
                Gamepad.current.startButton.wasPressedThisFrame ||
                Gamepad.current.selectButton.wasPressedThisFrame ||
                Gamepad.current.leftShoulder.wasPressedThisFrame ||
                Gamepad.current.rightShoulder.wasPressedThisFrame ||
                Gamepad.current.leftTrigger.wasPressedThisFrame ||
                Gamepad.current.rightTrigger.wasPressedThisFrame ||
                Gamepad.current.dpad.up.wasPressedThisFrame ||
                Gamepad.current.dpad.down.wasPressedThisFrame ||
                Gamepad.current.dpad.left.wasPressedThisFrame ||
                Gamepad.current.dpad.right.wasPressedThisFrame)
            {
                newDevice = InputDeviceType.Gamepad;
            }
            // Check stick movement
            else if (Gamepad.current.leftStick.magnitude > 0.2f || 
                     Gamepad.current.rightStick.magnitude > 0.2f)
            {
                newDevice = InputDeviceType.Gamepad;
            }
        }
        
        // Device changed
        if (newDevice != CurrentDevice)
        {
            CurrentDevice = newDevice;
            OnDeviceChanged?.Invoke(CurrentDevice);
            Debug.Log($"Input device switched to: {CurrentDevice}");
        }
    }
    
    public bool IsUsingGamepad()
    {
        return CurrentDevice == InputDeviceType.Gamepad;
    }
    
    public bool IsUsingKeyboardMouse()
    {
        return CurrentDevice == InputDeviceType.KeyboardMouse;
    }
}
