using UnityEngine;
using UnityEditor;

public class FixNegativeColliderScale : EditorWindow
{
    [MenuItem("Tools/Fix Negative Collider Scales")]
    public static void FixAll()
    {
        BoxCollider[] colliders = GameObject.FindObjectsOfType<BoxCollider>();
        foreach (BoxCollider col in colliders)
        {
            // Get lossy scale to understand the actual scaling in world space
            Vector3 worldScale = col.transform.lossyScale;
            Vector3 newSize = col.size;

            // Check if any dimension is negative
            if (worldScale.x < 0 || worldScale.y < 0 || worldScale.z < 0)
            {
                // Invert the size based on negative scale to make unity shut up please
                if (worldScale.x < 0) newSize.x = -Mathf.Abs(newSize.x);
                if (worldScale.y < 0) newSize.y = -Mathf.Abs(newSize.y);
                if (worldScale.z < 0) newSize.z = -Mathf.Abs(newSize.z);

                col.size = newSize;
            }
        }
    }
}
