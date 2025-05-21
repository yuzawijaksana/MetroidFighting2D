using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CharacterSelection : MonoBehaviour
{
    public List<CharacterCard> characterCards;
    public GameObject characterCardPrefab;
    public GameObject player1Indicator;
    public GameObject player2Indicator;
    private int player1Index = 0;
    private int player2Index = 0;
    private List<GameObject> spawnedCards = new List<GameObject>();
    private Vector2 player1TargetAnchoredPos;
    private Vector2 player2TargetAnchoredPos;
    public float indicatorMoveSpeed = 10f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (CharacterCard card in characterCards)
        {
            spawnCharacterCard(card);
        }
        // Ensure indicators are parented to the grid (this transform)
        if (player1Indicator != null)
        {
            player1Indicator.transform.SetParent(transform, false);
            player1Indicator.transform.SetSiblingIndex(0); // Top of hierarchy (behind grid content)
        }
        if (player2Indicator != null)
        {
            player2Indicator.transform.SetParent(transform, false);
            player2Indicator.transform.SetAsLastSibling(); // Bottom of hierarchy (above grid content)
        }

        StartCoroutine(MoveIndicatorsNextFrame());
    }

    private System.Collections.IEnumerator MoveIndicatorsNextFrame()
    {
        yield return new WaitForEndOfFrame();
        HighlightSelections();
        // Immediately set indicators to correct anchored position at start
        if (player1Indicator != null)
            player1Indicator.GetComponent<RectTransform>().anchoredPosition = player1TargetAnchoredPos;
        if (player2Indicator != null)
            player2Indicator.GetComponent<RectTransform>().anchoredPosition = player2TargetAnchoredPos;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Player 1 input (A/D)
        if (keyboard.aKey.wasPressedThisFrame)
        {
            player1Index = Mathf.Max(0, player1Index - 1);
            HighlightSelections();
        }
        if (keyboard.dKey.wasPressedThisFrame)
        {
            player1Index = Mathf.Min(characterCards.Count - 1, player1Index + 1);
            HighlightSelections();
        }
        // Player 2 input (Left/Right Arrow)
        if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            player2Index = Mathf.Max(0, player2Index - 1);
            HighlightSelections();
        }
        if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            player2Index = Mathf.Min(characterCards.Count - 1, player2Index + 1);
            HighlightSelections();
        }

        // Smoothly move indicators (anchoredPosition)
        if (player1Indicator != null)
        {
            var rect = player1Indicator.GetComponent<RectTransform>();
            rect.anchoredPosition = Vector2.Lerp(
                rect.anchoredPosition,
                player1TargetAnchoredPos,
                Time.deltaTime * indicatorMoveSpeed
            );
        }
        if (player2Indicator != null)
        {
            var rect = player2Indicator.GetComponent<RectTransform>();
            rect.anchoredPosition = Vector2.Lerp(
                rect.anchoredPosition,
                player2TargetAnchoredPos,
                Time.deltaTime * indicatorMoveSpeed
            );
        }
    }

    private void spawnCharacterCard(CharacterCard card)
    {
        GameObject characterCard = Instantiate(characterCardPrefab, transform);
        spawnedCards.Add(characterCard);

        Transform nameTransform = characterCard.transform.Find("Name");
        Transform artworkTransform = characterCard.transform.Find("Mask/Artwork");

        if (nameTransform == null)
        {
            Debug.LogError("Name child not found on characterCardPrefab.");
            return;
        }
        if (artworkTransform == null)
        {
            Debug.LogError("Artwork child not found on characterCardPrefab.");
            return;
        }

        Text characterName = nameTransform.GetComponent<Text>();
        Image artwork = artworkTransform.GetComponent<Image>();

        if (characterName == null)
        {
            Debug.LogError("Text component not found on Name child.");
            return;
        }
        if (artwork == null)
        {
            Debug.LogError("Image component not found on Artwork child.");
            return;
        }

        characterName.text = card.characterName;
        artwork.sprite = card.characterSprite;

        // Set CardDisplay's characterCard property
        CardDisplay display = characterCard.GetComponent<CardDisplay>();
        if (display != null)
        {
            display.characterCard = card;
            display.Refresh();
        }
    }

    private void HighlightSelections()
    {
        // Set target anchored positions for smooth movement
        if (player1Indicator != null && player1Index >= 0 && player1Index < spawnedCards.Count)
        {
            var cardRect = spawnedCards[player1Index].GetComponent<RectTransform>();
            player1TargetAnchoredPos = cardRect.anchoredPosition + new Vector2(0, 70); // Above card
        }
        if (player2Indicator != null && player2Index >= 0 && player2Index < spawnedCards.Count)
        {
            var cardRect = spawnedCards[player2Index].GetComponent<RectTransform>();
            player2TargetAnchoredPos = cardRect.anchoredPosition + new Vector2(0, -70); // Below card
        }
    }
}
