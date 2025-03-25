using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem; // Use the new Input System
using System.Collections.Generic;

[CustomEditor(typeof(PlayerController))]
public class PlayerControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PlayerController script = (PlayerController)target;

        // Display Control Settings at the top
        EditorGUILayout.LabelField("Control Settings", EditorStyles.boldLabel);

        // Detect connected keyboards using Unity's Input System
        List<string> controlOptions = new List<string> { "None" };
        int keyboardIndex = 1; // Start indexing keyboards from 1
        foreach (var device in InputSystem.devices)
        {
            if (device is Keyboard)
            {
                controlOptions.Add($"Keyboard{keyboardIndex}");
                keyboardIndex++;
            }
        }

        int selectedIndex = controlOptions.IndexOf(script.controlScheme);
        if (selectedIndex == -1) selectedIndex = 0; // Default to "None" if invalid

        selectedIndex = EditorGUILayout.Popup("Control Scheme", selectedIndex, controlOptions.ToArray());
        script.controlScheme = controlOptions[selectedIndex];

        script.isControllable = EditorGUILayout.Toggle("Is Controllable", script.isControllable);

        // Manually draw the rest of the inspector, excluding the already drawn fields
        EditorGUILayout.Space();
        SerializedProperty iterator = serializedObject.GetIterator();
        iterator.NextVisible(true); // Skip the script reference
        while (iterator.NextVisible(false))
        {
            if (iterator.name != "controlScheme" && iterator.name != "isControllable")
            {
                EditorGUILayout.PropertyField(iterator, true);
            }
        }

        serializedObject.ApplyModifiedProperties();

        if (GUILayout.Button("Movement & Jumping Tutorial"))
        {
            Application.OpenURL("https://www.youtube.com/watch?v=dYcf9_TdEW4&list=PLgXA5L5ma2BvEqzzeLnb7Q_4z8bz_cKmO");
        }

        if (GUILayout.Button("Wall Sliding & Jumping Tutorial"))
        {
            Application.OpenURL("https://www.youtube.com/watch?v=O6VX6Ro7EtA");
        }
    }
}