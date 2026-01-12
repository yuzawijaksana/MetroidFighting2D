using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

[System.Serializable]
public class DialogLine
{
    [TextArea(2, 4)]
    public string text;
    public Sprite characterPortrait;
    public string characterName;
}

public class StoryDialogTrigger : MonoBehaviour
{
    [Header("Dialog Content")]
    [SerializeField] private List<DialogLine> dialogLines = new List<DialogLine>();
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private bool pauseGameDuringDialog = true;
    
    [Header("Dialog Panel UI")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TextMeshProUGUI dialogText;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private Image characterPortrait;
    
    [Header("Continue Indicator")]
    [SerializeField] private GameObject continueButton; // DEPRECATED
    [SerializeField] private TextMeshProUGUI continueIndicator; // Blinking ♦ symbol
    
    [Header("Typewriter Effect")]
    [SerializeField] private bool useTypewriterEffect = true;
    [SerializeField] private float typewriterSpeed = 0.05f;
    [SerializeField] private bool canSkipTypewriter = true;
    [SerializeField] private AudioSource typewriterAudioSource;
    [SerializeField] private AudioClip typewriterSound;
    
    [Header("Trigger Detection")]
    [SerializeField] private bool useTriggerZone = true;
    [SerializeField] private bool requireSpecificAttacker = false;
    [SerializeField] private string requiredAttackerTag = "Player";
    [SerializeField] private List<string> ignoreTheseTagsInTrigger = new List<string>() { "PlayerAttack" };
    [SerializeField] private float minDamageToTrigger = 0f;
    
    [Header("Interaction Prompts (Before Dialog)")]
    [SerializeField] private bool requireButtonPress = false;
    [SerializeField] private GameObject promptUI; // Parent container
    [SerializeField] private GameObject keyboardInteractPrompt; // "Press ↑ to talk"
    [SerializeField] private GameObject gamepadInteractPrompt; // "Press D-Pad ↑ to talk"
    [SerializeField] private float promptHeightOffset = 2f;
    
    [Header("Input Settings")]
    [SerializeField] private bool useMouseClick = true;
    [SerializeField] private bool useKeyboard = true;
    [SerializeField] private bool useGamepad = true;
    
    [Header("Events")]
    [SerializeField] private UnityEngine.Events.UnityEvent onDialogStart;
    [SerializeField] private UnityEngine.Events.UnityEvent onDialogEnd;
    
    private bool hasTriggered = false;
    private int currentLineIndex = 0;
    private bool isShowingDialog = false;
    private bool isTyping = false;
    private Coroutine typewriterCoroutine;
    private string fullText = "";
    private bool playerInRange = false;
    private GameInputs controls;
    private Coroutine blinkCoroutine;
    
    // Static property to check if ANY dialog is showing
    public static bool IsAnyDialogActive { get; private set; } = false;
    public static float InputCooldownTimer { get; private set; } = 0f;
    
    private void Awake()
    {
        controls = new GameInputs();
    }
    
    private void OnEnable()
    {
        controls.Player.Enable();
    }
    
    private void OnDisable()
    {
        controls.Player.Disable();
    }
    
    void Start()
    {
        // Hide dialog panel at start
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
            
        // Hide continue indicator
        if (continueIndicator != null)
            continueIndicator.gameObject.SetActive(false);
            
        // Position and hide prompt UI
        if (promptUI != null)
        {
            PositionPrompt();
            promptUI.SetActive(false);
        }
        
        if (keyboardInteractPrompt != null)
        {
            PositionPrompt(keyboardInteractPrompt);
            keyboardInteractPrompt.SetActive(false);
        }
        
        if (gamepadInteractPrompt != null)
        {
            PositionPrompt(gamepadInteractPrompt);
            gamepadInteractPrompt.SetActive(false);
        }
        
        // Subscribe to device changes
        if (InputDeviceDetector.Instance != null)
        {
            InputDeviceDetector.Instance.OnDeviceChanged.AddListener(OnInputDeviceChanged);
        }
    }
    
    private void PositionPrompt()
    {
        if (promptUI != null)
        {
            // Position prompt above this object
            Vector3 promptPosition = transform.position;
            promptPosition.y += promptHeightOffset;
            promptUI.transform.position = promptPosition;
        }
    }
    
    private void PositionPrompt(GameObject prompt)
    {
        if (prompt != null)
        {
            // Position prompt above this object
            Vector3 promptPosition = transform.position;
            promptPosition.y += promptHeightOffset;
            prompt.transform.position = promptPosition;
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from device changes
        if (InputDeviceDetector.Instance != null)
        {
            InputDeviceDetector.Instance.OnDeviceChanged.RemoveListener(OnInputDeviceChanged);
        }
    }
    
    private void OnInputDeviceChanged(InputDeviceType deviceType)
    {
        // Update interact prompt if player is in range and dialog isn't showing
        if (playerInRange && !isShowingDialog && requireButtonPress)
        {
            bool isGamepad = deviceType == InputDeviceType.Gamepad;
            
            if (keyboardInteractPrompt != null)
            {
                keyboardInteractPrompt.SetActive(true); // Keep active
                SpriteRenderer sr = keyboardInteractPrompt.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.enabled = !isGamepad;
            }
                
            if (gamepadInteractPrompt != null)
            {
                gamepadInteractPrompt.SetActive(true); // Keep active
                SpriteRenderer sr = gamepadInteractPrompt.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.enabled = isGamepad;
            }
        }
    }
    
    void Update()
    {
        // Count down cooldown timer
        if (InputCooldownTimer > 0)
        {
            InputCooldownTimer -= Time.unscaledDeltaTime; // Use unscaled time in case game is paused
            if (InputCooldownTimer < 0) InputCooldownTimer = 0;
        }
        
        // Check for up button when player is in range
        if (playerInRange && requireButtonPress && !isShowingDialog)
        {
            if (controls.Player.Interact.WasPressedThisFrame())
            {
                Debug.Log("Interact button pressed - triggering dialog!");
                TriggerDialog();
            }
        }
        
        // Advance dialog with various inputs
        if (isShowingDialog)
        {
            bool inputDetected = false;
            
            // Jump button (Space/A) to continue
            if (useKeyboard && controls.Player.Jump.WasPressedThisFrame())
            {
                inputDetected = true;
            }
            
            // Mouse click to continue
            if (useMouseClick && Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
            {
                inputDetected = true;
            }
            
            if (inputDetected)
            {
                // Skip typewriter if currently typing
                if (isTyping && canSkipTypewriter)
                {
                    SkipTypewriter();
                }
                // Advance to next line if not typing
                else if (!isTyping)
                {
                    NextLine();
                }
            }
        }
    }
    
    // Called when this object takes damage
    public void TakeDamage(float damage, Vector2 knockback, GameObject attacker)
    {
        // Check if already triggered
        if (triggerOnce && hasTriggered)
            return;
        
        // Check minimum damage requirement
        if (damage < minDamageToTrigger)
        {
            Debug.Log($"Damage {damage} below minimum {minDamageToTrigger}. Dialog not triggered.");
            return;
        }
        
        // Check if we require a specific attacker
        if (requireSpecificAttacker && attacker != null)
        {
            if (!attacker.CompareTag(requiredAttackerTag))
            {
                Debug.Log($"Attacker tag '{attacker.tag}' doesn't match required '{requiredAttackerTag}'. Dialog not triggered.");
                return;
            }
        }
        
        // Trigger dialog
        TriggerDialog();
    }
    
    // Trigger zone detection
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTriggerZone) return;
        
        Debug.Log($"Something entered trigger: {other.gameObject.name} with tag: {other.tag}");
        
        // Ignore specific tags (like PlayerAttack)
        foreach (string ignoreTag in ignoreTheseTagsInTrigger)
        {
            if (other.CompareTag(ignoreTag))
            {
                Debug.Log($"Dialog trigger ignored tag: {ignoreTag}");
                return;
            }
        }
        
        // Check if the object entering has the required tag
        if (requireSpecificAttacker && !other.CompareTag(requiredAttackerTag))
        {
            Debug.Log($"Tag mismatch! Expected '{requiredAttackerTag}' but got '{other.tag}'");
            return;
        }
        
        // If require button press, just mark player in range
        if (requireButtonPress)
        {
            playerInRange = true;
            
            // Show appropriate prompt based on input device
            if (InputDeviceDetector.Instance != null)
            {
                bool isGamepad = InputDeviceDetector.Instance.IsUsingGamepad();
                
                if (keyboardInteractPrompt != null)
                {
                    PositionPrompt(keyboardInteractPrompt);
                    keyboardInteractPrompt.SetActive(true); // Always activate GameObject first
                    SpriteRenderer sr = keyboardInteractPrompt.GetComponent<SpriteRenderer>();
                    if (sr != null)
                        sr.enabled = !isGamepad;
                }
                
                if (gamepadInteractPrompt != null)
                {
                    PositionPrompt(gamepadInteractPrompt);
                    gamepadInteractPrompt.SetActive(true); // Always activate GameObject first
                    SpriteRenderer sr = gamepadInteractPrompt.GetComponent<SpriteRenderer>();
                    if (sr != null)
                        sr.enabled = isGamepad;
                }
            }
            else
            {
                // Fallback: Show keyboard prompt if InputDeviceDetector is missing
                Debug.LogWarning("InputDeviceDetector not found! Add it to your scene. Defaulting to keyboard prompts.");
                
                if (keyboardInteractPrompt != null)
                {
                    PositionPrompt(keyboardInteractPrompt);
                    keyboardInteractPrompt.SetActive(true); // Always activate GameObject first
                    SpriteRenderer sr = keyboardInteractPrompt.GetComponent<SpriteRenderer>();
                    if (sr != null)
                        sr.enabled = true;
                }
                
                if (gamepadInteractPrompt != null)
                {
                    gamepadInteractPrompt.SetActive(true); // Always activate GameObject first
                    SpriteRenderer sr = gamepadInteractPrompt.GetComponent<SpriteRenderer>();
                    if (sr != null)
                        sr.enabled = false;
                }
            }
            
            // Always show promptUI if assigned (as base/parent for prompts)
            if (promptUI != null)
            {
                PositionPrompt();
                promptUI.SetActive(true);
            }
            
            Debug.Log("Player in range. Press UP to start dialog.");
        }
        else
        {
            // Auto-trigger dialog
            TriggerDialog();
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!useTriggerZone) return;
        
        // Ignore specific tags
        foreach (string ignoreTag in ignoreTheseTagsInTrigger)
        {
            if (other.CompareTag(ignoreTag))
                return;
        }
        
        // Check if the object exiting has the required tag
        if (requireSpecificAttacker && !other.CompareTag(requiredAttackerTag))
            return;
        
        // Player left the area
        playerInRange = false;
        if (promptUI != null)
            promptUI.SetActive(false);
        if (keyboardInteractPrompt != null)
            keyboardInteractPrompt.SetActive(false);
        if (gamepadInteractPrompt != null)
            gamepadInteractPrompt.SetActive(false);
    }
    
    // Alternative: Call this directly if you don't want to use the damage system
    public void TriggerDialog()
    {
        if (triggerOnce && hasTriggered)
            return;
        
        if (dialogLines.Count == 0)
        {
            Debug.LogWarning("No dialog lines assigned!");
            return;
        }
        
        hasTriggered = true;
        currentLineIndex = 0;
        isShowingDialog = true;
        IsAnyDialogActive = true; // Disable player controls
        
        // Hide prompt UI
        if (promptUI != null)
            promptUI.SetActive(false);
        if (keyboardInteractPrompt != null)
            keyboardInteractPrompt.SetActive(false);
        if (gamepadInteractPrompt != null)
            gamepadInteractPrompt.SetActive(false);
        
        // Pause game if enabled
        if (pauseGameDuringDialog)
            Time.timeScale = 0f;
        
        // Invoke start event
        onDialogStart?.Invoke();
        
        // Show first line
        ShowCurrentLine();
        
        Debug.Log("Story dialog triggered!");
    }
    
    private void ShowCurrentLine()
    {
        if (currentLineIndex >= dialogLines.Count)
        {
            EndDialog();
            return;
        }
        
        DialogLine line = dialogLines[currentLineIndex];
        
        // Show dialog panel
        if (dialogPanel != null)
            dialogPanel.SetActive(true);
        
        // Set character name
        if (characterNameText != null)
            characterNameText.text = line.characterName;
        
        // Set portrait
        if (characterPortrait != null && line.characterPortrait != null)
        {
            characterPortrait.sprite = line.characterPortrait;
            characterPortrait.enabled = true;
        }
        else if (characterPortrait != null)
        {
            characterPortrait.enabled = false;
        }
        
        // Show/hide continue button (if using old system)
        if (continueButton != null)
            continueButton.SetActive(false);
            
        // Hide continue indicator while typing
        if (continueIndicator != null)
            continueIndicator.gameObject.SetActive(false);
        
        // Show text with typewriter effect or instantly
        if (useTypewriterEffect)
        {
            fullText = line.text;
            if (typewriterCoroutine != null)
                StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = StartCoroutine(TypewriterEffect());
        }
        else
        {
            // Show text instantly
            if (dialogText != null)
                dialogText.text = line.text;
                
            // Show continue indicator immediately with blinking
            if (continueIndicator != null)
            {
                continueIndicator.text = "♦";
                continueIndicator.gameObject.SetActive(true);
                if (blinkCoroutine != null)
                    StopCoroutine(blinkCoroutine);
                blinkCoroutine = StartCoroutine(BlinkContinueIndicator());
            }
        }
        
        Debug.Log($"Showing dialog line {currentLineIndex + 1}/{dialogLines.Count}: {line.text}");
    }
    
    private IEnumerator TypewriterEffect()
    {
        isTyping = true;
        dialogText.text = "";
        
        for (int i = 0; i < fullText.Length; i++)
        {
            dialogText.text += fullText[i];
            
            // Play typewriter sound
            if (typewriterAudioSource != null && typewriterSound != null)
            {
                // Don't play sound for spaces
                if (fullText[i] != ' ')
                {
                    typewriterAudioSource.PlayOneShot(typewriterSound);
                }
            }
            
            // Use WaitForSecondsRealtime to work with paused game (Time.timeScale = 0)
            yield return new WaitForSecondsRealtime(typewriterSpeed);
        }
        
        isTyping = false;
        
        // Show continue indicator (♦) with blinking effect
        if (continueIndicator != null)
        {
            continueIndicator.text = "♦";
            continueIndicator.gameObject.SetActive(true);
            if (blinkCoroutine != null)
                StopCoroutine(blinkCoroutine);
            blinkCoroutine = StartCoroutine(BlinkContinueIndicator());
        }
    }
    
    private void SkipTypewriter()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        
        // Show full text immediately
        dialogText.text = fullText;
        isTyping = false;
        
        // Show continue indicator with blinking
        if (continueIndicator != null)
        {
            continueIndicator.text = "♦";
            continueIndicator.gameObject.SetActive(true);
            if (blinkCoroutine != null)
                StopCoroutine(blinkCoroutine);
            blinkCoroutine = StartCoroutine(BlinkContinueIndicator());
        }
    }
    
