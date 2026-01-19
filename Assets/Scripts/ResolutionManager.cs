using UnityEngine;
using TMPro;

public class ResolutionManager : MonoBehaviour
{
    [Header("Resolution Settings")]
    [Tooltip("Text component that displays the current resolution")]
    public TextMeshProUGUI resolutionText;
    
    [Tooltip("Available resolutions")]
    public Resolution[] availableResolutions = new Resolution[]
    {
        new Resolution { width = 1280, height = 720 },
        new Resolution { width = 1600, height = 900 },
        new Resolution { width = 1920, height = 1080 },
        new Resolution { width = 2560, height = 1440 },
        new Resolution { width = 3840, height = 2160 }
    };
    
    [Header("Fullscreen Settings")]
    [Tooltip("Text component that displays On/Off")]
    public TextMeshProUGUI fullscreenText;
    
    private int currentIndex = 0;
    private bool isFullscreen = true;

    [System.Serializable]
    public struct Resolution
    {
        public int width;
        public int height;
    }

    void Start()
    {
        // Find the current resolution and set index
        int currentWidth = Screen.width;
        int currentHeight = Screen.height;
        
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            if (availableResolutions[i].width == currentWidth && availableResolutions[i].height == currentHeight)
            {
                currentIndex = i;
                break;
            }
        }
        
        // Get current fullscreen state
        isFullscreen = Screen.fullScreen;
        
        UpdateResolutionDisplay();
        UpdateFullscreenDisplay();
    }

    /// <summary>
    /// Cycle to the previous resolution (called by Left Action)
    /// </summary>
    public void PreviousResolution()
    {
        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = availableResolutions.Length - 1;
        }
        Debug.Log($"Previous Resolution: Index {currentIndex}");
        UpdateResolutionDisplay();
    }

    /// <summary>
    /// Cycle to the next resolution (called by Right Action)
    /// </summary>
    public void NextResolution()
    {
        currentIndex++;
        if (currentIndex >= availableResolutions.Length)
        {
            currentIndex = 0;
        }
        Debug.Log($"Next Resolution: Index {currentIndex}");
        UpdateResolutionDisplay();
    }

    /// <summary>
    /// Apply the currently selected resolution (called by Confirm Action)
    /// </summary>
    public void ApplyResolution()
    {
        Resolution selectedRes = availableResolutions[currentIndex];
        Screen.SetResolution(selectedRes.width, selectedRes.height, Screen.fullScreenMode);
        Debug.Log($"Resolution changed to {selectedRes.width}x{selectedRes.height}");
    }

    /// <summary>
    /// Toggle fullscreen (called by Left or Right Action)
    /// </summary>
    public void ToggleFullscreen()
    {
        isFullscreen = !isFullscreen;
        Debug.Log($"Fullscreen toggled to: {(isFullscreen ? "On" : "Off")}");
        UpdateFullscreenDisplay();
    }

    /// <summary>
    /// Apply the fullscreen setting (called by Confirm Action)
    /// </summary>
    public void ApplyFullscreen()
    {
        Screen.fullScreen = isFullscreen;
        Debug.Log($"Fullscreen set to: {(isFullscreen ? "On" : "Off")}");
    }

    private void UpdateResolutionDisplay()
    {
        if (resolutionText != null)
        {
            Resolution currentRes = availableResolutions[currentIndex];
            resolutionText.text = $"{currentRes.width}x{currentRes.height}";
            Debug.Log($"Resolution display updated to: {currentRes.width}x{currentRes.height}");
            
            // Force layout update to fix spacing
            Canvas.ForceUpdateCanvases();
            RectTransform parentRect = resolutionText.rectTransform.parent as RectTransform;
            if (parentRect != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }
        }
        else
        {
            Debug.LogWarning("Resolution Text is not assigned!");
        }
    }

    private void UpdateFullscreenDisplay()
    {
        if (fullscreenText != null)
        {
            fullscreenText.text = isFullscreen ? "On" : "Off";
            Debug.Log($"Fullscreen display updated to: {fullscreenText.text}");
            
            // Force layout update to fix spacing
            Canvas.ForceUpdateCanvases();
            RectTransform parentRect = fullscreenText.rectTransform.parent as RectTransform;
            if (parentRect != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }
        }
        else
        {
            Debug.LogWarning("Fullscreen Text is not assigned!");
        }
    }
}
