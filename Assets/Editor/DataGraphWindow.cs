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
        // --- TOOLBAR ---
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

        // --- GRAPH CANVAS ---
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

        // Set styling to allow infinite vertical growth
        node.style.width = _defaultWidth;
        node.style.height = StyleKeyword.Auto;
        node.mainContainer.style.height = StyleKeyword.Auto;
        node.extensionContainer.style.height = StyleKeyword.Auto;
        node.extensionContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // --- RECURSIVE DATA BUILDING ---
        // This function manually builds the fields so they never use a ScrollView
        void BuildDataUI(SerializedProperty p, VisualElement container)
        {
            if (p.isArray && p.propertyType == SerializedPropertyType.Generic)
            {
                // Manual List Header
                var foldout = new Foldout { text = $"{p.displayName} (List: {p.arraySize})", value = true };
                container.Add(foldout);

                // Create a field for every element in the list manually
                for (int i = 0; i < p.arraySize; i++)
                {
                    var element = p.GetArrayElementAtIndex(i);
                    var elementContainer = new VisualElement { style = { paddingLeft = 15, paddingBottom = 2 } };
                    BuildDataUI(element, elementContainer); // Recursive call for nested data
                    foldout.Add(elementContainer);
                }
            }
            else
            {
                // For standard variables, just use a basic PropertyField
                var field = new PropertyField(p.Copy());
                field.Bind(p.serializedObject);
                container.Add(field);
            }
        }

        BuildDataUI(prop.Copy(), node.extensionContainer);

        node.RefreshExpandedState();
        _graphView.AddElement(node);

        // Save Position callback
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
