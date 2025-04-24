using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[CustomEditor(typeof(PlayerController))]
public class PlayerControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PlayerController script = (PlayerController)target;

        EditorGUILayout.LabelField("Control Settings", EditorStyles.boldLabel);

        List<string> controlOptions = new List<string> { "None" };
        foreach (var device in InputSystem.devices)
        {
            if (device is Keyboard)
            {
                controlOptions.Add(device.name); // Use the unique device name
            }
        }

        int selectedIndex = controlOptions.IndexOf(script.controlScheme);
        if (selectedIndex == -1) selectedIndex = 0;

        selectedIndex = EditorGUILayout.Popup("Control Scheme", selectedIndex, controlOptions.ToArray());
        script.controlScheme = controlOptions[selectedIndex];

        script.isControllable = EditorGUILayout.Toggle("Is Controllable", script.isControllable);

        EditorGUILayout.Space();
        SerializedProperty iterator = serializedObject.GetIterator();
        iterator.NextVisible(true);
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