using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages input rebinding and saves bindings to PlayerPrefs
/// </summary>
public class InputRebindingManager : MonoBehaviour
{
    public static InputRebindingManager Instance { get; private set; }

    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActionAsset;
    
    [HideInInspector]
    public GameInputs gameInputs;

    private Dictionary<string, string> rebindOverrides = new Dictionary<string, string>();
    private const string REBIND_SAVE_KEY = "InputRebinds";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeInputs();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeInputs()
    {
        // Always create a new instance of GameInputs
        gameInputs = new GameInputs();

        LoadBindings();
        gameInputs.Enable();
    }

    /// <summary>
    /// Start rebinding for a specific action
    /// </summary>
    public void StartRebinding(string actionName, int bindingIndex, Action<bool> onComplete)
    {
        var action = GetInputAction(actionName);
        if (action == null)
        {
            Debug.LogError($"Action {actionName} not found!");
            onComplete?.Invoke(false);
            return;
        }

        // Disable the action before rebinding
        action.Disable();

        var rebindOperation = action.PerformInteractiveRebinding(bindingIndex)
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation =>
            {
                // Save the override
                SaveBinding(actionName, bindingIndex);
                
                // Re-enable the action
                action.Enable();
                operation.Dispose();
                
                onComplete?.Invoke(true);
            })
            .OnCancel(operation =>
            {
                action.Enable();
                operation.Dispose();
                onComplete?.Invoke(false);
            });

        rebindOperation.Start();
    }

    /// <summary>
    /// Reset a specific binding to default
    /// </summary>
    public void ResetBinding(string actionName, int bindingIndex)
    {
        var action = GetInputAction(actionName);
        if (action == null) return;

        action.RemoveBindingOverride(bindingIndex);
        
        string key = $"{actionName}_{bindingIndex}";
        if (rebindOverrides.ContainsKey(key))
        {
            rebindOverrides.Remove(key);
        }

        SaveAllBindings();
    }

    /// <summary>
    /// Reset all bindings to default
    /// </summary>
    public void ResetAllBindings()
    {
        gameInputs.RemoveAllBindingOverrides();
        rebindOverrides.Clear();
        PlayerPrefs.DeleteKey(REBIND_SAVE_KEY);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Get the current binding display string for an action
    /// </summary>
    public string GetBindingDisplayString(string actionName, int bindingIndex)
    {
        var action = GetInputAction(actionName);
        if (action == null) return "";

        return action.GetBindingDisplayString(bindingIndex);
    }

    /// <summary>
    /// Save a specific binding override
    /// </summary>
    private void SaveBinding(string actionName, int bindingIndex)
    {
        var action = GetInputAction(actionName);
        if (action == null) return;

        string overridePath = action.bindings[bindingIndex].overridePath;
        string key = $"{actionName}_{bindingIndex}";
        
        rebindOverrides[key] = overridePath;
        SaveAllBindings();
    }

    /// <summary>
    /// Save a specific binding override with a custom path (used for swapping bindings)
    /// </summary>
    public void SaveBindingOverride(string actionName, int bindingIndex, string overridePath)
    {
        string key = $"{actionName}_{bindingIndex}";
        rebindOverrides[key] = overridePath;
        SaveAllBindings();
    }

    /// <summary>
    /// Save all binding overrides to PlayerPrefs
    /// </summary>
    private void SaveAllBindings()
    {
        string json = JsonUtility.ToJson(new SerializableBindings(rebindOverrides));
        PlayerPrefs.SetString(REBIND_SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Load all saved bindings from PlayerPrefs
    /// </summary>
    private void LoadBindings()
    {
        if (!PlayerPrefs.HasKey(REBIND_SAVE_KEY))
            return;

        string json = PlayerPrefs.GetString(REBIND_SAVE_KEY);
        var loadedBindings = JsonUtility.FromJson<SerializableBindings>(json);

        if (loadedBindings == null || loadedBindings.keys == null)
            return;

        rebindOverrides = loadedBindings.ToDictionary();

        // Apply all loaded overrides
        foreach (var kvp in rebindOverrides)
        {
            string[] parts = kvp.Key.Split('_');
            if (parts.Length != 2) continue;

            string actionName = parts[0];
            if (!int.TryParse(parts[1], out int bindingIndex)) continue;

            var action = GetInputAction(actionName);
            if (action == null) continue;

            action.ApplyBindingOverride(bindingIndex, kvp.Value);
        }
    }

    /// <summary>
    /// Get an InputAction by name
    /// </summary>
    private InputAction GetInputAction(string actionName)
    {
        // Check Player action map
        var playerAction = gameInputs.Player.Get().FindAction(actionName);
        if (playerAction != null) return playerAction;

        Debug.LogWarning($"Action {actionName} not found in any action map");
        return null;
    }

    void OnDestroy()
    {
        if (gameInputs != null)
        {
            gameInputs.Disable();
            gameInputs.Dispose();
        }
    }

    // Serializable wrapper for Dictionary
    [Serializable]
    private class SerializableBindings
    {
        public List<string> keys = new List<string>();
        public List<string> values = new List<string>();

        public SerializableBindings(Dictionary<string, string> dictionary)
        {
            foreach (var kvp in dictionary)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }

        public Dictionary<string, string> ToDictionary()
        {
            var dict = new Dictionary<string, string>();
            for (int i = 0; i < keys.Count && i < values.Count; i++)
            {
                dict[keys[i]] = values[i];
            }
            return dict;
        }
    }
}
