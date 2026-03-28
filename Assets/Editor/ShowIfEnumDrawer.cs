using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomPropertyDrawer(typeof(ShowIfEnumAttribute))]
public class ShowIfEnumDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (ShouldShow(property))
        {
            // Apply your Label Width fix here
            float oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(label).x + 20f;

            EditorGUI.PropertyField(position, property, label, true);

            EditorGUIUtility.labelWidth = oldWidth;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return ShouldShow(property) ? EditorGUI.GetPropertyHeight(property, label) : 0f;
    }

    private bool ShouldShow(SerializedProperty property)
    {
        ShowIfEnumAttribute attr = (ShowIfEnumAttribute)attribute;

        // Find the enum property relative to this field
        string path = property.propertyPath.Replace(property.name, attr.EnumName);
        SerializedProperty enumField = property.serializedObject.FindProperty(path);

        if (enumField == null) return true;

        // Compare the current index of the enum to the target values
        bool isMatch = false;
        foreach (var target in attr.TargetValues)
        {
            if ((int)target == enumField.enumValueIndex)
            {
                isMatch = true;
                break;
            }
        }

        return attr.Invert ? !isMatch : isMatch;
    }
}
