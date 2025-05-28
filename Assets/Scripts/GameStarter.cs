using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
// using YourProject.UI; // Uncomment and replace with your actual namespace if CharacterIngameGridUI is in a different namespace

public class GameStarter : MonoBehaviour
{
    public static GameStarter Instance { get; private set; }

    [Header("References (Assign in Inspector)")]
    public CharacterIngameGridUI ingameGridUI; // Assign in inspector

    public CharacterCard player1Card; // Store selected CharacterCard
    public CharacterCard player2Card; // Store selected CharacterCard

    [HideInInspector] public GameObject player1Prefab;
    [HideInInspector] public GameObject player2Prefab;
    public GameObject mapObject;
    public GameObject countdownTextObject;
    public GameObject playersParent; // Assign your Players GameObject in the Inspector
    public TargetGroupManager targetGroupManager; // Assign in Inspector

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
        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        // Enable map
        if (mapObject != null) mapObject.SetActive(true);

        // Store player prefabs, spawn positions, and labels in lists for scalability
        var playerPrefabs = new List<GameObject>();
        spawnPositions = new List<Vector3>(); // <-- ensure this is the class field
        var playerLabels = new List<string>();

        if (player1Prefab != null)
        {
            playerPrefabs.Add(player1Prefab);
            spawnPositions.Add(new Vector3(-3, 2, 0));
            playerLabels.Add("P1");
        }
        if (player2Prefab != null)
        {
            playerPrefabs.Add(player2Prefab);
            spawnPositions.Add(new Vector3(3, 2, 0));
            playerLabels.Add("P2");
        }
        // Add more players here if needed

        selectedPrefabs.Clear();
        selectedLabels.Clear();
        selectedCards.Clear();

        if (player1Prefab != null)
        {
            selectedPrefabs.Add(player1Prefab);
            selectedLabels.Add("P1");
            selectedCards.Add(player1Card);
        }
        if (player2Prefab != null)
        {
            selectedPrefabs.Add(player2Prefab);
            selectedLabels.Add("P2");
            selectedCards.Add(player2Card);
        }

        for (int i = 0; i < selectedPrefabs.Count; i++)
        {
            var playerObj = Instantiate(selectedPrefabs[i], spawnPositions[i], Quaternion.identity, playersParent != null ? playersParent.transform : null);
            spawnedPlayers.Add(playerObj);

            var controller = playerObj.GetComponent<PlayerController>();
            if (controller != null)
            {
                if (i == 0) controller.controlScheme = ControlScheme.Keyboard1;
                else if (i == 1) controller.controlScheme = ControlScheme.Keyboard2;
                // Add more control schemes as needed
            }
            if (i == 1 && playerObj != null)
            {
                // Flip player 2 to face left
                playerObj.transform.localScale = new Vector3(-Mathf.Abs(playerObj.transform.localScale.x), playerObj.transform.localScale.y, playerObj.transform.localScale.z);
                if (controller != null) controller.isFacingRight = false;
            }
        }

        // Wait one frame to ensure all prefabs are parented and active
        yield return null;

        // Update Cinemachine target group after spawning players
        if (targetGroupManager != null)
            targetGroupManager.UpdateTargetGroup();


        // Push selected cards to ingame UI
        if (ingameGridUI != null)
        {
            var selectedCards = new List<(CharacterCard, string)>();
            for (int i = 0; i < playerLabels.Count; i++)
            {
                // You must have a corresponding list of CharacterCards for each player
                CharacterCard card = null;
                if (i == 0) card = player1Card;
                else if (i == 1) card = player2Card;
                // Add more cases if you have more player cards (e.g., player3Card, etc.)

                if (card != null)
                    selectedCards.Add((card, playerLabels[i]));
            }
            ingameGridUI.SetCards(selectedCards);
        }

        // Enable the parent of Ingame UI at the start of the match
        if (ingameGridUI != null && ingameGridUI.transform.parent != null)
            ingameGridUI.transform.parent.gameObject.SetActive(true);

        // Find all cell UIs (should be as many as players)
        var allCells = GameObject.FindObjectsByType<CharacterIngameCellUI>(FindObjectsSortMode.None);

