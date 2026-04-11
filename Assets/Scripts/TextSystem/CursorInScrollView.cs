using UnityEngine;
using UnityEngine.UI;

// literally just so that we can track when the cursor is in or out of the scrollview
// so it stops fucking doing the thing where it INSPECTS THE OBJECT WHILE SCROLLING TH ETEXT
public class CursorInScrollView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform scrollViewRect;
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private Scrollbar vertScrollbar;

    [Header("Pulse Settings")]
    [SerializeField] private float minAlpha = 0.1f;
    [SerializeField] private float maxAlpha = 0.3f;
    [SerializeField] private float pulseSpeed = 1f;

    private bool hasScrolledThisScene = false;

    private Graphic[] scrollbarGraphics;
    private float originalAlpha = 0f;

    private void Awake()
    {
        if (vertScrollbar != null)
        {
            scrollbarGraphics = vertScrollbar.GetComponentsInChildren<Graphic>(true);
            originalAlpha = scrollbarGraphics[0].color.a;
        }
    }

    private void Update()
    {

        // if scroll, permanently stop the hint for this scene
        if (!hasScrolledThisScene && IsCursorInScrollView() && Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f)
        {
            hasScrolledThisScene = true;
            SetScrollbarAlpha(originalAlpha);
            return;
        }

        // pulse only if cursor is inside + user hasn't scrolled yet
        if (!hasScrolledThisScene && IsCursorInScrollView())
        {
            float t = Mathf.PingPong(Time.unscaledTime * pulseSpeed, 1f);
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
            SetScrollbarAlpha(alpha);
        }
        else
        {
            SetScrollbarAlpha(originalAlpha);
        }
    }

    public bool IsCursorInScrollView()
    {
        // HUD canvas render mode = screen space overlay
        // so screen coords are reliable

        return RectTransformUtility.RectangleContainsScreenPoint(
            scrollViewRect,
            Input.mousePosition
        );
    }

    // set transparency - default is actually 0 
    private void SetScrollbarAlpha(float alpha)
    {
        if (scrollbarGraphics == null)
            return;

        for (int i = 0; i < scrollbarGraphics.Length; i++)
        {
            Color c = scrollbarGraphics[i].color;
            c.a = alpha;
            scrollbarGraphics[i].color = c;
        }
    }
}