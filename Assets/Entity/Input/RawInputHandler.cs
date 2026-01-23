using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class RawInputHandler : MonoBehaviour
{
    [DllImport("RawInputPlugin.dll")]
    private static extern void RegisterRawInput(System.IntPtr hwnd);

    [DllImport("RawInputPlugin.dll")]
    private static extern void ProcessRawInputMessages();

    [DllImport("RawInputPlugin.dll")]
    private static extern IntPtr GetLastKeyboardDevice();

    [DllImport("RawInputPlugin.dll")]
    private static extern ushort GetLastKeyPressed();

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    private void Start()
    {
        IntPtr hwnd = GetUnityWindowHandle();
        RegisterRawInput(hwnd);
    }

    private IntPtr GetUnityWindowHandle()
    {
        IntPtr hwnd = GetActiveWindow();

        if (hwnd == IntPtr.Zero)
        {
            Debug.LogError("Failed to find Unity window handle.");
        }
        else
        {
            Debug.Log($"Unity window handle found: {hwnd}");
        }

        return hwnd;
    }

    public (IntPtr keyboardDevice, ushort keyPressed) GetLastInput()
    {
        return (GetLastKeyboardDevice(), GetLastKeyPressed());
    }
}
