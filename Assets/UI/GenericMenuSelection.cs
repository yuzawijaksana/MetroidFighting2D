using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using TMPro;
using System.Collections;

public class GenericMenuSelection : MonoBehaviour
{
    public enum MenuType
    {
        MainMenu,
        PlayMode,
        StoryMode,
        Options,
        Keybind
    }

    [Header("Menu Context")]
    public MenuType menuType;

    [Header("Menu Buttons (Order matters)")]
    [Tooltip("Assign menu buttons in order. The LAST button should be the 'Back' button for ESC to work.")]
    public GameObject[] menuButtons;

    [Header("Button Actions (Order must match buttons)")]
    [Tooltip("Assign UnityEvents for each button. The LAST action should be for the 'Back' button.")]
    public UnityEvent[] buttonActions;
    [Tooltip("Optional: Actions triggered when pressing LEFT on a menu item (for settings like resolution)")]
    public UnityEvent[] leftActions;
    [Tooltip("Optional: Actions triggered when pressing RIGHT on a menu item (for settings like resolution)")]
    public UnityEvent[] rightActions;

    [Header("Selection Indicator")]
    [Tooltip("Indicator that will appear on both sides of the selected menu item")]
    public GameObject selectionIndicator;
    [Tooltip("Offset from the text edge (X = horizontal spacing, Y = vertical adjustment)")]
    public Vector2 indicatorOffset = new Vector2(5f, 0f);
    [Tooltip("Speed of indicator movement animation (higher = faster)")]
    public float animationSpeed = 15f;
    [Tooltip("Allow menu selection to wrap around (bottom to top and vice versa)")]
    public bool wrapAround = true;

    [Header("Input Settings")]
    [Tooltip("Keyboard key for confirm action")]
    public Key confirmKey = Key.Enter;
    [Tooltip("Gamepad button for confirm action")]
    public string confirmButton = "buttonSouth"; // X button
    [Tooltip("Keyboard key for back action")]
    public Key backKey = Key.Escape;
    [Tooltip("Gamepad button for back action")]
    public string backButton = "buttonEast"; // Circle button

    [Header("Sound Settings")]
    [Tooltip("Sound effect when confirming selection (optional)")]
    public AudioClip confirmSound;
    [Tooltip("Sound effect when going back (optional)")]
    public AudioClip backSound;
    [Tooltip("Sound effect when moving selection (optional)")]
    public AudioClip navigationSound;
    [Tooltip("Volume for menu sounds")]
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    private GameInputs gameInputs;
    private AudioSource audioSource;
    private GameObject leftIndicator;
    private GameObject rightIndicator;
    private Vector3 targetLeftPosition;
    private Vector3 targetRightPosition;
    private int selectedIndex = 0;
    private Vector3 lastMousePosition;
    private bool mouseMoved;
    private Vector2 previousMovement = Vector2.zero;

    void Start()
    {
        selectedIndex = 0; // Always start from 0
        
        // Get or add AudioSource for menu sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // Get input system from InputRebindingManager to use rebindings
        var rebindManager = InputRebindingManager.Instance;
        if (rebindManager != null)
        {
            gameInputs = rebindManager.gameInputs;
        }
        else
        {
            Debug.LogWarning("InputRebindingManager not found! Menu controls may not reflect custom bindings.");
            gameInputs = new GameInputs();
        }
        gameInputs.Enable();
        
        // Create left and right indicators
        if (selectionIndicator != null)
        {
            // Use the original as the right indicator
            rightIndicator = selectionIndicator;
            rightIndicator.SetActive(true);
            
            // Find or create left indicator
            string leftIndicatorName = selectionIndicator.name + "_Left";
            Transform parent = selectionIndicator.transform.parent;
            
            // Search for existing left indicator
            foreach (Transform child in parent)
            {
                if (child.gameObject.name == leftIndicatorName)
                {
                    leftIndicator = child.gameObject;
                    break;
                }
            }
            
            // Create only if not found
            if (leftIndicator == null)
            {
                leftIndicator = Instantiate(selectionIndicator, parent);
                leftIndicator.name = leftIndicatorName;
                
                // Flip the left indicator horizontally
                RectTransform leftRect = leftIndicator.GetComponent<RectTransform>();
                if (leftRect != null)
                {
                    leftRect.localScale = new Vector3(-leftRect.localScale.x, leftRect.localScale.y, leftRect.localScale.z);
                }
            }
        }
        
        lastMousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector3.zero;
        mouseMoved = false;
        
        // Wait for end of frame to ensure all layout groups have calculated positions
        StartCoroutine(InitializeIndicators());
    }

