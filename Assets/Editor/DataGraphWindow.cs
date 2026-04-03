using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using System.Collections.Generic;

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
        Vector2 pos = new Vector2(EditorPrefs.GetFloat(saveKey + "X", index * (_defaultWidth + 50)), EditorPrefs.GetFloat(saveKey + "Y", 50));

        var node = new Node { title = prop.displayName };
        node.SetPosition(new Rect(pos, new Vector2(_defaultWidth, 200)));

        // Node Styling
        node.style.width = StyleKeyword.Auto;
        node.style.minWidth = _defaultWidth;
        node.extensionContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        node.extensionContainer.style.paddingTop = 5;
        node.extensionContainer.style.paddingBottom = 5;
        node.extensionContainer.style.paddingLeft = 5;
        node.extensionContainer.style.paddingRight = 5;

        VisualElement CreateCellWrapper()
        {
            var wrapper = new VisualElement();
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
            // === 1. ARRAY HANDLING (Manual Control + Fast Diff) ===
            if (p.isArray && p.propertyType == SerializedPropertyType.Generic)
            {
                // A. The Header Container
                var headerContainer = new VisualElement();
                headerContainer.style.flexDirection = FlexDirection.Row;
                headerContainer.style.marginBottom = 5;
                headerContainer.style.alignItems = Align.Center; // Centers items vertically

                // B. Manual Foldout
                var foldout = new Foldout { text = p.displayName, value = true };
                foldout.style.flexGrow = 1;

                // C. FIX 1: Explicit IntegerField for Size Input
                // We use a real IntegerField instead of PropertyField to guarantee an input box appears.
                var sizeProp = p.FindPropertyRelative("Array.size");
                var sizeField = new IntegerField("Size");
                sizeField.style.minWidth = 120; // Ensure enough space for label + input
                sizeField.bindingPath = sizeProp.propertyPath; // Bind manually
                sizeField.Bind(p.serializedObject);

                headerContainer.Add(foldout);
                headerContainer.Add(sizeField);
                container.Add(headerContainer);

                // D. Content Container
                var listContent = new VisualElement();
                listContent.style.flexDirection = isVertical ? FlexDirection.Column : FlexDirection.Row;
                listContent.style.flexWrap = Wrap.NoWrap;
                listContent.style.marginLeft = 15;

                foldout.RegisterValueChangedCallback(evt => listContent.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None);
                container.Add(listContent);

                // Capture References
                string arrayPath = p.propertyPath;
                SerializedObject so = p.serializedObject;

                // E. FIX 2: The Fast Sync Logic
                // Instead of destroying the list (Rebuild), we simply Add or Remove items.
                // This restores the speed of the graph creation and updates.
                void SyncContent()
                {
                    so.Update();
                    var freshArray = so.FindProperty(arrayPath);
                    if (freshArray == null) return;

                    int targetCount = freshArray.arraySize;
                    int currentCount = listContent.childCount;

                    // Optimization: If counts match, do nothing (Fastest possible path)
                    if (targetCount == currentCount) return;

                    // Grow: Add new items
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
                    // Shrink: Remove from end
                    else
                    {
                        while (listContent.childCount > targetCount)
                        {
                            listContent.RemoveAt(listContent.childCount - 1);
                        }
                    }
                }

                // Initial Build
                SyncContent();

                // F. The Anti-Lag Listener
                // We keep the exact logic that fixed the lag: listening to the IntegerField.
                sizeField.RegisterCallback<ChangeEvent<int>>(evt =>
                {
                    listContent.schedule.Execute(SyncContent);
                });
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
            // === 3. SIMPLE PROPERTY HANDLING ===
            else
            {
                var field = new PropertyField(p.Copy());
                field.Bind(p.serializedObject);
                field.style.minWidth = 150;
                container.Add(field);
            }
        }

        BuildDataUI(prop.Copy(), node.extensionContainer, true);
        node.RefreshExpandedState();
        _graphView.AddElement(node);

        node.RegisterCallback<GeometryChangedEvent>(evt => {
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
