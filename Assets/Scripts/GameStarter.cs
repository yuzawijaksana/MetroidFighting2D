using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
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

    [Header("Inspector Setup - assign these")]
    public CharacterIngameGridUI ingameGridUI; // In-game HUD grid
    public GameObject playersParent; // Parent object for spawned players
    public TargetGroupManager targetGroupManager; // Camera target group
    public GameObject loadingScreen; // Loading overlay canvas
    public Slider loadingSlider; // Loading progress bar
    public TMP_Text loadingText; // Loading status text
    [SerializeField] private float loadingScreenDuration = 5f; // How long the loading screen stays visible before match starts
    [SerializeField] private CinemachineCamera gameplayVirtualCamera; // Active follow camera that controls the view size
    [SerializeField] private CinemachineGroupFraming gameplayFraming; // Actual framing component used by the current virtual camera
    [SerializeField] private Camera gameplayCamera; // Fallback camera reference
    public Image player1Portrait; // P1 portrait image in HUD
    public Image player2Portrait; // P2 portrait image in HUD
    public TMP_Text player1NameText; // P1 name text in HUD
    public TMP_Text player2NameText; // P2 name text in HUD
    public TMP_Text player1LabelText; // P1 label text like "P1"
    public TMP_Text player2LabelText; // P2 label text like "P2"
    public TMP_Text tipsLabelText; // Tips text label in HUD
    public VictoryScreenUI victoryScreenUI; // Winner screen
    [SerializeField] private CinemachineConfiner2D cinemachineConfiner;

    [Header("Selection payload - sent from CharacterSelection")]
    public CharacterCard selectedPlayer1Card;
    public CharacterCard selectedPlayer2Card;

    [HideInInspector] public GameObject selectedPlayer1Prefab;
    [HideInInspector] public GameObject selectedPlayer2Prefab;

    [Header("Runtime / internal state - do not assign manually")]
    public GameObject countdownTextObject;
    private List<GameObject> selectedPrefabs = new List<GameObject>();
    private List<string> selectedLabels = new List<string>();
    private List<CharacterCard> selectedCards = new List<CharacterCard>();
    private List<GameObject> spawnedPlayers = new List<GameObject>();
    private List<Vector3> spawnPositions = new List<Vector3>();

    [Header("Camera Modes")]
    [SerializeField] private float fixedCameraSize = 4f;
    [SerializeField] private float defaultCameraSize = 6f;
    [SerializeField] private float actionCameraSize = 5f;

    private enum CameraMode
    {
        Fixed,
        Default,
        Action
    }

    private CameraMode currentCameraMode = CameraMode.Default;
    private bool forceMatchCameraView = false;

    private void Awake()
    {
        Instance = this;
        ResolveGameplayCameraReferences();
        SetGameplayCameraMode(false);
    }

    private void Start()
    {
        ResolveGameplayCameraReferences();
        SetGameplayCameraMode(false);
    }

    private void ResolveGameplayCameraReferences()
    {
        if (gameplayCamera == null)
            gameplayCamera = Camera.main;

        if (gameplayVirtualCamera == null)
        {
            var foundCamera = GameObject.Find("CinemachineCamera");
            if (foundCamera != null)
                gameplayVirtualCamera = foundCamera.GetComponent<CinemachineCamera>();
        }

        if (gameplayVirtualCamera == null && gameplayCamera != null)
            gameplayVirtualCamera = gameplayCamera.GetComponent<CinemachineCamera>();

        if (gameplayVirtualCamera == null)
            gameplayVirtualCamera = FindFirstObjectByType<CinemachineCamera>();

        if (gameplayVirtualCamera != null && gameplayFraming == null)
            gameplayFraming = gameplayVirtualCamera.GetComponent<CinemachineGroupFraming>();

        if (gameplayVirtualCamera == null)
            Debug.LogWarning("GameStarter: Could not find the active Cinemachine camera. Assign CinemachineCamera manually in GameStarter.");
    }

    private void Update()
    {
        if (forceMatchCameraView)
        {
            ForceCameraToActionView();
        }

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            CycleCameraMode();
        }
    }

    public void StartGame()
    {
        // Validate map before starting
        if (!ValidateMap())
        {
            return;
        }

        StartCoroutine(StartMatchRoutine("Preparing arena...", loadingScreenDuration));
    }

    public void Rematch()
    {
        StartCoroutine(StartMatchRoutine("Rematch...", loadingScreenDuration));
    }

    private IEnumerator StartMatchRoutine(string loadingMessage, float delayBeforeSetup)
    {
        ShowLoading(true, 0.15f, loadingMessage);
        yield return new WaitForSeconds(delayBeforeSetup);

        ResetGameState();
        InitializeMap();
        InitializeSpawnPositions();
        InitializePlayers();
        InitializeUI();
        LinkPlayersToUI(); // Link players to their UI cells
        UpdateTargetGroup(); // Call the reusable method
        UpdateSelectedPlayerVisuals();
        SetGameplayCameraMode(true);
        forceMatchCameraView = true;
        ForceCameraToActionView();
        StartCountdownAndEnableControls();

        ShowLoading(false, 1f, "Fight!");
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

        ApplyCameraMode();
    }

    private void CycleCameraMode()
    {
        currentCameraMode = (CameraMode)(((int)currentCameraMode + 1) % 3);
        ApplyCameraMode();
    }

    private void ApplyCameraMode()
    {
        ResolveGameplayCameraReferences();

        float cameraSize = currentCameraMode switch
        {
            CameraMode.Fixed => fixedCameraSize,
            CameraMode.Action => actionCameraSize,
            _ => defaultCameraSize
        };

        if (gameplayVirtualCamera != null)
        {
            var lens = gameplayVirtualCamera.Lens;
            lens.OrthographicSize = cameraSize;
            gameplayVirtualCamera.Lens = lens;
        }

        if (gameplayCamera != null)
        {
            gameplayCamera.orthographicSize = cameraSize;
        }

        if (gameplayFraming != null)
        {
            gameplayFraming.FramingSize = cameraSize;
        }

        Debug.Log($"GameStarter: Camera mode set to {currentCameraMode} ({cameraSize}) | virtual={gameplayVirtualCamera?.name} | framing={gameplayFraming?.FramingSize}");
    }

    private void ForceCameraToActionView()
    {
        ResolveGameplayCameraReferences();
        currentCameraMode = CameraMode.Action;
        SetGameplayCameraMode(true);

        if (gameplayVirtualCamera != null)
        {
            var lens = gameplayVirtualCamera.Lens;
            lens.OrthographicSize = 4f;
            gameplayVirtualCamera.Lens = lens;
        }

        if (gameplayCamera != null)
        {
            gameplayCamera.orthographicSize = 4f;
        }

        if (gameplayFraming != null)
        {
            gameplayFraming.FramingSize = 4f;
        }

        Debug.Log("GameStarter: Forced gameplay camera zoom to 4 for match start.");
    }

    private void SetGameplayCameraMode(bool isGameplayCamera)
    {
        ResolveGameplayCameraReferences();

        if (gameplayFraming != null)
        {
            gameplayFraming.enabled = !isGameplayCamera;
        }

        if (gameplayVirtualCamera != null)
        {
            var lens = gameplayVirtualCamera.Lens;
            lens.OrthographicSize = isGameplayCamera ? 4f : 10f;
            gameplayVirtualCamera.Lens = lens;
        }

        Debug.Log($"GameStarter: Gameplay camera mode = {isGameplayCamera}");
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
        var playerPrefabs = new List<GameObject> { selectedPlayer1Prefab, selectedPlayer2Prefab };
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
        if (selectedPlayer1Card == null || selectedPlayer2Card == null)
        {
            Debug.LogError("Player cards are not assigned.");
            return;
        }

        // Re-enable the parent of ingameGridUI
        ingameGridUI.transform.parent.gameObject.SetActive(true);

        // Recreate the UI elements
        ingameGridUI.SetCards(new List<(CharacterCard, string)>
        {
            (selectedPlayer1Card, "P1"),
            (selectedPlayer2Card, "P2")
        });
    }

    private void UpdateSelectedPlayerVisuals()
    {
        // Source data comes from CharacterSelection via selectedPlayerXCard.
        // Destination UI slots are these portrait/name/label fields above.
        ApplyPlayerVisual(0, selectedPlayer1Card, "P1", player1Portrait, player1NameText, player1LabelText);
        ApplyPlayerVisual(1, selectedPlayer2Card, "P2", player2Portrait, player2NameText, player2LabelText);
    }

    private void ApplyPlayerVisual(int index, CharacterCard card, string label, Image portraitImage, TMP_Text nameText, TMP_Text labelText)
    {
        if (card == null)
            return;

        // CharacterSelection sends the selected CharacterCard, and that card contains:
        // - card.characterSprite -> portrait source
        // - card.characterName -> name source
        if (portraitImage != null && card.characterSprite != null)
            portraitImage.sprite = card.characterSprite;

        if (nameText != null)
            nameText.text = card.characterName;

        if (labelText != null)
            labelText.text = label;
    }

    public void SetTip(string tipText)
    {
        if (tipsLabelText != null)
            tipsLabelText.text = tipText;
    }

    public void ApplySelectedCardVisuals()
    {
        ApplyPlayerVisual(0, selectedPlayer1Card, "P1", player1Portrait, player1NameText, player1LabelText);
        ApplyPlayerVisual(1, selectedPlayer2Card, "P2", player2Portrait, player2NameText, player2LabelText);
    }

    private void ShowLoading(bool isVisible, float progressValue = 0f, string message = "Loading...")
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(isVisible);

        if (loadingSlider != null)
            loadingSlider.value = Mathf.Clamp01(progressValue);

        if (loadingText != null)
            loadingText.text = message;
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