    private System.Collections.IEnumerator InitializeIndicators()
    {
        // Wait a frame for layout groups to initialize
        yield return null;
        
        // Force rebuild the layout if there's a layout group
        if (menuButtons.Length > 0 && menuButtons[0] != null)
        {
            Transform parent = menuButtons[0].transform.parent;
            if (parent != null)
            {
                var layoutGroup = parent.GetComponent<UnityEngine.UI.LayoutGroup>();
                if (layoutGroup != null)
                {
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(parent.GetComponent<RectTransform>());
                }
            }
        }
        
        HighlightSelection();
    }

    /// <summary>
    /// Force all layout groups to rebuild immediately. Call this after SetActive changes.
    /// Can be called from UnityEvents in button actions.
    /// </summary>
    public void ForceLayoutUpdate()
    {
        Canvas.ForceUpdateCanvases();
        
        // Force rebuild all layout groups in this menu
        if (menuButtons.Length > 0 && menuButtons[0] != null)
        {
            Transform parent = menuButtons[0].transform.parent;
            if (parent != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(parent.GetComponent<RectTransform>());
            }
        }
        
        // Update indicator positions after layout changes
        HighlightSelection();
    }

    void OnEnable()
    {
        // Reset selection to top when menu is shown
        selectedIndex = 0;
        
        // Ensure indicators are shown when menu becomes active
        StartCoroutine(ShowIndicatorsDelayed());
    }
    
    private IEnumerator ShowIndicatorsDelayed()
    {
        // Wait one frame to ensure layout is ready
        yield return null;
        
        if (leftIndicator != null)
        {
            leftIndicator.SetActive(true);
        }
        if (rightIndicator != null)
        {
            rightIndicator.SetActive(true);
        }
        
        HighlightSelection();
    }

    void OnDestroy()
    {
        // Clean up the duplicated left indicator
        if (leftIndicator != null)
        {
            Destroy(leftIndicator);
        }
        
        if (gameInputs != null)
        {
            gameInputs.Disable();
            gameInputs.Dispose();
        }
    }

    void OnDisable()
    {
        // Hide indicators when menu is disabled
        if (leftIndicator != null)
        {
            leftIndicator.SetActive(false);
        }
        if (rightIndicator != null)
        {
            rightIndicator.SetActive(false);
        }
    }

    void Update()
    {
        // Update indicator positions every frame to handle resolution changes
        UpdateIndicatorPositions();

        // Track mouse movement (immediate)
        Vector3 currentMousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : lastMousePosition;
        mouseMoved = (currentMousePosition - lastMousePosition).sqrMagnitude > 0.01f * 0.01f;
        lastMousePosition = currentMousePosition;

        bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        // Get movement input from analog stick and rebound keys
        Vector2 movement = gameInputs.Player.Movement.ReadValue<Vector2>();
        var keyboard = Keyboard.current;
        var gamepad = Gamepad.current;
        
        // Detect when movement just started (not held) for snappy menu navigation
        bool upPressed = (movement.y > 0.5f && previousMovement.y <= 0.5f) || (gamepad != null && gamepad.dpad.up.wasPressedThisFrame);
        bool downPressed = (movement.y < -0.5f && previousMovement.y >= -0.5f) || (gamepad != null && gamepad.dpad.down.wasPressedThisFrame);
        bool leftPressed = (movement.x < -0.5f && previousMovement.x >= -0.5f) || (gamepad != null && gamepad.dpad.left.wasPressedThisFrame);
        bool rightPressed = (movement.x > 0.5f && previousMovement.x <= 0.5f) || (gamepad != null && gamepad.dpad.right.wasPressedThisFrame);
        
        // Store current movement for next frame
        previousMovement = movement;
        
        // Confirm button - configurable in inspector (default: X button or Enter)
        bool confirmPressed = (keyboard != null && keyboard[confirmKey].wasPressedThisFrame) ||
                             (gamepad != null && GetGamepadButton(gamepad, confirmButton));
        
        // Back button - configurable in inspector (default: Circle button or Escape)
        bool backPressed = (keyboard != null && keyboard[backKey].wasPressedThisFrame) ||
                          (gamepad != null && GetGamepadButton(gamepad, backButton));

        // Mouse hover detection only if mouse moved this frame
        if (mouseMoved)
        {
            for (int i = 0; i < menuButtons.Length; i++)
            {
                if (menuButtons[i] == null) continue;
                var rectTransform = menuButtons[i].GetComponent<RectTransform>();
                if (rectTransform == null) continue;

                Vector2 localMousePosition;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform,
                    currentMousePosition,
                    null,
                    out localMousePosition
                );
                if (rectTransform.rect.Contains(localMousePosition))
                {
                    if (selectedIndex != i)
                    {
                        selectedIndex = i;
                        PlaySound(navigationSound);
                        HighlightSelection();
                    }
                    if (mouseClicked)
                    {
                        HandleSelectionByIndex(i);
                    }
                }
            }
        }