        // Assign cellUI to each player's Damageable using a loop
        for (int i = 0; i < spawnedPlayers.Count; i++)
        {
            string label = playerLabels[i];
            var cell = System.Array.Find(allCells, c => c.nameText != null && c.nameText.text == label);
            var dmg = spawnedPlayers[i].GetComponentInChildren<Damageable>();
            var controller = spawnedPlayers[i].GetComponent<PlayerController>();
            if (dmg != null && cell != null)
            {
                dmg.cellUI = cell;
                cell.UpdateMaskColor(dmg);
                cell.InitHearts(dmg.maxHearts);
                cell.SetHearts(dmg.maxHearts);
            }
            // Disable control before countdown
            if (controller != null)
                controller.SetControllable(false);
        }

        // Countdown in Debug.Log
        for (int i = 3; i > 0; i--)
        {
            Debug.Log(i);
            yield return new WaitForSeconds(1f);
        }
        Debug.Log("Start!");

        // Enable control after countdown
        foreach (var playerObj in spawnedPlayers)
        {
            var controller = playerObj.GetComponent<PlayerController>();
            if (controller != null)
                controller.SetControllable(true);
        }
    }

    // Call this when a player dies
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

            // Try to match by index in selectedPrefabs/selectedLabels
            int idx = -1;
            for (int i = 0; i < selectedPrefabs.Count; i++)
            {
                // Compare prefab name (without (Clone)) to winnerObj name (without (Clone))
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

            if (victoryScreenUI != null)
                victoryScreenUI.Show(winnerLabel, winnerCard);
        }
    }

    public void Rematch()
    {
        // Enable the parent of Ingame UI
        if (ingameGridUI != null && ingameGridUI.transform.parent != null)
            ingameGridUI.transform.parent.gameObject.SetActive(true);

        // Re-enable and reset all players
        spawnedPlayers.Clear();
        int playerIndex = 0;
        foreach (Transform child in playersParent.transform)
        {
            child.gameObject.SetActive(true);
            spawnedPlayers.Add(child.gameObject);

            // Reset player state
            var dmg = child.GetComponentInChildren<Damageable>();
            if (dmg != null)
            {
                dmg.maxHearts = 3; // Or your default value
                dmg.currentHealth = 0;
                if (dmg.cellUI != null)
                {
                    dmg.cellUI.InitHearts(dmg.maxHearts);
                    dmg.cellUI.SetHearts(dmg.maxHearts);
                    dmg.cellUI.UpdateMaskColor(dmg);
                }
            }
            var controller = child.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.SetControllable(false);
            }
            // Reset player position to spawn point
            if (playerIndex < spawnPositions.Count)
                child.position = spawnPositions[playerIndex];
            playerIndex++;
        }

        // Update Cinemachine, UI, etc. as in StartGame
        if (targetGroupManager != null)
            targetGroupManager.UpdateTargetGroup();

        // Push selected cards to ingame UI
        if (ingameGridUI != null)
        {
            var selectedCards = new List<(CharacterCard, string)>();
            for (int i = 0; i < selectedLabels.Count; i++)
            {
                CharacterCard card = null;
                if (i == 0) card = player1Card;
                else if (i == 1) card = player2Card;
                if (card != null)
                    selectedCards.Add((card, selectedLabels[i]));
            }
            ingameGridUI.SetCards(selectedCards);
        }

        // Find all cell UIs (should be as many as players)
        var allCells = GameObject.FindObjectsByType<CharacterIngameCellUI>(FindObjectsSortMode.None);

        // Assign cellUI to each player's Damageable using a loop
        for (int i = 0; i < spawnedPlayers.Count; i++)
        {
            string label = selectedLabels[i];
            var cell = System.Array.Find(allCells, c => c.nameText != null && c.nameText.text.Trim() == label.Trim());
            var dmg = spawnedPlayers[i].GetComponentInChildren<Damageable>();
            var controller = spawnedPlayers[i].GetComponent<PlayerController>();
            if (dmg != null && cell != null)
            {
                dmg.cellUI = cell;
                // Always update UI after assignment
                cell.InitHearts(dmg.maxHearts);
                cell.SetHearts(dmg.maxHearts);
                cell.UpdateMaskColor(dmg);
            }
            // Disable control before countdown
            if (controller != null)
                controller.SetControllable(false);
        }

        // Start countdown and enable control as in StartGame
        StartCoroutine(RematchCountdown());
    }

    private IEnumerator RematchCountdown()
    {
        for (int i = 3; i > 0; i--)
        {
            Debug.Log(i);
            yield return new WaitForSeconds(1f);
        }
        Debug.Log("Start!");
        foreach (var playerObj in spawnedPlayers)
        {
            var controller = playerObj.GetComponent<PlayerController>();
            if (controller != null)
                controller.SetControllable(true);
        }
    }
}
