using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Unity.Cinemachine;
// using YourProject.UI; // Uncomment and replace with your actual namespace if CharacterIngameGridUI is in a different namespace

public class GameStarter : MonoBehaviour
{
    public static GameStarter Instance { get; private set; }

    [Header("Map Configuration")]
    public GameObject mapObject; // Assign your default PVP map
    [SerializeField] private Collider2D cameraBounds; // Assign camera bounds for the map
    [SerializeField] private Vector3 player1SpawnPosition = new Vector3(-3, 2, 0);
    [SerializeField] private Vector3 player2SpawnPosition = new Vector3(3, 2, 0);

    [Header("References (Assign in Inspector)")]
    public CharacterIngameGridUI ingameGridUI; // Assign in inspector

    public CharacterCard player1Card; // Store selected CharacterCard
    public CharacterCard player2Card; // Store selected CharacterCard

    [HideInInspector] public GameObject player1Prefab;
    [HideInInspector] public GameObject player2Prefab;
    public GameObject countdownTextObject;
    public GameObject playersParent; // Assign your Players GameObject in the Inspector
    public TargetGroupManager targetGroupManager; // Assign in Inspector
    [SerializeField] private CinemachineConfiner2D cinemachineConfiner;

    [Header("Victory UI")]
    public VictoryScreenUI victoryScreenUI; // Assign in inspector

    // Store selection history for rematch
    private List<GameObject> selectedPrefabs = new List<GameObject>();
    private List<string> selectedLabels = new List<string>();
    private List<CharacterCard> selectedCards = new List<CharacterCard>();

    private List<GameObject> spawnedPlayers = new List<GameObject>();

    // Add this field to store spawn positions for each player
    private List<Vector3> spawnPositions = new List<Vector3>();

    private void Awake()
    {
        Instance = this;
    }

    public void StartGame()
    {
        // Validate map before starting
        if (!ValidateMap())
        {
            return;
        }
        
        ResetGameState();
        InitializeMap();
        InitializeSpawnPositions();
        InitializePlayers();
        InitializeUI();
        LinkPlayersToUI(); // Link players to their UI cells
        UpdateTargetGroup(); // Call the reusable method
        StartCountdownAndEnableControls();
    }

    public void Rematch()
    {
        ResetGameState();
        InitializeMap(); // Re-enable map for rematch
        InitializePlayers();
        InitializeUI(); // Ensure UI is recreated before updating hearts
        LinkPlayersToUI(); // Link players to their UI cells
        UpdateTargetGroup(); // Call the reusable method
        StartCountdownAndEnableControls();
    }

    private void ResetGameState()
    {
        spawnedPlayers.Clear();

        // Destroy all players
        foreach (Transform child in playersParent.transform)
        {
            if (child != null)
                Destroy(child.gameObject);
        }

        // Clear all UI elements in CharacterIngameGridUI
        if (ingameGridUI != null)
        {
            foreach (Transform child in ingameGridUI.transform)
            {
                if (child != null)
                    Destroy(child.gameObject);
            }
            ingameGridUI.transform.parent.gameObject.SetActive(false); // Hide the UI parent
        }
        
        // Disable all maps
        DisableAllMaps();
    }
    
    private bool ValidateMap()
    {
        if (mapObject == null)
        {
            Debug.LogWarning("GameStarter: Map Object is not assigned. Game will not start.");
            return false;
        }
        
        return true;
    }
    
    private void DisableAllMaps()
    {
        if (mapObject != null)
        {
            mapObject.SetActive(false);
        }
    }

    private void InitializeMap()
    {
        // Enable the map
        if (mapObject != null)
        {
            mapObject.SetActive(true);
        }
        
        // Set camera bounds
        if (cinemachineConfiner != null && cameraBounds != null)
        {
            cinemachineConfiner.BoundingShape2D = cameraBounds;
            cinemachineConfiner.InvalidateBoundingShapeCache();
            Debug.Log($"GameStarter: Camera bounds set to {cameraBounds.name}");
        }
        else if (cameraBounds == null)
        {
            Debug.LogWarning("GameStarter: Camera bounds not assigned.");
        }
        else if (cinemachineConfiner == null)
        {
            Debug.LogWarning("GameStarter: Cinemachine Confiner not assigned.");
        }
    }

    private void InitializeSpawnPositions()
    {
        spawnPositions = new List<Vector3>
        {
            player1SpawnPosition,
            player2SpawnPosition
        };
    }

    private void InitializePlayers()
    {
        var playerPrefabs = new List<GameObject> { player1Prefab, player2Prefab };
        var playerHearts = new Dictionary<int, int>();

        for (int i = 0; i < playerPrefabs.Count; i++)
        {
            var playerObj = Instantiate(playerPrefabs[i], spawnPositions[i], Quaternion.identity, playersParent.transform);
            spawnedPlayers.Add(playerObj);

            var controller = playerObj.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.controlScheme = i == 0 ? ControlScheme.Keyboard1 : ControlScheme.Keyboard2;
                controller.SetControllable(false); // Disable controls during initialization
                if (i == 1)
                {
                    playerObj.transform.localScale = new Vector3(-Mathf.Abs(playerObj.transform.localScale.x), playerObj.transform.localScale.y, playerObj.transform.localScale.z);
                    controller.isFacingRight = false;
                }
            }

            var dmg = playerObj.GetComponentInChildren<Damageable>();
            if (dmg != null)
            {
                dmg.maxHearts = 3; // Reset hearts to default value
                dmg.currentHealth = 0; // Reset health
                playerHearts[i] = dmg.maxHearts; // Store hearts for grid update
            }
            else
            {
                Debug.LogError($"Damageable component not found on player {i + 1}. Ensure it is attached to the player prefab.");
            }
        }

