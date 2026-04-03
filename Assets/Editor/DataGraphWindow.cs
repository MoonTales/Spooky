using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using System.Collections.Generic;
using System;

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
        picker.RegisterValueChangedCallback(evt => { _currentTarget = evt.newValue as MonoBehaviour; RefreshNodes(); });

        var refreshBtn = new Button(RefreshNodes) { text = "Refresh Graph" };



        toolbar.Add(picker);
        toolbar.Add(refreshBtn);
        toolbar.Add(new ToolbarSpacer());
        rootVisualElement.Add(toolbar);

        _graphView = new SimpleGraphView { style = { flexGrow = 1 } };
        _graphView.AddManipulator(new ContentDragger());
        _graphView.AddManipulator(new SelectionDragger());
        _graphView.AddManipulator(new RectangleSelector());
        _graphView.SetupZoom(0.05f, 4.0f); // Allow deep zoom out

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

        // 1. Load Position
        Vector2 savedPos = new Vector2(
            EditorPrefs.GetFloat(saveKey + "X", index * (_defaultWidth + 50)),
            EditorPrefs.GetFloat(saveKey + "Y", 50)
        );

        var node = new Node { title = prop.displayName };
        node.SetPosition(new Rect(savedPos, new Vector2(_defaultWidth, 200)));

        // 2. Layout Settings
        node.style.width = StyleKeyword.Auto;
        node.style.minWidth = _defaultWidth;
        node.extensionContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        node.extensionContainer.style.paddingTop = 5;
        node.extensionContainer.style.paddingBottom = 5;
        node.extensionContainer.style.paddingLeft = 5;
        node.extensionContainer.style.paddingRight = 5;

        // Helper: Cell Styling
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

        void BuildDataUI(SerializedProperty p, VisualElement container, bool isVertical)
        {
            // === 1. ARRAY HANDLING (Manual Control) ===
            if (p.isArray && p.propertyType == SerializedPropertyType.Generic)
            {
                var headerContainer = new VisualElement();
                headerContainer.style.flexDirection = FlexDirection.Row;
                headerContainer.style.marginBottom = 5;
                headerContainer.style.alignItems = Align.Center;

                var foldout = new Foldout { text = p.displayName, value = true };
                foldout.style.flexGrow = 1;

                var sizeProp = p.FindPropertyRelative("Array.size");
                // Use Delayed Integer Field for Size too
                var sizeField = new IntegerField("Size") { isDelayed = true };
                sizeField.style.minWidth = 120;
                sizeField.bindingPath = sizeProp.propertyPath;
                sizeField.Bind(p.serializedObject);

                headerContainer.Add(foldout);
                headerContainer.Add(sizeField);
                container.Add(headerContainer);

                var listContent = new VisualElement();
                listContent.style.flexDirection = isVertical ? FlexDirection.Column : FlexDirection.Row;
                listContent.style.flexWrap = Wrap.NoWrap;
                listContent.style.marginLeft = 15;

                foldout.RegisterValueChangedCallback(evt => listContent.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None);
                container.Add(listContent);

                string arrayPath = p.propertyPath;
                SerializedObject so = p.serializedObject;

                void SyncContent()
                {
                    so.Update();
                    var freshArray = so.FindProperty(arrayPath);
                    if (freshArray == null) return;

                    int targetCount = freshArray.arraySize;
                    int currentCount = listContent.childCount;

                    if (targetCount == currentCount) return;

                    if (targetCount > currentCount)
                    {
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
                        {
                            listContent.RemoveAt(listContent.childCount - 1);
                        }
                    }
                }

                SyncContent();
                sizeField.RegisterCallback<ChangeEvent<int>>(evt => listContent.schedule.Execute(SyncContent));
            }
            // === 2. CLASS HANDLING ===
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
            // === 3. PRIMITIVE HANDLING (THE LAG FIX) ===
            else
            {
                VisualElement field = null;

                // We switch to Native Fields with 'isDelayed = true'.
                // This prevents the layout engine from recalculating on every keystroke.
                // It only recalculates when you press Enter or focus away.
                if (p.propertyType == SerializedPropertyType.String)
                {
                    field = new TextField(p.displayName) { isDelayed = true, bindingPath = p.propertyPath };
                }
                else if (p.propertyType == SerializedPropertyType.Integer)
                {
                    field = new IntegerField(p.displayName) { isDelayed = true, bindingPath = p.propertyPath };
                }
                else if (p.propertyType == SerializedPropertyType.Float)
                {
                    field = new FloatField(p.displayName) { isDelayed = true, bindingPath = p.propertyPath };
                }
                else if (p.propertyType == SerializedPropertyType.Boolean)
                {
                    field = new Toggle(p.displayName) { bindingPath = p.propertyPath };
                }
                else
                {
                    // Fallback for complex types (Vectors, Enums, ObjectRefs)
                    var propField = new PropertyField(p.Copy());
                    field = propField;
                }

                field.Bind(p.serializedObject);
                field.style.minWidth = 150;
                field.style.flexShrink = 0;
                container.Add(field);
            }
        }

        BuildDataUI(prop.Copy(), node.extensionContainer, true);
        node.RefreshExpandedState();
        _graphView.AddElement(node);

        // === 4. SAVE ON DRAG RELEASE ONLY ===
        node.RegisterCallback<MouseUpEvent>(evt => {
            var currentPos = node.GetPosition().position;
            EditorPrefs.SetFloat(saveKey + "X", currentPos.x);
            EditorPrefs.SetFloat(saveKey + "Y", currentPos.y);
        });
    }
}

public class SimpleGraphView : GraphView
{
    public void UpdateZoom(float scale) => UpdateViewTransform(viewTransform.position, new Vector3(scale, scale, 1));
}
