using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ShowIfAttribute))]
public class ShowIfDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ShowIfAttribute attr = (ShowIfAttribute)attribute;
        SerializedProperty conditionField = property.serializedObject.FindProperty(property.propertyPath.Replace(property.name, attr.ConditionName));

        if (conditionField != null && conditionField.boolValue)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ShowIfAttribute attr = (ShowIfAttribute)attribute;
        SerializedProperty conditionField = property.serializedObject.FindProperty(property.propertyPath.Replace(property.name, attr.ConditionName));

        return (conditionField != null && conditionField.boolValue) ? EditorGUI.GetPropertyHeight(property) : 0f;
    }
}
