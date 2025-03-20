using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerController))]
public class PlayerControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PlayerController script = (PlayerController)target;

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