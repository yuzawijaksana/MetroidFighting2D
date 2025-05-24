using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
// using YourProject.UI; // Uncomment and replace with your actual namespace if CharacterIngameGridUI is in a different namespace

public class GameStarter : MonoBehaviour
{
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
        var spawnPositions = new List<Vector3>();
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

        var spawnedPlayers = new List<GameObject>();
        for (int i = 0; i < playerPrefabs.Count; i++)
        {
            var playerObj = Instantiate(playerPrefabs[i], spawnPositions[i], Quaternion.identity, playersParent != null ? playersParent.transform : null);
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
}
