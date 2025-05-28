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
        // If the number of cells doesn't match, destroy all and recreate
        if (spawnedCells.Count != selectedCards.Count)
        {
            foreach (var cell in spawnedCells)
            {
                if (cell != null) Destroy(cell);
            }
            spawnedCells.Clear();
        }

        // Create new cells if needed
        while (spawnedCells.Count < selectedCards.Count)
        {
            GameObject cell = Instantiate(cellPrefab, transform);
            spawnedCells.Add(cell);
        }

        // Update each cell's data
        for (int i = 0; i < selectedCards.Count; i++)
        {
            var cellUI = spawnedCells[i].GetComponent<CharacterIngameCellUI>();
            if (cellUI != null)
            {
                cellUI.SetCharacterCard(selectedCards[i].card, selectedCards[i].label);
                // Optionally reset hearts here if needed:
                // cellUI.InitHearts(cellUI.heartImages.Count); // Or pass the correct maxHearts
            }
            spawnedCells[i].SetActive(true);
        }

        // Hide any extra cells
        for (int i = selectedCards.Count; i < spawnedCells.Count; i++)
        {
            spawnedCells[i].SetActive(false);
        }
    }
}
