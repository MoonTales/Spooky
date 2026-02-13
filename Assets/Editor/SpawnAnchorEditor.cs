using UnityEngine;
using UnityEditor;

namespace Placeables
{
    [CustomEditor(typeof(SpawnAnchor))]
    public class SpawnAnchorEditor : Editor
    {
        private static class Styles
        {
            public static GUIStyle headerStyle;
            public static GUIStyle buttonStyle;
            public static GUIStyle sectionStyle;
            
            static Styles()
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
                };
                
                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    fixedHeight = 32
                };
                
                sectionStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }
        }
        
        public override void OnInspectorGUI()
        {
            // Draw the default inspector
            DrawDefaultInspector();
            
            SpawnAnchor spawnAnchor = (SpawnAnchor)target;
            
            EditorGUILayout.Space(15);
            
            // Header section
            DrawHeader();
            
            EditorGUILayout.Space(5);
            
            // Buttons section with colored backgrounds
            EditorGUILayout.BeginVertical(Styles.sectionStyle);
            
            DrawVisualizationButtons(spawnAnchor);
            EditorGUILayout.Space(10);
            DrawActionButtons(spawnAnchor);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawHeader()
        {
            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, Styles.headerStyle, GUILayout.Height(30));
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
            GUI.Label(rect, "Spawn Anchor Editor", Styles.headerStyle);
        }
        
        private void DrawVisualizationButtons(SpawnAnchor spawnAnchor)
        {
            GUILayout.Label("Visualization", EditorStyles.miniBoldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            // Visualize button - Blue theme
            GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
            if (GUILayout.Button(new GUIContent("👁 Visualize", "Preview Spawn Location"), Styles.buttonStyle))
            {
                spawnAnchor.Editor_VisualizeSpawnPoints();
                EditorUtility.SetDirty(spawnAnchor);
            }
            
            // Clear button - Orange theme
            
            GUI.backgroundColor = new Color(1f, 0.6f, 0.3f);
            if (GUILayout.Button("✖ Clear", Styles.buttonStyle))
            {
                spawnAnchor.Editor_ClearSpawnPoints();
                EditorUtility.SetDirty(spawnAnchor);
            }
            
            GUI.backgroundColor = spawnAnchor.IsDrawingGizmos() ? Color.white : Color.gray;
            if (GUILayout.Button("Toggle Visualization", Styles.buttonStyle))
            {
                spawnAnchor.Editor_ToggleGizmos();
                EditorUtility.SetDirty(spawnAnchor);
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawActionButtons(SpawnAnchor spawnAnchor)
        {
            GUILayout.Label("Actions", EditorStyles.miniBoldLabel);
            
            // Spawn button - Green theme
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("▶ Spawn Objects", Styles.buttonStyle))
            {
                spawnAnchor.Editor_SpawnObjects();
                EditorUtility.SetDirty(spawnAnchor);
            }
            
            // Undo button - Red theme
            GUI.backgroundColor = spawnAnchor.GetNumberOfSpawnedLists() != 0 ? new Color(0.9f, 0.4f, 0.4f) : Color.black;
            if (GUILayout.Button("↶ Undo Last Spawn", Styles.buttonStyle))
            {
                spawnAnchor.Editor_UndoLastSpawn();
                EditorUtility.SetDirty(spawnAnchor);
            }
            
            // Redo button - Purple theme
            GUI.backgroundColor = spawnAnchor.GetNumberOfUndoneLists() != 0 ? new Color(0.7f, 0.4f, 0.9f) : Color.black;
            if (GUILayout.Button("↷ Redo Last Spawn", Styles.buttonStyle))
            {
                spawnAnchor.Editor_RedoLastUndo();
                EditorUtility.SetDirty(spawnAnchor);
            }
            
            GUI.backgroundColor = Color.white;
        }
    }
}