    private void NextLine()
    {
        currentLineIndex++;
        
        // Stop blinking when advancing
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        
        ShowCurrentLine();
    }
    
    private IEnumerator BlinkContinueIndicator()
    {
        while (true)
        {
            if (continueIndicator != null)
            {
                continueIndicator.enabled = false;
                yield return new WaitForSecondsRealtime(0.5f);
                continueIndicator.enabled = true;
                yield return new WaitForSecondsRealtime(0.5f);
            }
            else
            {
                yield break;
            }
        }
    }
    
    private void EndDialog()
    {
        isShowingDialog = false;
        isTyping = false;
        IsAnyDialogActive = false; // Re-enable player controls
        InputCooldownTimer = 0.2f; // 0.2 second cooldown to prevent accidental input
        
        // Stop blinking
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        
        // Hide dialog panel
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
            
        // Hide continue indicator
        if (continueIndicator != null)
            continueIndicator.gameObject.SetActive(false);
        
        // Resume game if paused
        if (pauseGameDuringDialog)
            Time.timeScale = 1f;
        
        // Show interact prompts again if player is still in range
        if (playerInRange && requireButtonPress)
        {
            if (InputDeviceDetector.Instance != null)
            {
                bool isGamepad = InputDeviceDetector.Instance.IsUsingGamepad();
                
                if (keyboardInteractPrompt != null)
                {
                    keyboardInteractPrompt.SetActive(true);
                    SpriteRenderer sr = keyboardInteractPrompt.GetComponent<SpriteRenderer>();
                    if (sr != null)
                        sr.enabled = !isGamepad;
                }
                
                if (gamepadInteractPrompt != null)
                {
                    gamepadInteractPrompt.SetActive(true);
                    SpriteRenderer sr = gamepadInteractPrompt.GetComponent<SpriteRenderer>();
                    if (sr != null)
                        sr.enabled = isGamepad;
                }
            }
            else
            {
                // Fallback to keyboard prompt
                if (keyboardInteractPrompt != null)
                {
                    keyboardInteractPrompt.SetActive(true);
                    SpriteRenderer sr = keyboardInteractPrompt.GetComponent<SpriteRenderer>();
                    if (sr != null)
                        sr.enabled = true;
                }
            }
            
            if (promptUI != null)
                promptUI.SetActive(true);
        }
        
        // Invoke end event
        onDialogEnd?.Invoke();
        
        Debug.Log("Story dialog ended.");
    }
    
