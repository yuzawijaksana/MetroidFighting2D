using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Automatically adjusts button background size based on text word count
/// 1 word = 16x16, 2+ words = 16x32
/// </summary>
[ExecuteAlways]
public class DynamicButtonBackground : MonoBehaviour
{
    [Header("Background Settings")]
    [Tooltip("The Image component to swap sprites on")]
    public Image backgroundImage;
    
    [Header("Sprite Settings")]
    [Tooltip("Sprite when text has 1 word (16x16)")]
    public Sprite singleWordSprite;
    
    [Tooltip("Sprite when text has 2+ words (16x32)")]
    public Sprite multiWordSprite;
    
    [Header("Optional: Auto-detect Text")]
    [Tooltip("Auto-find TextMeshProUGUI in children if not assigned")]
    public TextMeshProUGUI buttonText;
    
    private string previousText = "";
    
    void Start()
    {
        // Auto-find components if not assigned
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }
        
        if (buttonText == null)
        {
            buttonText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        UpdateBackgroundSize();
    }
    
    void Update()
    {
        // Check if text changed
        if (buttonText != null && buttonText.text != previousText)
        {
            previousText = buttonText.text;
            UpdateBackgroundSize();
        }
    }
    
    /// <summary>
    /// Update the background size based on word count
    /// </summary>
    public void UpdateBackgroundSize()
    {
        if (backgroundImage == null || buttonText == null)
            return;
        
        // Count words in the text
        int wordCount = CountWords(buttonText.text);
        
        // Swap sprite based on word count
        if (wordCount <= 1)
        {
            if (singleWordSprite != null)
            {
                backgroundImage.sprite = singleWordSprite;
            }
        }
        else
        {
            if (multiWordSprite != null)
            {
                backgroundImage.sprite = multiWordSprite;
            }
        }
        
        // Optional: Set native size to match sprite dimensions
        backgroundImage.SetNativeSize();
    }
    
    /// <summary>
    /// Count words in a string
    /// </summary>
    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;
        
        // Split by spaces and count non-empty entries
        string[] words = text.Split(new char[] { ' ', '\t', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        return words.Length;
    }
    
    /// <summary>
    /// Call this from editor or code to force update
    /// </summary>
    [ContextMenu("Force Update Background")]
    public void ForceUpdate()
    {
        if (buttonText != null)
        {
            previousText = buttonText.text;
        }
        UpdateBackgroundSize();
        
        Debug.Log($"[DynamicBackground] Updated: '{(buttonText != null ? buttonText.text : "NULL")}' = {CountWords(buttonText != null ? buttonText.text : "")} words");
    }
}
