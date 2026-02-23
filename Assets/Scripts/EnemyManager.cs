using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// A global singleton manager to keep track of all active enemies in the scene.
/// This is used by systems like the camera to know which targets to frame.
/// </summary>
public class EnemyManager : MonoBehaviour
{
    // Singleton instance
    public static EnemyManager Instance { get; private set; }

    // List of all currently active enemies in the scene
    private readonly List<Transform> activeEnemies = new List<Transform>();
    public IReadOnlyList<Transform> ActiveEnemies => activeEnemies;

    public event Action<Transform> OnEnemyRegistered;
    public event Action<Transform> OnEnemyUnregistered;

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void RegisterEnemy(Transform enemy)
    {
        if (!activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
            OnEnemyRegistered?.Invoke(enemy);
        }
    }

    public void UnregisterEnemy(Transform enemy)
    {
        if (activeEnemies.Remove(enemy))
        {
            OnEnemyUnregistered?.Invoke(enemy);
        }
    }
}