using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardBindingTester : MonoBehaviour
{
    private RawInputHandler rawInputHandler;

    private void Start()
    {
        rawInputHandler = Object.FindFirstObjectByType<RawInputHandler>();
        if (rawInputHandler == null)
        {
            Debug.LogError("RawInputHandler not found in the scene.");
        }
    }

    private void Update()
    {
        // Remove all binding and mapping test code, as it's no longer needed
    }
}
