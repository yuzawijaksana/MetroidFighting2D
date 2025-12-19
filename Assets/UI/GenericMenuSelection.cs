using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class GenericMenuSelection : MonoBehaviour
{
    public enum MenuType
    {
        MainMenu,
        PlayMode,
        StoryMode,
        Options
    }

    [Header("Menu Context")]
    public MenuType menuType;

    [Header("Menu Buttons (Order matters)")]
    [Tooltip("Assign menu buttons in order. The LAST button should be the 'Back' button for ESC to work.")]
    public GameObject[] menuButtons;

    [Header("Button Actions (Order must match buttons)")]
    [Tooltip("Assign UnityEvents for each button. The LAST action should be for the 'Back' button.")]
    public UnityEvent[] buttonActions;

    private int selectedIndex = 0;
    private Vector3 lastMousePosition;
    private bool mouseMoved;

    void Start()
    {
        selectedIndex = 0; // Always start from 0
        HighlightSelection();
        lastMousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector3.zero;
        mouseMoved = false;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Track mouse movement (immediate)
        Vector3 currentMousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : lastMousePosition;
        mouseMoved = (currentMousePosition - lastMousePosition).sqrMagnitude > 0.01f * 0.01f;
        lastMousePosition = currentMousePosition;

        bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

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
                        HighlightSelection();
                    }
                    if (mouseClicked)
                    {
                        HandleSelectionByIndex(i);
                    }
                }
            }
        }

        // Up navigation
        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
        {
            int nextIndex = selectedIndex;
            do
            {
                nextIndex = Mathf.Max(0, nextIndex - 1);
            } while (nextIndex > 0 && menuButtons[nextIndex] == null);
            if (menuButtons[nextIndex] != null)
            {
                selectedIndex = nextIndex;
                HighlightSelection();
            }
        }
        // Down navigation
        if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
        {
            int nextIndex = selectedIndex;
            do
            {
                nextIndex = Mathf.Min(menuButtons.Length - 1, nextIndex + 1);
            } while (nextIndex < menuButtons.Length - 1 && menuButtons[nextIndex] == null);
            if (menuButtons[nextIndex] != null)
            {
                selectedIndex = nextIndex;
                HighlightSelection();
            }
        }

        // Confirm selection (Space, Enter, J)
        if (menuButtons[selectedIndex] != null &&
            (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.jKey.wasPressedThisFrame))
        {
            HandleSelectionByIndex(selectedIndex);
            selectedIndex = 0; // Reset to 0 after selection
            HighlightSelection();
        }

        // ESC toggle for "back" (last button in menuButtons/buttonActions)
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            int backIndex = menuButtons.Length - 1;
            if (backIndex >= 0 && menuButtons[backIndex] != null && buttonActions != null && backIndex < buttonActions.Length && buttonActions[backIndex] != null)
            {
                selectedIndex = backIndex;
                HighlightSelection();
                HandleSelectionByIndex(backIndex);
                selectedIndex = 0; // Reset to 0 after back
                HighlightSelection();
            }
        }
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
            buttonActions[index].Invoke();
        }
    }

    private void HighlightSelection()
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            var textComponent = menuButtons[i]?.GetComponentInChildren<Text>();
            if (textComponent != null)
            {
                textComponent.color = (i == selectedIndex) ? Color.black : Color.grey;
            }
        }
    }
}
