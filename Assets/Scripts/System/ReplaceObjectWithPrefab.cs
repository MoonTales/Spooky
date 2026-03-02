#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace System
{
    public class ReplaceMeshWithPrefab : EditorWindow
    {
        GameObject _prefab;
        private string _targetMeshName = "THHEDOORRR";

        [MenuItem("Tools/Replace Mesh With Prefab")]
        static void Open() => GetWindow<ReplaceMeshWithPrefab>("Replace With Prefab");

        void OnGUI()
        {
            _prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", _prefab, typeof(GameObject), false);
            _targetMeshName = EditorGUILayout.TextField("Object Name Contains", _targetMeshName);

            if (GUILayout.Button("Replace in Open Scenes"))
            {
                Replace();
            }
            
        }

        void Replace()
        {
            if (_prefab == null) { DebugUtils.LogError("Assign a prefab first!"); return; }

            GameObject[] all = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
    
            // Collect matches before any destruction
            List<GameObject> toReplace = new List<GameObject>();
            foreach (GameObject go in all)
            {
                if (go == null){ continue;}
                if (!go.name.Contains(_targetMeshName)){ continue;}
                if (PrefabUtility.GetCorrespondingObjectFromSource(go) == _prefab){ continue;}
                toReplace.Add(go);
            }

            int count = 0;
            foreach (GameObject obj in toReplace)
            {
                if (obj == null){ continue;
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(_prefab, obj.transform.parent);
                instance.transform.SetPositionAndRotation(obj.transform.position, obj.transform.rotation);
                instance.transform.localScale = obj.transform.localScale;
                instance.name = obj.name;

                Undo.RegisterCreatedObjectUndo(instance, "Replace with Prefab");
                Undo.DestroyObjectImmediate(obj);
                count++;
            }

            DebugUtils.LogSuccess($"Replaced {count} objects with prefab '{_prefab.name}'");
        }
    }
}
#endif