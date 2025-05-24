using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MainMenuSelection : MonoBehaviour
{
    public GameObject arcadeButton;
    public GameObject settingsButton;
    public GameObject exitButton;
    public GameObject leftIndicator;
    public GameObject rightIndicator;
    public float indicatorMoveSpeed = 10f;
    public GameObject characterSelectionPanel;
    public GameObject mainMenuPanel;

    private int selectedIndex = 0;
    private Vector3 leftTargetAnchoredPos;
    private Vector3 rightTargetAnchoredPos;
    private GameObject[] menuButtons;

    void Start()
    {
        // Initialize menuButtons array in order
        menuButtons = new GameObject[] { arcadeButton, settingsButton, exitButton };

        // Parent indicators to the same parent as the menu buttons for correct alignment
        if (menuButtons.Length > 0 && menuButtons[0] != null)
        {
            Transform menuParent = menuButtons[0].transform.parent;
            if (leftIndicator != null)
                leftIndicator.transform.SetParent(menuParent, false);
            if (rightIndicator != null)
                rightIndicator.transform.SetParent(menuParent, false);
        }

        StartCoroutine(MoveIndicatorsNextFrame());
    }

    private System.Collections.IEnumerator MoveIndicatorsNextFrame()
    {
        yield return new WaitForEndOfFrame();
        HighlightSelection();
        if (leftIndicator != null)
            leftIndicator.GetComponent<RectTransform>().localPosition = leftTargetAnchoredPos;
        if (rightIndicator != null)
            rightIndicator.GetComponent<RectTransform>().localPosition = rightTargetAnchoredPos;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Up/Down navigation (Arrow keys and WASD)
        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
        {
            selectedIndex = Mathf.Max(0, selectedIndex - 1);
            HighlightSelection();
        }
        if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
        {
            selectedIndex = Mathf.Min(menuButtons.Length - 1, selectedIndex + 1);
            HighlightSelection();
        }

        // Confirm selection (Space, Enter, J)
        if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.jKey.wasPressedThisFrame)
        {
            if (selectedIndex == 0)
            {
                // Arcade Mode
                if (characterSelectionPanel != null) characterSelectionPanel.SetActive(true);
                if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            }
            else if (selectedIndex == 2)
            {
                // Exit
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        // Smoothly move indicators (localPosition)
        if (leftIndicator != null)
        {
            var rect = leftIndicator.GetComponent<RectTransform>();
            rect.localPosition = Vector3.Lerp(
                rect.localPosition,
                leftTargetAnchoredPos,
                Time.deltaTime * indicatorMoveSpeed
            );
        }
        if (rightIndicator != null)
        {
            var rect = rightIndicator.GetComponent<RectTransform>();
            rect.localPosition = Vector3.Lerp(
                rect.localPosition,
                rightTargetAnchoredPos,
                Time.deltaTime * indicatorMoveSpeed
            );
        }
    }

    private void HighlightSelection()
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            var text = menuButtons[i]?.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.color = (i == selectedIndex) ? Color.black : Color.grey;
            }
        }

        if (selectedIndex >= 0 && selectedIndex < menuButtons.Length && menuButtons[selectedIndex] != null)
        {
            var buttonRect = menuButtons[selectedIndex].GetComponent<RectTransform>();
            leftTargetAnchoredPos = buttonRect.localPosition + new Vector3(-110, 0, 0);
            rightTargetAnchoredPos = buttonRect.localPosition + new Vector3(110, 0, 0);
        }
    }
}
