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

        serializedObject.Update();

        var controlSchemeProp = serializedObject.FindProperty("controlScheme");
        controlSchemeProp.enumValueIndex = (int)(ControlScheme)EditorGUILayout.EnumPopup(
            "Control Scheme", (ControlScheme)controlSchemeProp.enumValueIndex);

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