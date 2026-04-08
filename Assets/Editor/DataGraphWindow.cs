using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using System.Collections.Generic;
using System;
using System.Linq;

public class DataGraphWindow : EditorWindow
{
    private SimpleGraphView _graphView;
    private MonoBehaviour _currentTarget;
    private float _defaultWidth = 0f;

    [MenuItem("Window/Custom/Data Dashboard Graph")]
    public static void Open() => GetWindow<DataGraphWindow>("Data Dashboard");

    private void CreateGUI()
    {
        var toolbar = new Toolbar();

        var picker = new ObjectField("Target") { objectType = typeof(MonoBehaviour), style = { width = 250 } };
        picker.RegisterValueChangedCallback(evt => {
            _currentTarget = evt.newValue as MonoBehaviour;
            RefreshNodes();
        });

        var refreshBtn = new Button(RefreshNodes) { text = "Refresh Graph" };

        toolbar.Add(picker);
        toolbar.Add(refreshBtn);
        toolbar.Add(new ToolbarSpacer());
        rootVisualElement.Add(toolbar);

        _graphView = new SimpleGraphView { style = { flexGrow = 1 } };
        _graphView.AddManipulator(new ContentDragger());
        _graphView.AddManipulator(new SelectionDragger());
        _graphView.AddManipulator(new RectangleSelector());
        _graphView.SetupZoom(0.05f, 4.0f);

        rootVisualElement.Add(_graphView);
    }

    private void RefreshNodes()
    {
        _graphView.DeleteElements(_graphView.graphElements);
        if (_currentTarget == null) return;

        var so = new SerializedObject(_currentTarget);
        var prop = so.GetIterator();
        prop.NextVisible(true);

        int index = 0;
        while (prop.NextVisible(false))
        {
            if (prop.hasChildren)
            {
                CreateDataNode(prop, ref index);
                index++;
            }
        }
    }

