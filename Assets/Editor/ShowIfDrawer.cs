using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ShowIfAttribute))]
public class ShowIfDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ShowIfAttribute attr = (ShowIfAttribute)attribute;

        // Find the bool within the same class instance
        SerializedProperty conditionField = property.serializedObject.FindProperty(
            property.propertyPath.Replace(property.name, attr.ConditionName));

        if (conditionField != null && conditionField.boolValue)
        {
            EditorGUI.BeginProperty(position, label, property);

            // 1. Calculate how much space the label actually needs
            float labelWidth = GUI.skin.label.CalcSize(label).x + 5f; // +5 for padding

            // 2. Set the global label width for this specific draw call
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = labelWidth;

            // 3. Draw the property. Because we set labelWidth, 
            // the input field will automatically start at 'labelWidth'
            EditorGUI.PropertyField(position, property, label, true);

            // 4. Restore the original width for other properties
            EditorGUIUtility.labelWidth = originalLabelWidth;

            EditorGUI.EndProperty();
        }
    }



    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ShowIfAttribute attr = (ShowIfAttribute)attribute;
        SerializedProperty conditionField = property.serializedObject.FindProperty(
            property.propertyPath.Replace(property.name, attr.ConditionName));

        // If bool is false, height is 0 so it disappears completely
        return (conditionField != null && conditionField.boolValue)
            ? EditorGUI.GetPropertyHeight(property, label)
            : 0f;
    }
}
