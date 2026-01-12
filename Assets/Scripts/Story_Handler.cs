using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

[System.Serializable]
public class StoryData
{
    public string storyName;
    public GameObject characterPrefab;
    public Transform startPoint;
    public GameObject mapGameObject;
    public Collider2D cameraBounds;
}

public class Story_Handler : MonoBehaviour
{
    [Header("Story Configurations")]
    [SerializeField] private StoryData[] stories;
    
    [Header("Optional Settings")]
    [SerializeField] private bool spawnPlayerOnStart = false;
    [SerializeField] private CinemachineTargetGroup cinemachineTargetGroup;
    [SerializeField] private CinemachineConfiner2D cinemachineConfiner;
    [SerializeField] private int defaultStoryIndex = 0;
    [SerializeField] private Transform playersParent;
    [SerializeField] private GameObject mainMenuCanvas;
    
    private GameObject playerInstance;
    private int currentStoryIndex = -1;
    
    void Start()
    {
        // Disable all maps on start
        DisableAllMaps();
        
        // Removed auto-start to wait for user input
        if (spawnPlayerOnStart && stories.Length > 0)
        {
            StartStory(defaultStoryIndex);
        }
    }
    
    void Update()
    {
        // Check for Escape key to return to main menu
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            ReturnToMainMenu();
        }
    }
    
    // Call this method from the main menu to start a specific story by index
    public void StartStory(int storyIndex)
    {
        if (storyIndex < 0 || storyIndex >= stories.Length)
        {
            Debug.LogError($"Story_Handler: Invalid story index {storyIndex}!");
            return;
        }
        
        // Disable all maps first
        DisableAllMaps();
        
        currentStoryIndex = storyIndex;
        StoryData currentStory = stories[storyIndex];
        
        // Check if map is assigned before starting
        if (currentStory.mapGameObject == null)
        {
            Debug.LogWarning($"Story_Handler: Map GameObject is not assigned for story '{currentStory.storyName}'. Story will not start.");
            return;
        }
        
        // Enable the map for this story
        if (currentStory.mapGameObject != null)
        {
            currentStory.mapGameObject.SetActive(true);
        }
        
        // Set camera bounds for this story
        if (cinemachineConfiner != null && currentStory.cameraBounds != null)
        {
            cinemachineConfiner.BoundingShape2D = currentStory.cameraBounds;
            cinemachineConfiner.InvalidateBoundingShapeCache();
        }
        else if (currentStory.cameraBounds == null)
        {
            Debug.LogWarning($"Story_Handler: Camera bounds not assigned for story '{currentStory.storyName}'.");
        }
        
        // Spawn the character for this story
        SpawnPlayer(currentStory);
    }
    
    // Call this method from the main menu with story name (e.g., "Bird", "Squirrel")
    public void StartStoryByName(string storyName)
    {
        for (int i = 0; i < stories.Length; i++)
        {
            if (stories[i].storyName == storyName)
            {
                StartStory(i);
                return;
            }
        }
        
        Debug.LogError($"Story_Handler: Story '{storyName}' not found!");
    }
    
    private void SpawnPlayer(StoryData storyData)
    {
        if (storyData.characterPrefab != null)
        {
            // Destroy existing player instance if any
            if (playerInstance != null)
            {
                Destroy(playerInstance);
            }
            
            // Determine spawn position from startPoint
            if (storyData.startPoint == null)
            {
                Debug.LogWarning($"Story_Handler: No spawn point assigned for story '{storyData.storyName}'!");
                return;
            }
            
            // Instantiate the character at the start point
            playerInstance = Instantiate(storyData.characterPrefab, storyData.startPoint.position, storyData.startPoint.rotation);
            
            // Set the player as a child of the Players GameObject
            if (playersParent != null)
            {
                playerInstance.transform.SetParent(playersParent);
            }
            
            // Add player to Cinemachine Target Group
            if (cinemachineTargetGroup != null)
            {
                cinemachineTargetGroup.AddMember(playerInstance.transform, 1f, 1f);
            }
            
            // Set player to keyboard 1 control scheme
            PlayerController playerController = playerInstance.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.controlScheme = ControlScheme.Keyboard1;
            }
        }
        else
        {
            Debug.LogWarning($"Story_Handler: Character prefab is not assigned for story '{storyData.storyName}'!");
        }
    }
    
    private void DisableAllMaps()
    {
        foreach (StoryData story in stories)
        {
            if (story.mapGameObject != null)
            {
                story.mapGameObject.SetActive(false);
            }
        }
    }
    
    // Optional: Reset the current story
    public void ResetStory()
    {
        if (currentStoryIndex >= 0 && currentStoryIndex < stories.Length)
        {
            if (playerInstance != null)
            {
                Destroy(playerInstance);
            }
            
            StoryData currentStory = stories[currentStoryIndex];
            
            if (currentStory.mapGameObject != null)
            {
                currentStory.mapGameObject.SetActive(true);
            }
            
            SpawnPlayer(currentStory);
        }
    }
    
    // Return to main menu when Escape is pressed
    public void ReturnToMainMenu()
    {
        // Remove player from Cinemachine Target Group
        if (cinemachineTargetGroup != null && playerInstance != null)
        {
            cinemachineTargetGroup.RemoveMember(playerInstance.transform);
        }
        
        // Destroy the player instance
        if (playerInstance != null)
        {
            Destroy(playerInstance);
        }
        
        // Disable all maps
        DisableAllMaps();
        
        // Reset current story index
        currentStoryIndex = -1;
        
        // Show main menu GameObject
        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Story_Handler: Main Menu Canvas is not assigned!");
        }
    }
}