    private void CreateDataNode(SerializedProperty prop, ref int index)
    {
        string saveKey = $"{_currentTarget.GetType().Name}_{prop.propertyPath}_pos";

        Vector2 savedPos = new Vector2(
            EditorPrefs.GetFloat(saveKey + "X", index * (_defaultWidth + 50)),
            EditorPrefs.GetFloat(saveKey + "Y", 50)
        );

        var node = new Node { title = prop.displayName };
        node.SetPosition(new Rect(savedPos, new Vector2(_defaultWidth, 200)));

        node.style.width = StyleKeyword.Auto;
        node.style.minWidth = _defaultWidth;
        node.extensionContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        node.extensionContainer.style.paddingTop = 5;
        node.extensionContainer.style.paddingBottom = 5;
        node.extensionContainer.style.paddingLeft = 5;
        node.extensionContainer.style.paddingRight = 5;

        // Capture the SerializedObject once per node, NEVER    EVER   BIND IT
        var so = prop.serializedObject;

        VisualElement CreateCellWrapper()
        {
            var wrapper = new VisualElement();
            wrapper.style.flexShrink = 0;
            wrapper.style.paddingTop = 5; wrapper.style.paddingBottom = 5;
            wrapper.style.paddingLeft = 5; wrapper.style.paddingRight = 5;
            wrapper.style.borderTopWidth = 1; wrapper.style.borderBottomWidth = 1;
            wrapper.style.borderLeftWidth = 1; wrapper.style.borderRightWidth = 1;
            var borderColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            wrapper.style.borderTopColor = borderColor;
            wrapper.style.borderBottomColor = borderColor;
            wrapper.style.borderLeftColor = borderColor;
            wrapper.style.borderRightColor = borderColor;
            wrapper.style.backgroundColor = new Color(1, 1, 1, 0.03f);
            return wrapper;
        }

        // This is the ONLY place I ever touch the serialized object after the build,,, because the object is EVIL and wants to KILL ME    AND KILL DOUGLAS
        void Commit(Action<SerializedProperty> write, string path)
        {
            so.Update();
            var p = so.FindProperty(path);
            if (p == null) return;
            write(p);
            so.ApplyModifiedProperties();
        }

        void BuildDataUI(SerializedProperty p, VisualElement container, bool isVertical)
        {
            // Snapshot the path here; the SerializedProperty iterator is reused
            // so I gotta capture the path as a string   ig   cause that works... strings saveee the daayyy
            string path = p.propertyPath;

            // Logic for lists and arrays etc
            if (p.isArray && p.propertyType == SerializedPropertyType.Generic)
            {
                var headerContainer = new VisualElement();
                headerContainer.style.flexDirection = FlexDirection.Row;
                headerContainer.style.marginBottom = 5;
                headerContainer.style.alignItems = Align.Center;

                var foldout = new Foldout { text = p.displayName, value = true };
                foldout.style.flexGrow = 1;

                // Read size once, NO BINDING
                var sizeField = new IntegerField("Size") { isDelayed = true, value = p.arraySize };
                sizeField.style.minWidth = 120;

                var listContent = new VisualElement();
                listContent.style.flexDirection = isVertical ? FlexDirection.Column : FlexDirection.Row;
                listContent.style.flexWrap = Wrap.NoWrap;
                listContent.style.marginLeft = 15;

                foldout.RegisterValueChangedCallback(evt =>
                    listContent.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None);

                headerContainer.Add(foldout);
                headerContainer.Add(sizeField);
                container.Add(headerContainer);
                container.Add(listContent);

                void SyncContent(int targetCount)
                {
                    int currentCount = listContent.childCount;
                    if (targetCount == currentCount) return;

                    if (targetCount > currentCount)
                    {
                        so.Update();
                        var freshArray = so.FindProperty(path);
                        if (freshArray == null) return;
                        for (int i = currentCount; i < targetCount; i++)
                        {
                            var elementProp = freshArray.GetArrayElementAtIndex(i);
                            var wrapper = CreateCellWrapper();
                            BuildDataUI(elementProp, wrapper, !isVertical);
                            listContent.Add(wrapper);
                        }
                    }
                    else
                    {
                        while (listContent.childCount > targetCount)
                            listContent.RemoveAt(listContent.childCount - 1);
                    }
                }

                // Build initial contents without touching SO again
                SyncContent(p.arraySize);

                sizeField.RegisterValueChangedCallback(evt => {
                    Commit(freshProp => freshProp.arraySize = evt.newValue, path);
                    SyncContent(evt.newValue);
                });
            }
            // Class and other Structs
            else if (p.hasVisibleChildren)
            {
                var classFoldout = new Foldout { text = p.displayName, value = true };
                var it = p.Copy();
                var end = it.GetEndProperty();
                it.NextVisible(true);

                while (it != null && !SerializedProperty.EqualContents(it, end))
                {
                    BuildDataUI(it.Copy(), classFoldout, isVertical);
                    if (!it.NextVisible(false)) break;
                }
                container.Add(classFoldout);
            }
            // All the other stuff like regular variables or whetever. read these once, write on commit, and of course  nEVER bINd
            else
            {
                VisualElement field = null;

                switch (p.propertyType)
                {
                    case SerializedPropertyType.String:
                        {
                            var f = new TextField(p.displayName) { isDelayed = true, value = p.stringValue };
                            f.RegisterValueChangedCallback(evt => Commit(fp => fp.stringValue = evt.newValue, path));
                            field = f;
                            break;
                        }
                    case SerializedPropertyType.Integer:
                        {
                            var f = new IntegerField(p.displayName) { isDelayed = true, value = p.intValue };
                            f.RegisterValueChangedCallback(evt => Commit(fp => fp.intValue = evt.newValue, path));
                            field = f;
                            break;
                        }
                    case SerializedPropertyType.Float:
                        {
                            var f = new FloatField(p.displayName) { isDelayed = true, value = p.floatValue };
                            f.RegisterValueChangedCallback(evt => Commit(fp => fp.floatValue = evt.newValue, path));
                            field = f;
                            break;
                        }
                    case SerializedPropertyType.Boolean:
                        {
                            var f = new Toggle(p.displayName) { value = p.boolValue };
                            f.RegisterValueChangedCallback(evt => Commit(fp => fp.boolValue = evt.newValue, path));
                            field = f;
                            break;
                        }
                    case SerializedPropertyType.Vector2:
                        {
                            var f = new Vector2Field(p.displayName) { value = p.vector2Value };
                            f.RegisterValueChangedCallback(evt => Commit(fp => fp.vector2Value = evt.newValue, path));
                            field = f;
                            break;
                        }
                    case SerializedPropertyType.Vector3:
                        {
                            var f = new Vector3Field(p.displayName) { value = p.vector3Value };
                            f.RegisterValueChangedCallback(evt => Commit(fp => fp.vector3Value = evt.newValue, path));
                            field = f;
                            break;
                        }
                    case SerializedPropertyType.Vector4:
                        {
                            var f = new Vector4Field(p.displayName) { value = p.vector4Value };
                            f.RegisterValueChangedCallback(evt => Commit(fp => fp.vector4Value = evt.newValue, path));
                            field = f;
                            break;
                        }
                    case SerializedPropertyType.Color:
                        {
                            var f = new ColorField(p.displayName) { value = p.colorValue };
                            f.RegisterValueChangedCallback(evt => Commit(fp => fp.colorValue = evt.newValue, path));
                            field = f;
                            break;
                        }
                    case SerializedPropertyType.Enum:
                        {
                            // EnumField needs the actual System.Type, so I look it up from the targete
                            var enumType = GetEnumType(p);
                            if (enumType != null)
                            {
                                var f = new EnumField((Enum)Enum.ToObject(enumType, p.enumValueIndex));
                                f.RegisterValueChangedCallback(evt => {
                                    Commit(fp => fp.enumValueIndex = Convert.ToInt32(evt.newValue), path);
                                });
                                field = f;
                            }
                            else
                            {
                                // Failsafe: plain int if the type can't be resolved
                                var f = new IntegerField(p.displayName) { isDelayed = true, value = p.enumValueIndex };
                                f.RegisterValueChangedCallback(evt => Commit(fp => fp.enumValueIndex = evt.newValue, path));
                                field = f;
                            }
                            break;
                        }
                    case SerializedPropertyType.ObjectReference:
                        {
                            var objType = GetObjectReferenceType(p) ?? typeof(UnityEngine.Object);
                            var f = new ObjectField(p.displayName) { objectType = objType, value = p.objectReferenceValue };
                            f.RegisterValueChangedCallback(evt => Commit(fp => fp.objectReferenceValue = evt.newValue as UnityEngine.Object, path));
                            field = f;
                            break;
                        }
                    case SerializedPropertyType.AnimationCurve:
                        {
                            var f = new CurveField(p.displayName) { value = p.animationCurveValue };
                            f.RegisterValueChangedCallback(evt => Commit(fp => fp.animationCurveValue = evt.newValue, path));
                            field = f;
                            break;
                        }
                    case SerializedPropertyType.Gradient:
                        {
                            var f = new GradientField(p.displayName) { value = p.gradientValue };
                            f.RegisterValueChangedCallback(evt => Commit(fp => fp.gradientValue = evt.newValue, path));
                            field = f;
                            break;
                        }
                    case SerializedPropertyType.LayerMask:
                        {
                            var f = new LayerMaskField(p.displayName) { value = p.intValue };
                            f.RegisterValueChangedCallback(evt => Commit(fp => fp.intValue = evt.newValue, path));
                            field = f;
                            break;
                        }
                    default:
                        {
                            // Last resort label for truly unsupported types
                            field = new Label($"{p.displayName}: ({p.propertyType})");
                            break;
                        }
                }

                field.style.minWidth = 150;
                field.style.flexShrink = 0;
                container.Add(field);
            }
        }

        BuildDataUI(prop.Copy(), node.extensionContainer, true);
        node.RefreshExpandedState();
        _graphView.AddElement(node);

        node.RegisterCallback<MouseUpEvent>(evt => {
            var currentPos = node.GetPosition().position;
            EditorPrefs.SetFloat(saveKey + "X", currentPos.x);
            EditorPrefs.SetFloat(saveKey + "Y", currentPos.y);
        });
    }

