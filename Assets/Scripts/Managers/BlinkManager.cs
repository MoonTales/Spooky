using System;
using Player;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using Types = System.Types;

namespace Managers
{
    public class BlinkManager : Singleton<BlinkManager>
    {

        // this is gonna be added to the "sleepy" to make it feel more.. like sleepy and not crack
        // so when the player is tired, we will do a normalized value, where where you look straight or upwards, your eyes are open,
        // but if you look down they will passively close, with straight down being fully closed

        [SerializeField] private GameObject canvas;
        [SerializeField] private float eyelidSmoothSpeed = 5f;
        [SerializeField] private float BlinkTopLocation_FULLOPEN;
        [SerializeField] private float BlinkTopLocation_FULLCLOSED_MILDLYSLEEPDEPRIVED;
        [SerializeField] private float BlinkTopLocation_FULLCLOSED_MODERATELYSLEEPDEPRIVED;
        [SerializeField] private float BlinkTopLocation_FULLCLOSED_SEVERELYSLEEPDEPRIVED;
        // The bottom location just uses the negative of the top location for simplicity

        private Image _blinkImageTop;
        private Image _blinkImageBottom;
        
        private CinemachineCamera cinemaCamera;
        private CinemachinePanTilt panTilt;
        
        private float _blinkLocation = 900f;

        private bool _shouldAllowEyeDrop = false;
        
        private float _currentTopY;
        private float _currentBottomY;


        protected override void Awake()
        {
            base.Awake();
            _blinkImageTop = canvas.transform.Find("BlinkingTop").GetComponent<Image>();
            _blinkImageBottom = canvas.transform.Find("BlinkingBottom").GetComponent<Image>();
        }
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        

            cinemaCamera = PlayerManager.Instance.GetCinemachineCamera();
            panTilt = cinemaCamera.GetComponent<CinemachinePanTilt>();
        }

        public void RequestBlink(float duration = 1f)
        {
            
        }
        
        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(()=> EventBroadcaster.OnPlayerHealthStateChanged += OnPlayerHealthStateChanged,
                () => EventBroadcaster.OnPlayerHealthStateChanged -= OnPlayerHealthStateChanged);
        }

        private void OnPlayerHealthStateChanged(Types.PlayerMentalState newmentalstate)
        {
            /*
             * public enum PlayerMentalState
            {
            // for the anxiety side:
            Normal, // 100% - 80%
            MildlyAnxious, // 79% - 59%
            ModeratelyAnxious, // 58% - 29%
            SeverelyAnxious, // 28% - 10%
            Panic, // 9% - 1%
            // for the sleep deprivation side:
            MildlySleepDeprived, // 100% - 80%
            ModeratelySleepDeprived, // 60% - 25%
            SeverelySleepDeprived, // 25% - 10%
            Exhausted, // 10% - 1%
            // Common states:
            Breakdown, // when both anxiety and sleep deprivation are at their worst
            }
             */
            
            // we only wanna cause this to happen during: ModeratelySleepDeprived, SeverelySleepDeprived, and Exhausted
            if (newmentalstate == Types.PlayerMentalState.MildlySleepDeprived ||
                newmentalstate == Types.PlayerMentalState.ModeratelySleepDeprived ||
                newmentalstate == Types.PlayerMentalState.SeverelySleepDeprived ||
                newmentalstate == Types.PlayerMentalState.Exhausted)
            {
                if (newmentalstate == Types.PlayerMentalState.MildlySleepDeprived)
                {
                    _blinkLocation = BlinkTopLocation_FULLCLOSED_MILDLYSLEEPDEPRIVED;
                }
                if (newmentalstate == Types.PlayerMentalState.ModeratelySleepDeprived)
                {
                    _blinkLocation = BlinkTopLocation_FULLCLOSED_MODERATELYSLEEPDEPRIVED;
                }
                else if (newmentalstate == Types.PlayerMentalState.SeverelySleepDeprived)
                {
                    _blinkLocation = BlinkTopLocation_FULLCLOSED_SEVERELYSLEEPDEPRIVED;
                }
                _shouldAllowEyeDrop = true;
            }
            else
            {
                _shouldAllowEyeDrop = false;
            }
        }
        
        protected void OnGameStateChanged(Types.GameState newGameState)
        {

        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (GameStateManager.Instance.GetCurrentGameState() != Types.GameState.Gameplay) { return; }
 
            float targetTopY;
            float targetBottomY;
 
            if (_shouldAllowEyeDrop)
            {
                float currentTilt = panTilt.TiltAxis.Value;
                Debug.Log($"Current Blink location: {_blinkLocation}");
 
                // Positive tilt = looking up, negative = looking down
                float tiltMin = 80f; // looking straight down = fully closed
                float tiltMax = 0f;  // looking straight ahead = fully open
 
                // Normalize: 0 = fully closed (looking down), 1 = fully open (looking up/straight)
                float t = Mathf.InverseLerp(tiltMin, tiltMax, currentTilt);
                t = Mathf.Clamp01(t);
 
                targetTopY    = Mathf.Lerp(_blinkLocation, BlinkTopLocation_FULLOPEN, t);
                targetBottomY = Mathf.Lerp(-_blinkLocation, -BlinkTopLocation_FULLOPEN, t);
            }
            else
            {
                // Smoothly return to fully open when not sleep deprived
                targetTopY    = BlinkTopLocation_FULLOPEN;
                targetBottomY = -BlinkTopLocation_FULLOPEN;
            }
 
            // Smooth the eyelids toward their targets
            _currentTopY    = Mathf.Lerp(_currentTopY,    targetTopY,    Time.fixedDeltaTime * eyelidSmoothSpeed);
            _currentBottomY = Mathf.Lerp(_currentBottomY, targetBottomY, Time.fixedDeltaTime * eyelidSmoothSpeed);
 
            RectTransform topRect    = _blinkImageTop.rectTransform;
            RectTransform bottomRect = _blinkImageBottom.rectTransform;
 
            topRect.anchoredPosition    = new Vector2(topRect.anchoredPosition.x,    _currentTopY);
            bottomRect.anchoredPosition = new Vector2(bottomRect.anchoredPosition.x, _currentBottomY);
        }
    }
}
