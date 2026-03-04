using UnityEngine;
using UnityEngine.EventSystems;

// literally just so that we can track when the cursor is in or out of the scrollview
// so it stops fucking doing the thing where it INSPECTS THE OBJECT WHILE SCROLLING TH ETEXT
public class CursorInScrollView : MonoBehaviour
{
    [SerializeField] private RectTransform scrollViewRect;
    [SerializeField] private Canvas parentCanvas;

    public bool IsCursorInScrollView()
    {
        // HUD canvas render mode = screen space overlay
        // so screen coords are reliable
        return RectTransformUtility.RectangleContainsScreenPoint(
            scrollViewRect,
            Input.mousePosition
        );
    }
}