using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A serializable wrapper around a Unity scene reference.
/// Allows referencing a scene in the Inspector while storing its name for use with scene-loading APIs.
/// </summary>
/// <remarks>
/// - Stores both the scene asset reference (Editor-only) and the scene name (runtime).
/// - Provides an implicit conversion to <see cref="string"/> for convenient use with <c>SceneManager.LoadScene</c>.
/// - Scene asset references are only valid in the Unity Editor, not at runtime.
/// </remarks>
[System.Serializable]
public class SceneField
{
    [SerializeField]
    private Object _sceneAsset;   // The scene asset reference (only valid inside the Unity Editor).
    
    [SerializeField]
    private string _sceneName;    // The serialized scene name (usable at runtime).

    /// <summary>
    /// Gets the serialized scene name, used for scene loading at runtime.
    /// </summary>
    public string SceneName
    {
        get
        {
#if UNITY_EDITOR
            if (_sceneAsset != null)
            {
                // Always resolve from asset in the Editor
                return (_sceneAsset as SceneAsset)?.name ?? _sceneName;
            }
#endif
            // In build, fallback to serialized name
            if (string.IsNullOrEmpty(_sceneName))
            {
                Debug.LogError("SceneField: scene name is empty! Did you assign it in the Inspector?");
            }
            return _sceneName;
        }
    }

    
    /// <summary>
    /// Implicitly converts a <see cref="SceneField"/> to its scene name string.
    /// This enables direct use in Unity's <c>SceneManager.LoadScene</c> and related methods.
    /// </summary>
    /// <param name="sceneField">The <see cref="SceneField"/> instance.</param>
    public static implicit operator string(SceneField sceneField)
    {
        return sceneField != null ? sceneField.SceneName : "";
    }
}

#if UNITY_EDITOR
/// <summary>
/// Custom property drawer for <see cref="SceneField"/>.
/// Provides an ObjectField in the Inspector that accepts scene assets
/// and automatically updates the stored scene name.
/// </summary>
[CustomPropertyDrawer(typeof(SceneField))]
public class SceneFieldPropertyDrawer : PropertyDrawer
{
    /// <summary>
    /// Draws the custom inspector UI for <see cref="SceneField"/>.
    /// </summary>
    /// <param name="position">Rectangle on the screen to use for the property GUI.</param>
    /// <param name="property">Serialized property representing the <see cref="SceneField"/>.</param>
    /// <param name="label">Display label for the field.</param>
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Begin handling property drawing
        EditorGUI.BeginProperty(position, GUIContent.none, property);
        
        // Get serialized fields (_sceneAsset and _sceneName) from the object
        SerializedProperty sceneAsset = property.FindPropertyRelative("_sceneAsset");
        SerializedProperty sceneName = property.FindPropertyRelative("_sceneName");
        
        // Draw label prefix
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        
        // Draw the ObjectField for scene assets
        if (sceneAsset != null)
        {
            sceneAsset.objectReferenceValue = EditorGUI.ObjectField(
                position,
                sceneAsset.objectReferenceValue,
                typeof(Object),
                false
            );

            // If a valid SceneAsset is assigned, update the stored scene name
            if (sceneAsset.objectReferenceValue != null)
            {
                sceneName.stringValue = (sceneAsset.objectReferenceValue as SceneAsset)?.name ?? "";
            }
            else
            {
                sceneName.stringValue = ""; // Clear scene name if no asset is assigned
            }
        }
        
        // End property drawing
        EditorGUI.EndProperty();
    }
}
#endif
