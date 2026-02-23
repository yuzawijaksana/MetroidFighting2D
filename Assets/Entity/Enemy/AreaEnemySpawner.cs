using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))]
public class AreaEnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnData
    {
        public string label; // Unique ID (e.g., "Bird_Boss_1")
        public GameObject enemyPrefab;
        public Transform spawnPoint;
        public bool spawnOnlyOnce = true; 
    }

    [Header("Targeting")]
    [Tooltip("Drag the 'Players' Parent folder here")]
    public Transform playerFolder; 

    [Header("Spawn Configuration")]
    public List<EnemySpawnData> enemiesToSpawn = new List<EnemySpawnData>();
    [Tooltip("The A* Graph Index for this area. If -1, it will be calculated automatically.")]
    public int areaGraphIndex = -1;
    
    [Header("State Tracking")]
    [SerializeField] private List<string> permanentlyClearedEnemies = new List<string>();
    private Dictionary<string, GameObject> activeEnemies = new Dictionary<string, GameObject>();
    private BoxCollider2D areaCollider;

    private void Awake()
    {
        areaCollider = GetComponent<BoxCollider2D>();
        areaCollider.isTrigger = true; // Ensures the player can walk into the area

        // If the graph index isn't manually set, try to calculate it.
        if (areaGraphIndex == -1 && AstarPath.active != null)
        {
            var node = AstarPath.active.GetNearest(transform.position).node;
            if (node != null)
            {
                areaGraphIndex = (int)node.GraphIndex;
            }
            else
            {
                Debug.LogWarning($"AreaEnemySpawner at {gameObject.name} could not find a close A* node to determine its graph index. Please set it manually.", this);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Triggers when the player enters the green box
        if (other.CompareTag("Player"))
        {
            SpawnEnemies();
        }
    }

    private void SpawnEnemies()
    {
        // DEEP SEARCH: Look for the child tagged 'Player' inside the folder
        Transform actualPlayer = null;
        if (playerFolder != null)
        {
            foreach (Transform child in playerFolder)
            {
                if (child.CompareTag("Player"))
                {
                    actualPlayer = child;
                    break;
                }
            }
        }

        foreach (var data in enemiesToSpawn)
        {
            // Skip logic for one-time kills and existing enemies
            if (data.spawnOnlyOnce && permanentlyClearedEnemies.Contains(data.label)) continue;
            if (activeEnemies.ContainsKey(data.label) && activeEnemies[data.label] != null) continue;

            if (data.enemyPrefab != null && data.spawnPoint != null)
            {
                GameObject enemy = Instantiate(data.enemyPrefab, data.spawnPoint.position, Quaternion.identity);
                enemy.name = data.enemyPrefab.name + "_" + data.label; // More descriptive name
                activeEnemies[data.label] = enemy;
                
                // Link the enemy to its 'Home' and the 'Player' target
                var controller = enemy.GetComponent<EnemyAIController>();
                if (controller != null)
                {
                    controller.myHomeArea = this;
                    controller.homeGraphIndex = areaGraphIndex;
                    var destSetter = enemy.GetComponent<Pathfinding.AIDestinationSetter>();
                    if (destSetter != null && actualPlayer != null)
                    {
                        destSetter.target = actualPlayer;
                    }
                }
            }
        }
    }

    private void Update()
    {
        // Check if enemies are killed to mark them as 'Permanently Cleared'
        List<string> deadEnemyLabels = null;
        foreach (var pair in activeEnemies)
        {
            if (pair.Value == null)
            {
                if (deadEnemyLabels == null) deadEnemyLabels = new List<string>();
                deadEnemyLabels.Add(pair.Key);
            }
        }

        if (deadEnemyLabels != null)
        {
            foreach (string label in deadEnemyLabels)
            {
                if (!permanentlyClearedEnemies.Contains(label)) permanentlyClearedEnemies.Add(label);
                activeEnemies.Remove(label);
            }
        }
    }

    // This is the function the Slime calls to know when to stop chasing
    public bool IsInArea(Vector3 position)
    {
        return areaCollider.OverlapPoint(position);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            DespawnEnemies();
        }
    }

    private void DespawnEnemies()
    {
        // Loop through all currently alive enemies in this area
        foreach (var pair in activeEnemies)
        {
            if (pair.Value != null)
            {
                Destroy(pair.Value); // Remove them from the game world
            }
        }
        
        // Clear the list so the spawner knows the area is empty again
        activeEnemies.Clear();
        Debug.Log($"Player left {gameObject.name}. Enemies despawned.");
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Draw the detected graph index in the scene view for easy debugging
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2, $"Graph Index: {areaGraphIndex}");
    }
#endif
}