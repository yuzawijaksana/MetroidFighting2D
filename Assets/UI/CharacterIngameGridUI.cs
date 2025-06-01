using System.Collections.Generic;
using UnityEngine;

public class CharacterIngameGridUI : MonoBehaviour
{
    [Header("References (Assign in Inspector)")]
    public GameObject cellPrefab; // Prefab with CharacterIngameCellUI attached

    private List<GameObject> spawnedCells = new List<GameObject>();

    // Call this to update the grid with the selected cards
    public void SetCards(List<(CharacterCard card, string label)> selectedCards)
    {
        // Ensure the cellPrefab is valid
        if (cellPrefab == null)
        {
            Debug.LogError("Cell prefab is not assigned in CharacterIngameGridUI.");
            return;
        }

        // Destroy all existing cells
        foreach (var cell in spawnedCells)
        {
            if (cell != null)
                Destroy(cell);
        }
        spawnedCells.Clear();

        // Create new cells for the selected cards
        for (int i = 0; i < selectedCards.Count; i++)
        {
            var selectedCard = selectedCards[i];
            if (selectedCard.card == null)
            {
                Debug.LogWarning("Selected card is null. Skipping.");
                continue;
            }

            GameObject cell = Instantiate(cellPrefab, transform);
            cell.name = $"{selectedCard.label}_DamageUI"; // Rename the cell
            spawnedCells.Add(cell);

            var cellUI = cell.GetComponent<CharacterIngameCellUI>();
            if (cellUI != null)
            {
                cellUI.SetCharacterCard(selectedCard.card, selectedCard.label);
                cellUI.InitHearts(selectedCard.card.characterPrefab.GetComponent<Damageable>()?.maxHearts ?? 3); // Default to 3 hearts
            }
            else
            {
                Debug.LogError("CharacterIngameCellUI component is missing on the cell prefab.");
            }
        }
    }

    public void ClearCells()
    {
        // Destroy all cells managed by the grid
        foreach (var cell in spawnedCells)
        {
            if (cell != null)
                Destroy(cell);
        }
        spawnedCells.Clear();
    }

    public void UpdateAllHearts(Dictionary<int, int> playerHearts)
    {
        // Ensure playerHearts is valid
        if (playerHearts == null || playerHearts.Count == 0)
        {
            Debug.LogError("Player hearts data is null or empty.");
            return;
        }

        for (int i = 0; i < spawnedCells.Count; i++)
        {
            if (spawnedCells[i] == null)
            {
                Debug.LogWarning($"Cell at index {i} is null or destroyed. Skipping.");
                continue;
            }

            var cellUI = spawnedCells[i]?.GetComponent<CharacterIngameCellUI>();
            if (cellUI != null && playerHearts.ContainsKey(i))
            {
                cellUI.SetHearts(playerHearts[i]); // Update hearts for each player
            }
            else
            {
                Debug.LogWarning($"Cell UI or player data missing for index {i}. Skipping.");
            }
        }
    }
}
