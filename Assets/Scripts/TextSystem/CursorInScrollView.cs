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
        if (scrollViewRect == null) return false;

        Camera eventCamera = null;

        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = parentCanvas.worldCamera;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(
            scrollViewRect,
            Input.mousePosition,
            eventCamera
        );
    }
}