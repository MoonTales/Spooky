using UnityEngine;
using UnityEditor;

namespace Placeables
{
    [CustomEditor(typeof(SpawnAnchor))]
    public class SpawnAnchorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector
            DrawDefaultInspector();
            
            SpawnAnchor spawnAnchor = (SpawnAnchor)target;
            
            // Add some space
            EditorGUILayout.Space(10);
            
            // Add buttons
            if (GUILayout.Button("Generate Spawn Points", GUILayout.Height(30)))
            {
                spawnAnchor.GenerateSpawnPointsFromEditor();
                EditorUtility.SetDirty(spawnAnchor); // Mark as dirty so Unity saves the changes
            }
            
            if (GUILayout.Button("Clear Spawn Points", GUILayout.Height(30)))
            {
                spawnAnchor.ClearSpawnPoints();
                EditorUtility.SetDirty(spawnAnchor);
            }
        }
    }
}