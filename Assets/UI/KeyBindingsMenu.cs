using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple UI controller for the key bindings menu
/// </summary>
public class KeyBindingsMenu : MonoBehaviour
{
    [Header("UI References")]
    public Button resetAllButton;
    public GameObject rebindingPanel;

    void Start()
    {
        if (resetAllButton != null)
        {
            resetAllButton.onClick.AddListener(ResetAllBindings);
        }
    }

    public void ResetAllBindings()
    {
        var rebindManager = InputRebindingManager.Instance;
        if (rebindManager != null)
        {
            rebindManager.ResetAllBindings();
            
            // Update all KeyboardBindingUI components in children
            var rebindUIs = GetComponentsInChildren<KeyboardBindingUI>();
            foreach (var ui in rebindUIs)
            {
                ui.ResetToDefault();
            }
        }
    }

    public void OpenBindingsMenu()
    {
        if (rebindingPanel != null)
        {
            rebindingPanel.SetActive(true);
        }
    }

    public void CloseBindingsMenu()
    {
        if (rebindingPanel != null)
        {
            rebindingPanel.SetActive(false);
        }
    }
}
