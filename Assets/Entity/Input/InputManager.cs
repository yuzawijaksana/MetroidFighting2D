using UnityEngine;

public class InputManager : MonoBehaviour
{
    private RawInputHandler rawInputHandler;

    private void Start()
    {
        rawInputHandler = GetComponent<RawInputHandler>();
        if (rawInputHandler == null)
        {
            Debug.LogError("RawInputHandler not found on InputManager.");
        }
    }
}
