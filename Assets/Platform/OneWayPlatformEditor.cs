using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OneWayPlatform))]
public class OneWayPlatformEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        OneWayPlatform script = (OneWayPlatform)target;

        if (GUILayout.Button("OneWayPlatform Tutorial"))
        {
            Application.OpenURL("https://youtu.be/7rCUt6mqqE8?si=ynfPsqW85V98CRtG");
        }
    }
}