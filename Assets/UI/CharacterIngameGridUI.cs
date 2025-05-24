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
        // Clear previous
        foreach (var cell in spawnedCells)
        {
            if (cell != null) Destroy(cell);
        }
        spawnedCells.Clear();

        // Spawn new cells for each selected card with label
        foreach (var entry in selectedCards)
        {
            GameObject cell = Instantiate(cellPrefab, transform);
            spawnedCells.Add(cell);

            var cellUI = cell.GetComponent<CharacterIngameCellUI>();
            if (cellUI != null)
            {
                cellUI.SetCharacterCard(entry.card, entry.label);
            }
        }
    }
}
