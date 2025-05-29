using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class VictoryScreenUI : MonoBehaviour
{
    [SerializeField] private Text victoryLabel;
    [SerializeField] private Image artworkImage; // Assign this in the inspector
    [SerializeField] private GameObject characterSelection; // Reference the GameObject directly

    private void Update()
    {
        if (!gameObject.activeSelf || Keyboard.current == null) return;

        // Rematch with Enter
        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            OnRematch();
        }
        // Character selection with Space
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            OnCharacterSelect();
        }
    }

    // Show the winner's name and artwork
    public void Show(string winnerName, CharacterCard winnerCard = null)
    {
        victoryLabel.text = winnerName + " wins!";
        if (artworkImage != null && winnerCard != null && winnerCard.characterSprite != null)
            artworkImage.sprite = winnerCard.characterSprite;
        gameObject.SetActive(true);
    }

    private void OnRematch()
    {
        GameStarter.Instance.Rematch();
        gameObject.SetActive(false);
    }

    private void OnCharacterSelect()
    {
        // Reset character selection logic
        if (characterSelection != null)
        {
            var characterSelectionComponent = characterSelection.GetComponent<CharacterSelection>();
            if (characterSelectionComponent != null)
            {
                characterSelectionComponent.ResetSelection();
            }
            characterSelection.SetActive(true); // Ensure CharacterSelection GameObject is active
        }
        else
        {
            Debug.LogWarning("CharacterSelection GameObject reference is not assigned.");
        }
        gameObject.SetActive(false);
    }
}