        // Update UI via grid
        if (ingameGridUI != null)
        {
            ingameGridUI.UpdateAllHearts(playerHearts);
        }
        else
        {
            Debug.LogError("IngameGridUI is not assigned.");
        }
    }

    private void InitializeUI()
    {
        // Ensure ingameGridUI is valid
        if (ingameGridUI == null || ingameGridUI.transform.parent == null)
        {
            Debug.LogError("IngameGridUI or its parent is not assigned.");
            return;
        }

        // Ensure player cards are valid
        if (player1Card == null || player2Card == null)
        {
            Debug.LogError("Player cards are not assigned.");
            return;
        }

        // Re-enable the parent of ingameGridUI
        ingameGridUI.transform.parent.gameObject.SetActive(true);

        // Recreate the UI elements
        ingameGridUI.SetCards(new List<(CharacterCard, string)>
        {
            (player1Card, "P1"),
            (player2Card, "P2")
        });
    }

    private void LinkPlayersToUI()
    {
        // Link each player's Damageable component to its corresponding UI cell
        for (int i = 0; i < spawnedPlayers.Count; i++)
        {
            var playerObj = spawnedPlayers[i];
            var damageable = playerObj.GetComponentInChildren<Damageable>();
            
            if (damageable != null && ingameGridUI != null)
            {
                // Get the UI cell for this player (index i corresponds to player i)
                var cellUI = GetCellUIForPlayer(i);
                if (cellUI != null)
                {
                    damageable.cellUI = cellUI;
                    Debug.Log($"Linked player {i + 1} Damageable to UI cell");
                    
                    // Initialize the UI with current health
                    cellUI.UpdateMaskColor(damageable);
                }
                else
                {
                    Debug.LogError($"Could not find UI cell for player {i + 1}");
                }
            }
            else
            {
                Debug.LogError($"Damageable component not found for player {i + 1}");
            }
        }
    }

    private CharacterIngameCellUI GetCellUIForPlayer(int playerIndex)
    {
        if (ingameGridUI == null || playerIndex < 0 || playerIndex >= ingameGridUI.transform.childCount)
            return null;
            
        var cellTransform = ingameGridUI.transform.GetChild(playerIndex);
        return cellTransform?.GetComponent<CharacterIngameCellUI>();
    }

    private void UpdateTargetGroup()
    {
        // Update TargetGroupManager
        if (targetGroupManager != null)
        {
            targetGroupManager.UpdateTargets();
        }

        // Update OffscreenIndicator
        var offscreenIndicator = FindFirstObjectByType<OffscreenIndicator>();
        if (offscreenIndicator != null)
        {
            offscreenIndicator.UpdateTargets();
        }
    }

    private void StartCountdownAndEnableControls()
    {
        StartCoroutine(CountdownCoroutine(() =>
        {
            foreach (var playerObj in spawnedPlayers)
            {
                var controller = playerObj.GetComponent<PlayerController>();
                controller?.SetControllable(true);
            }
        }));
    }

    private IEnumerator CountdownCoroutine(System.Action onCountdownComplete)
    {
        int countdown = 3;
        while (countdown > 0)
        {
            Debug.Log(countdown);
            countdown--;
            yield return new WaitForSeconds(1f);
        }

        Debug.Log("Start!");
        onCountdownComplete?.Invoke();
    }

    private CharacterIngameCellUI FindCellUIForPlayer(int playerIndex)
    {
        var allCells = GameObject.FindObjectsByType<CharacterIngameCellUI>(FindObjectsSortMode.None);
        string label = playerIndex == 0 ? "P1" : "P2";
        return System.Array.Find(allCells, c => c.nameText != null && c.nameText.text.Trim() == label.Trim());
    }

    public void ClearGameState()
    {
        selectedPrefabs.Clear();
        selectedLabels.Clear();
        selectedCards.Clear();
        spawnedPlayers.Clear();
    }

    public void OnPlayerDeath(GameObject deadPlayer)
    {
        if (spawnedPlayers.Contains(deadPlayer))
            spawnedPlayers.Remove(deadPlayer);

        // Check for victory
        if (spawnedPlayers.Count == 1)
        {
            // Find the winner's label and CharacterCard
            GameObject winnerObj = spawnedPlayers[0];
            string winnerLabel = winnerObj.name;
            CharacterCard winnerCard = null;

            int idx = -1;
            for (int i = 0; i < selectedPrefabs.Count; i++)
            {
                string prefabName = selectedPrefabs[i].name.Replace("(Clone)", "").Trim();
                string winnerObjName = winnerObj.name.Replace("(Clone)", "").Trim();
                if (winnerObjName.Contains(prefabName) || prefabName.Contains(winnerObjName))
                {
                    idx = i;
                    break;
                }
            }
            if (idx >= 0 && idx < selectedCards.Count)
            {
                winnerCard = selectedCards[idx];
                winnerLabel = selectedLabels[idx];
            }

            // Show victory screen
            if (victoryScreenUI != null)
                victoryScreenUI.Show(winnerLabel, winnerCard);
        }
    }

    public void SetSelectedData(List<GameObject> prefabs, List<string> labels, List<CharacterCard> cards)
    {
        selectedPrefabs = prefabs;
        selectedLabels = labels;
        selectedCards = cards;
    }
}