    // Resolves the System.Type for an enum property by lookin at the target's fields
    private Type GetEnumType(SerializedProperty prop)
    {
        var parts = prop.propertyPath.Replace(".Array.data[", "[").Split('.');
        Type currentType = _currentTarget.GetType();
        System.Reflection.FieldInfo fi = null;

        foreach (var part in parts)
        {
            if (part.Contains("["))
            {
                var fieldName = part.Substring(0, part.IndexOf('['));
                fi = currentType?.GetField(fieldName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);
                if (fi == null) return null;
                var elementType = fi.FieldType.IsArray
                    ? fi.FieldType.GetElementType()
                    : fi.FieldType.GetGenericArguments().FirstOrDefault();
                currentType = elementType;
            }
            else
            {
                fi = currentType?.GetField(part,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);
                currentType = fi?.FieldType;
            }
        }
        return currentType?.IsEnum == true ? currentType : null;
    }

    // Resolves the UnityEngine.Object subtype for an ObjectReference field
    private Type GetObjectReferenceType(SerializedProperty prop)
    {
        var parts = prop.propertyPath.Replace(".Array.data[", "[").Split('.');
        Type currentType = _currentTarget.GetType();
        System.Reflection.FieldInfo fi = null;

        foreach (var part in parts)
        {
            if (part.Contains("["))
            {
                var fieldName = part.Substring(0, part.IndexOf('['));
                fi = currentType?.GetField(fieldName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);
                if (fi == null) return null;
                var elementType = fi.FieldType.IsArray
                    ? fi.FieldType.GetElementType()
                    : fi.FieldType.GetGenericArguments().FirstOrDefault();
                currentType = elementType;
            }
            else
            {
                fi = currentType?.GetField(part,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);
                currentType = fi?.FieldType;
            }
        }
        return currentType;
    }
}

public class SimpleGraphView : GraphView
{
    public void UpdateZoom(float scale) => UpdateViewTransform(viewTransform.position, new Vector3(scale, scale, 1));
}