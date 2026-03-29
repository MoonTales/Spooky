using Player;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace Managers
{
    public class BlinkManager : MonoBehaviour
    {

        // this is gonna be added to the "sleepy" to make it feel more.. like sleepy and not crack
        // so when the player is tired, we will do a normalized value, where where you look straight or upwards, your eyes are open,
        // but if you look down they will passively close, with straight down being fully closed

        [SerializeField] private GameObject canvas;
        [SerializeField] private float BlinkTopLocation_FULLOPEN;
        [SerializeField] private float BlinkTopLocation_FULLCLOSED;
        [SerializeField] private float BlinkBottomLocation_FULLOPEN;
        [SerializeField] private float BlinkBottomLocation_FULLCLOSED;

        private Image _blinkImageTop;
        private Image _blinkImageBottom;
        
        private CinemachineCamera cinemaCamera;
        private CinemachinePanTilt panTilt;
    
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        
            _blinkImageTop = canvas.transform.Find("BlinkingTop").GetComponent<Image>();
            _blinkImageBottom = canvas.transform.Find("BlinkingBottom").GetComponent<Image>();
            cinemaCamera = PlayerManager.Instance.GetCinemachineCamera();
            panTilt = cinemaCamera.GetComponent<CinemachinePanTilt>();
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            float currentTilt = panTilt.TiltAxis.Value;

            // Define your tilt range — adjust these to match your camera's tilt limits
            // Positive tilt = looking up, negative = looking down
            float tiltMin = 80f; // looking straight down = fully closed
            float tiltMax = 0f;   // looking straight ahead = fully open

            // Normalize: 0 = fully closed (looking down), 1 = fully open (looking up/straight)
            float t = Mathf.InverseLerp(tiltMin, tiltMax, currentTilt);
            t = Mathf.Clamp01(t);

            // Move the top eyelid down as t decreases (more tired/looking down)
            float topY = Mathf.Lerp(BlinkTopLocation_FULLCLOSED, BlinkTopLocation_FULLOPEN, t);
            // Move the bottom eyelid up as t decreases
            float bottomY = Mathf.Lerp(BlinkBottomLocation_FULLCLOSED, BlinkBottomLocation_FULLOPEN, t);

            RectTransform topRect = _blinkImageTop.rectTransform;
            RectTransform bottomRect = _blinkImageBottom.rectTransform;

            topRect.anchoredPosition = new Vector2(topRect.anchoredPosition.x, topY);
            bottomRect.anchoredPosition = new Vector2(bottomRect.anchoredPosition.x, bottomY);
        }
    }
}