    // Reset to allow triggering again
    public void ResetTrigger()
    {
        hasTriggered = false;
        currentLineIndex = 0;
        isShowingDialog = false;
        isTyping = false;
        
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
    }
    
    // Preview in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
    
    // ========== EASY DIALOG MANAGEMENT ==========
    // Right-click on component in Inspector to access these!
    
    [ContextMenu("Add New Dialog Line")]
    private void AddDialogLine()
    {
        dialogLines.Add(new DialogLine 
        { 
            text = "New dialog text here...", 
            characterName = "Character Name" 
        });
        Debug.Log("Added new dialog line. Total: " + dialogLines.Count);
    }
    
    [ContextMenu("Remove Last Dialog Line")]
    private void RemoveLastDialogLine()
    {
        if (dialogLines.Count > 0)
        {
            dialogLines.RemoveAt(dialogLines.Count - 1);
            Debug.Log("Removed last dialog line. Total: " + dialogLines.Count);
        }
    }
    
    [ContextMenu("Clear All Dialog Lines")]
    private void ClearAllDialogLines()
    {
        dialogLines.Clear();
        Debug.Log("Cleared all dialog lines.");
    }
    
    [ContextMenu("Add Example Dialog")]
    private void AddExampleDialog()
    {
        dialogLines.Clear();
        dialogLines.Add(new DialogLine 
        { 
            text = "Hey there! Welcome to the forest.", 
            characterName = "Squirrel" 
        });
        dialogLines.Add(new DialogLine 
        { 
            text = "You at least are free from it. And free to shake it all up ... if'n that's your choosing. ♦", 
            characterName = "Squirrel" 
        });
        dialogLines.Add(new DialogLine 
        { 
            text = "Good luck on your journey!", 
            characterName = "Squirrel" 
        });
        Debug.Log("Added example dialog with 3 lines.");
    }
}