        // Up navigation - analog stick or D-pad/arrow keys
        if ((movement.y > 0.5f && !IsMovementInputHeld()) || upPressed)
        {
            int nextIndex = selectedIndex;
            do
            {
                nextIndex--;
                if (nextIndex < 0)
                {
                    nextIndex = wrapAround ? menuButtons.Length - 1 : 0;
                    if (!wrapAround) break;
                }
            } while (menuButtons[nextIndex] == null && nextIndex != selectedIndex);
            
            if (menuButtons[nextIndex] != null)
            {
                selectedIndex = nextIndex;
                PlaySound(navigationSound);
                HighlightSelection();
            }
            SetMovementInputHeld(true);
        }
        // Down navigation - analog stick or D-pad/arrow keys
        else if ((movement.y < -0.5f && !IsMovementInputHeld()) || downPressed)
        {
            int nextIndex = selectedIndex;
            do
            {
                nextIndex++;
                if (nextIndex >= menuButtons.Length)
                {
                    nextIndex = wrapAround ? 0 : menuButtons.Length - 1;
                    if (!wrapAround) break;
                }
            } while (menuButtons[nextIndex] == null && nextIndex != selectedIndex);
            
            if (menuButtons[nextIndex] != null)
            {
                selectedIndex = nextIndex;
                PlaySound(navigationSound);
                HighlightSelection();
            }
            SetMovementInputHeld(true);
        }
        else if (Mathf.Abs(movement.y) < 0.1f && !upPressed && !downPressed)
        {
            SetMovementInputHeld(false);
        }

        // Horizontal actions (left/right for changing settings)
        if (leftPressed && leftActions != null && selectedIndex < leftActions.Length && leftActions[selectedIndex] != null)
        {
            PlaySound(navigationSound);
            leftActions[selectedIndex].Invoke();
        }
        
        if (rightPressed && rightActions != null && selectedIndex < rightActions.Length && rightActions[selectedIndex] != null)
        {
            PlaySound(navigationSound);
            rightActions[selectedIndex].Invoke();
        }

        // Confirm selection (Triangle button or Enter)
        if (menuButtons[selectedIndex] != null && confirmPressed)
        {
            PlaySound(confirmSound);
            StartCoroutine(DelayedAction(buttonActions[selectedIndex]));
        }
        
