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

        // Remove characters and in-game UI
        if (GameStarter.Instance != null)
        {
            // Destroy all players
            foreach (Transform child in GameStarter.Instance.playersParent.transform)
            {
                if (child != null)
                    Destroy(child.gameObject);
            }

            // Clear in-game UI
            if (GameStarter.Instance.ingameGridUI != null)
            {
                foreach (Transform child in GameStarter.Instance.ingameGridUI.transform)
                {
                    if (child != null)
                        Destroy(child.gameObject);
                }
                GameStarter.Instance.ingameGridUI.transform.parent.gameObject.SetActive(false);
            }
        }

        gameObject.SetActive(true);
    }

    private void OnRematch()
    {
        // Add back in-game UI
        if (GameStarter.Instance != null)
        {
            if (GameStarter.Instance.ingameGridUI != null)
            {
                GameStarter.Instance.ingameGridUI.transform.parent.gameObject.SetActive(true);
            }
        }

        GameStarter.Instance.Rematch();
        gameObject.SetActive(false);
    }

    private void OnCharacterSelect()
    {
        // Activate CharacterSelection if assigned
        if (characterSelection != null)
        {
            characterSelection.SetActive(true);
        }

        // Clear references in GameStarter
        if (GameStarter.Instance != null)
        {
            GameStarter.Instance.player1Card = null;
            GameStarter.Instance.player2Card = null;
            GameStarter.Instance.player1Prefab = null;
            GameStarter.Instance.player2Prefab = null;
        }

        gameObject.SetActive(false);
    }
}