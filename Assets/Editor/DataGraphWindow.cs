using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;

public class DataGraphWindow : EditorWindow
{
    private SimpleGraphView _graphView;
    private MonoBehaviour _currentTarget;

    [MenuItem("Window/Custom/Data Dashboard Graph")]
    public static void Open() => GetWindow<DataGraphWindow>("Data Dashboard");

    private void CreateGUI()
    {
        // Create the Toolbar
        var toolbar = new VisualElement { style = { flexDirection = FlexDirection.Row, backgroundColor = new Color(0.2f, 0.2f, 0.2f) } };

        var picker = new ObjectField("Target") { objectType = typeof(MonoBehaviour), style = { flexGrow = 1 } };
        picker.RegisterValueChangedCallback(evt => {
            _currentTarget = evt.newValue as MonoBehaviour;
            RefreshNodes();
        });

        var refreshBtn = new Button(RefreshNodes) { text = "Refresh Graph" };

        toolbar.Add(picker);
        toolbar.Add(refreshBtn);
        rootVisualElement.Add(toolbar);

        // Create the GraphView using the wrapper class below
        _graphView = new SimpleGraphView { style = { flexGrow = 1 } };
        _graphView.AddManipulator(new ContentDragger());
        _graphView.AddManipulator(new SelectionDragger());
        _graphView.AddManipulator(new RectangleSelector());
        _graphView.SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        rootVisualElement.Add(_graphView);
    }

    private void RefreshNodes()
    {
        _graphView.DeleteElements(_graphView.graphElements);
        if (_currentTarget == null) return;

        var so = new SerializedObject(_currentTarget);
        var prop = so.GetIterator();
        prop.NextVisible(true);

        int count = 0;
        while (prop.NextVisible(false))
        {
            if (prop.hasChildren)
            {
                string saveKey = $"{_currentTarget.GetType().Name}_{prop.name}_pos";
                float savedX = EditorPrefs.GetFloat(saveKey + "X", count * 350);
                float savedY = EditorPrefs.GetFloat(saveKey + "Y", 50);

                CreateDataNode(prop.displayName, _currentTarget, prop.propertyPath, new Vector2(savedX, savedY), saveKey);
                count++;
            }
        }
    }

    private void CreateDataNode(string title, Object target, string propertyPath, Vector2 pos, string saveKey)
    {
        var node = new Node { title = title };
        node.SetPosition(new Rect(pos, new Vector2(350, 200)));

        node.RegisterCallback<GeometryChangedEvent>(evt => {
            var currentPos = node.GetPosition().position;
            EditorPrefs.SetFloat(saveKey + "X", currentPos.x);
            EditorPrefs.SetFloat(saveKey + "Y", currentPos.y);
        });

        var so = new SerializedObject(target);
        var propertyField = new PropertyField(so.FindProperty(propertyPath));
        propertyField.Bind(so);

        node.extensionContainer.Add(propertyField);
        node.RefreshExpandedState();
        _graphView.AddElement(node);
    }
}

// The Wrapper Class to fix the "Abstract Class" error
public class SimpleGraphView : GraphView { }
