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
    private float _defaultWidth = 400f;

    [MenuItem("Window/Custom/Data Dashboard Graph")]
    public static void Open() => GetWindow<DataGraphWindow>("Data Dashboard");

    private void CreateGUI()
    {
        var toolbar = new Toolbar();

        var picker = new ObjectField("Target") { objectType = typeof(MonoBehaviour), style = { width = 250 } };
        picker.RegisterValueChangedCallback(evt => { _currentTarget = evt.newValue as MonoBehaviour; RefreshNodes(); });

        var refreshBtn = new Button(RefreshNodes) { text = "Refresh Graph" };

        var widthSlider = new Slider("Node Width", 200, 1000) { value = _defaultWidth, style = { width = 200 } };
        widthSlider.RegisterValueChangedCallback(evt => { _defaultWidth = evt.newValue; RefreshNodes(); });

        var zoomSlider = new Slider("Zoom", 0.1f, 2.0f) { value = 1.0f, style = { width = 150 } };
        zoomSlider.RegisterValueChangedCallback(evt => _graphView.UpdateZoom(evt.newValue));

        toolbar.Add(picker);
        toolbar.Add(refreshBtn);
        toolbar.Add(new ToolbarSpacer());
        toolbar.Add(widthSlider);
        toolbar.Add(zoomSlider);
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

        // Base Node Styling
        node.style.width = StyleKeyword.Auto;
        node.style.minWidth = _defaultWidth;
        node.style.height = StyleKeyword.Auto;

        node.extensionContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        node.extensionContainer.style.paddingTop = 5;
        node.extensionContainer.style.paddingBottom = 5;
        node.extensionContainer.style.paddingLeft = 5;
        node.extensionContainer.style.paddingRight = 5;

        // if isVertical is true then Top-to-Bottom, if it is false then Left-to-Right
        void BuildDataUI(SerializedProperty p, VisualElement container, bool isVertical)
        {
            // If it is an array (or list technically)
            if (p.isArray && p.propertyType == SerializedPropertyType.Generic)
            {
                // Create a Header PropertyField to show the size/buttons
                var listHeaderField = new PropertyField(p.Copy());
                listHeaderField.Bind(p.serializedObject);

                // Use a schedule to find and hide ONLY the scrollable list part
                listHeaderField.schedule.Execute(() => {
                    // This never ended up working. Oh well
                    var internalListView = listHeaderField.Q<ListView>();
                    if (internalListView != null)
                    {
                        internalListView.style.display = DisplayStyle.None;
                    }
                }).ExecuteLater(50); // Small delay to let Unity build the internal list

                container.Add(listHeaderField);

                // Create the custom non-scrolling grid view below the header
                var listContent = new VisualElement();
                listContent.style.flexDirection = isVertical ? FlexDirection.Column : FlexDirection.Row;
                listContent.style.flexWrap = Wrap.NoWrap;
                listContent.style.marginLeft = 15; // Indent slightly below the buttons

                for (int i = 0; i < p.arraySize; i++)
                {
                    var element = p.GetArrayElementAtIndex(i);
                    var elementWrapper = new VisualElement();

                    // Cell Styling (corrected for IStyle)
                    elementWrapper.style.paddingTop = 5; elementWrapper.style.paddingBottom = 5;
                    elementWrapper.style.paddingLeft = 5; elementWrapper.style.paddingRight = 5;
                    elementWrapper.style.borderTopWidth = 1; elementWrapper.style.borderBottomWidth = 1;
                    elementWrapper.style.borderLeftWidth = 1; elementWrapper.style.borderRightWidth = 1;
                    elementWrapper.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                    elementWrapper.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                    elementWrapper.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                    elementWrapper.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                    elementWrapper.style.backgroundColor = new Color(1, 1, 1, 0.03f);

                    BuildDataUI(element, elementWrapper, !isVertical);
                    listContent.Add(elementWrapper);
                }
                container.Add(listContent);
            }
            // If it is a class
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
            // for simple variables
            else
            {
                var field = new PropertyField(p.Copy());
                field.Bind(p.serializedObject);
                field.style.minWidth = 150;
                container.Add(field);
            }
        }

        // Start with Vertical for the main list
        BuildDataUI(prop.Copy(), node.extensionContainer, true);

        node.RefreshExpandedState();
        _graphView.AddElement(node);

        // Save Position
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