        // Back button (Circle button or Escape) - triggers the last button action
        // Don't allow back on Main Menu
        if (backPressed && menuType != MenuType.MainMenu)
        {
            PlaySound(backSound);
            int backIndex = menuButtons.Length - 1;
            if (backIndex >= 0 && menuButtons[backIndex] != null && buttonActions != null && backIndex < buttonActions.Length && buttonActions[backIndex] != null)
            {
                StartCoroutine(DelayedAction(buttonActions[backIndex]));
            }
        }

    }

    private bool GetGamepadButton(Gamepad gamepad, string buttonName)
    {
        switch (buttonName)
        {
            case "buttonSouth": return gamepad.buttonSouth.wasPressedThisFrame;
            case "buttonEast": return gamepad.buttonEast.wasPressedThisFrame;
            case "buttonWest": return gamepad.buttonWest.wasPressedThisFrame;
            case "buttonNorth": return gamepad.buttonNorth.wasPressedThisFrame;
            default: return false;
        }
    }

    private bool isMovementHeld = false;
    
    private bool IsMovementInputHeld()
    {
        return isMovementHeld;
    }
    
    private void SetMovementInputHeld(bool held)
    {
        isMovementHeld = held;
    }

    private void HandleSelectionByIndex(int index)
    {
        // The actual logic for what happens when you select/click a button
        // is NOT in this script. Instead, you assign UnityEvents in the Inspector
        // for each button (in the buttonActions array).
        //
        // For example, in the MainMenu, you might assign:
        //   - Play: SetActive(playGrid, true), SetActive(mainMenuGrid, false)
        //   - Options: SetActive(optionsGrid, true), SetActive(mainMenuGrid, false)
        //   - Exit: Application.Quit()
        //
        // In PlayMode, you might assign:
        //   - Local: SetActive(characterSelectionPanel, true), SetActive(playGrid, false)
        //   - Online: Show "Coming Soon" popup, etc.
        //   - Back: SetActive(playGrid, true), SetActive(thisMenu, false)
        //
        // This script just calls the UnityEvent for the selected index:
        if (buttonActions != null && index < buttonActions.Length && buttonActions[index] != null)
        {
            // Execute button action immediately
            buttonActions[index].Invoke();
        }
    }

    private IEnumerator DelayedAction(UnityEvent action)
    {
        // Wait a small amount to ensure sound plays before menu transition
        yield return new WaitForSeconds(0.1f);
        
        if (action != null)
        {
            action.Invoke();
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            Debug.Log($"Playing sound: {clip.name} at volume {soundVolume}");
            audioSource.PlayOneShot(clip, soundVolume);
        }
        else
        {
            if (audioSource == null) Debug.LogWarning("AudioSource is null!");
            if (clip == null) Debug.LogWarning("AudioClip is null!");
        }
    }

    private void HighlightSelection()
    {
        // Calculate target positions when selection changes
        if (leftIndicator != null && rightIndicator != null && selectedIndex >= 0 && selectedIndex < menuButtons.Length && menuButtons[selectedIndex] != null)
        {
            leftIndicator.SetActive(true);
            rightIndicator.SetActive(true);
            
            TextMeshProUGUI buttonText = menuButtons[selectedIndex].GetComponentInChildren<TextMeshProUGUI>();
            
            if (buttonText != null)
            {
                // Force update to get accurate text rendering
                buttonText.ForceMeshUpdate();
                
                RectTransform textRect = buttonText.GetComponent<RectTransform>();
                
                // Get the world corners of the text rect
                Vector3[] textCorners = new Vector3[4];
                textRect.GetWorldCorners(textCorners);
                
                // textCorners: 0 = bottom-left, 1 = top-left, 2 = top-right, 3 = bottom-right
                float leftEdgeX = textCorners[0].x; // Left edge
                float rightEdgeX = textCorners[2].x; // Right edge
                float centerY = (textCorners[0].y + textCorners[1].y) / 2f;
                
                // Use resolution-independent offset based on canvas reference resolution
                Canvas canvas = buttonText.canvas;
                CanvasScaler canvasScaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
                float scaleFactor = 1f;
                
                if (canvasScaler != null && canvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    // Calculate scale based on reference resolution
                    float referenceWidth = canvasScaler.referenceResolution.x;
                    float currentWidth = Screen.width;
                    scaleFactor = currentWidth / referenceWidth;
                }
                
                float spacingX = indicatorOffset.x * scaleFactor;
                float spacingY = indicatorOffset.y * scaleFactor;
                
                // Set target positions (will be lerped to in UpdateIndicatorPositions)
                targetRightPosition = new Vector3(
                    rightEdgeX + spacingX,
                    centerY + spacingY,
                    textRect.position.z
                );
                
                targetLeftPosition = new Vector3(
                    leftEdgeX - spacingX,
                    centerY + spacingY,
                    textRect.position.z
                );
            }
        }
        else if (leftIndicator != null && rightIndicator != null)
        {
            leftIndicator.SetActive(false);
            rightIndicator.SetActive(false);
        }
    }

    private void UpdateIndicatorPositions()
    {
        // Smoothly animate indicators to target positions every frame
        if (leftIndicator != null && rightIndicator != null && leftIndicator.activeSelf)
        {
            RectTransform leftRect = leftIndicator.GetComponent<RectTransform>();
            RectTransform rightRect = rightIndicator.GetComponent<RectTransform>();
            
            if (leftRect != null && rightRect != null)
            {
                // Recalculate target positions to handle resolution changes
                if (selectedIndex >= 0 && selectedIndex < menuButtons.Length && menuButtons[selectedIndex] != null)
                {
                    TextMeshProUGUI buttonText = menuButtons[selectedIndex].GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.ForceMeshUpdate();
                        RectTransform textRect = buttonText.GetComponent<RectTransform>();
                        
                        Vector3[] textCorners = new Vector3[4];
                        textRect.GetWorldCorners(textCorners);
                        
                        float leftEdgeX = textCorners[0].x;
                        float rightEdgeX = textCorners[2].x;
                        float centerY = (textCorners[0].y + textCorners[1].y) / 2f;
                        
                        // Recalculate scale factor
                        Canvas canvas = buttonText.canvas;
                        CanvasScaler canvasScaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
                        float scaleFactor = 1f;
                        
                        if (canvasScaler != null && canvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                        {
                            float referenceWidth = canvasScaler.referenceResolution.x;
                            float currentWidth = Screen.width;
                            scaleFactor = currentWidth / referenceWidth;
                        }
                        
                        float spacingX = indicatorOffset.x * scaleFactor;
                        float spacingY = indicatorOffset.y * scaleFactor;
                        
                        targetRightPosition = new Vector3(
                            rightEdgeX + spacingX,
                            centerY + spacingY,
                            textRect.position.z
                        );
                        
                        targetLeftPosition = new Vector3(
                            leftEdgeX - spacingX,
                            centerY + spacingY,
                            textRect.position.z
                        );
                    }
                }
                
                // Lerp to target positions
                leftRect.position = Vector3.Lerp(leftRect.position, targetLeftPosition, Time.deltaTime * animationSpeed);
                rightRect.position = Vector3.Lerp(rightRect.position, targetRightPosition, Time.deltaTime * animationSpeed);
            }
        }
    }
}
