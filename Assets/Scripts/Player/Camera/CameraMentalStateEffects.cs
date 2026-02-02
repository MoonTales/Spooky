using System;
using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Types = System.Types;

namespace Player.Camera
{
    
    public class CameraMentalStateEffects : EventSubscriberBase
    {
        // Setting up structs (these can be private, since I know no other class will need these)
        [Serializable]
        private struct TiredSwaySettings
        {
            public float intensity;
            public float speed;
            public float verticalBias;
            public bool verticalSin;
            public bool horizontalSin;
            public float verticalFrequency;
            public float horizontalFrequency;
        }
        private CinemachineCamera _cinemachineCamera;
        private CinemachinePanTilt _panTilt;
        private Volume _postProcessVolume;
        private DepthOfField _depthOfField;
        private ChromaticAberration _chromaticAberration;

        // Track current mental state and active coroutine
        private Types.PlayerMentalState _currentMentalState;
        private Coroutine _activeSwayCoroutine;
        
        
        
        
        // Now we are gonna have our serialized fields that we can tweak in the inspector
        [SerializeField]
        TiredSwaySettings mildlyTiredSwaySettings = new TiredSwaySettings
        {
            intensity = 5.3f,
            speed = 0.25f,
            verticalBias = 0.7f,
            verticalSin = false,
            horizontalSin = true,
            verticalFrequency = 0.25f,
            horizontalFrequency = 0.75f
        };
        [SerializeField]
        TiredSwaySettings moderatelyTiredSwaySettings = new TiredSwaySettings
        {
            intensity = 6.3f,
            speed = 0.30f,
            verticalBias = 0.7f,
            verticalSin = false,
            horizontalSin = true,
            verticalFrequency = 0.5f,
            horizontalFrequency = 0.85f
        };
        [SerializeField]
        TiredSwaySettings severelyTiredSwaySettings = new TiredSwaySettings
        {
            intensity = 8.3f,
            speed = 0.50f,
            verticalBias = 0.7f,
            verticalSin = false,
            horizontalSin = true,
            verticalFrequency = 0.7f,
            horizontalFrequency = 0.9f
        };
        [SerializeField]
        TiredSwaySettings exhaustedTiredSwaySettings = new TiredSwaySettings
        {
            intensity = 12.3f,
            speed = 0.75f,
            verticalBias = 0.75f,
            verticalSin = false,
            horizontalSin = true,
            verticalFrequency = 0.9f,
            horizontalFrequency = 1f
        };

        private float _currentBlurIntensity;
        private float _currentFocusDistance;
        private float _currentChromaticAberration;
        private float _targetBlurIntensity;
        private float _targetFocusDistance;
        private float _targetChromaticAberration;
        private float transitionSpeed = 0.25f;


        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(() => EventBroadcaster.OnPlayerHealthStateChanged += OnPlayerMentalStateChanged,
                () => EventBroadcaster.OnPlayerHealthStateChanged -= OnPlayerMentalStateChanged);
        }

        private void Start()
        {
            _cinemachineCamera = PlayerManager.Instance.GetCinemachineCamera();
            _panTilt = _cinemachineCamera.GetComponent<CinemachinePanTilt>();
            _currentMentalState = Types.PlayerMentalState.Normal;
            OnPlayerMentalStateChanged(_currentMentalState); // sets our initial state, incase we have "normal" logic
            InitializePostProcessing();
        }

        private void Update()
        {
            // Smoothly lerp blur and chromatic aberration values
            _currentBlurIntensity = Mathf.Lerp(_currentBlurIntensity, _targetBlurIntensity, Time.deltaTime * transitionSpeed);
            _currentFocusDistance = Mathf.Lerp(_currentFocusDistance, _targetFocusDistance, Time.deltaTime * transitionSpeed);
            _currentChromaticAberration = Mathf.Lerp(_currentChromaticAberration, _targetChromaticAberration, Time.deltaTime * transitionSpeed);
    
            // Apply the current values
            if (_currentBlurIntensity > 0.01f)
            {
                ApplyBlurEffect(_currentBlurIntensity, _currentFocusDistance);
            }
            else if (_depthOfField != null && _depthOfField.active)
            {
                _depthOfField.active = false;
            }
    
            SetChromaticAberrationIntensity(_currentChromaticAberration);
        }

        private void OnPlayerMentalStateChanged(Types.PlayerMentalState newMentalState)
        {
            // Stop any active sway effect when state changes
            if (_activeSwayCoroutine != null)
            {
                StopCoroutine(_activeSwayCoroutine);
                _activeSwayCoroutine = null;
            }
            
            _currentMentalState = newMentalState;
            
            switch (newMentalState)
            {
                case Types.PlayerMentalState.Normal:
                    HandleNormalStateEffects();
                    break;
                case Types.PlayerMentalState.MildlyAnxious:
                    HandleMildlyAnxiousEffects();
                    break;
                case Types.PlayerMentalState.ModeratelyAnxious:
                    HandleModeratelyAnxiousEffects();
                    break;
                case Types.PlayerMentalState.SeverelyAnxious:
                    HandleSeverelyAnxiousEffects();
                    break;
                case Types.PlayerMentalState.Panic:
                    HandlePanicEffects();
                    break;
                case Types.PlayerMentalState.MildlySleepDeprived:
                    HandleMildlySleepDeprivedEffects();
                    break;
                case Types.PlayerMentalState.ModeratelySleepDeprived:
                    HandleModeratelySleepDeprivedEffects();
                    break;
                case Types.PlayerMentalState.SeverelySleepDeprived:
                    HandleSeverelySleepDeprivedEffects();
                    break;
                case Types.PlayerMentalState.Exhausted:
                    HandleExhaustedEffects();
                    break;
                case Types.PlayerMentalState.Breakdown:
                    HandleBreakdownEffects();
                    break;
                default:
                    break;
            }
        }

        private void HandleNormalStateEffects()
        {
            _targetBlurIntensity = 0f;
            _targetFocusDistance = 10f;
            _targetChromaticAberration = 0f;
        }

        private void HandleMildlyAnxiousEffects()
        {
            // unique effects
        }

        private void HandleModeratelyAnxiousEffects()
        {
            // unique effects
        }

        private void HandleSeverelyAnxiousEffects()
        {
            // unique effects
        }

        private void HandlePanicEffects()
        {
            // unique effects
        }

        private void HandleMildlySleepDeprivedEffects()
        {
            _activeSwayCoroutine = StartCoroutine(TiredSwayCoroutine(mildlyTiredSwaySettings));
            // Add blur effect
            _targetBlurIntensity = 10f;
            _targetFocusDistance = 3f;
            _targetChromaticAberration = 1f;
            //ApplyBlurEffect(blurIntensity: 10f, focusDistance: 3f);
            //SetChromaticAberrationIntensity(1);
        }

        private void HandleModeratelySleepDeprivedEffects()
        {
            _activeSwayCoroutine = StartCoroutine(TiredSwayCoroutine(moderatelyTiredSwaySettings));
            _targetBlurIntensity = 15f;
            _targetFocusDistance = 2f;
            _targetChromaticAberration = 2f;
            //ApplyBlurEffect(blurIntensity: 15f, focusDistance: 2f);
            //SetChromaticAberrationIntensity(2);
        }

        private void HandleSeverelySleepDeprivedEffects()
        {
            _activeSwayCoroutine = StartCoroutine(TiredSwayCoroutine(severelyTiredSwaySettings));
            _targetBlurIntensity = 20f;
            _targetFocusDistance = 1f;
            _targetChromaticAberration = 3f;
            //ApplyBlurEffect(blurIntensity: 20f, focusDistance: 1f);
            //SetChromaticAberrationIntensity(3);
        }


        private void HandleExhaustedEffects()
        {
            _activeSwayCoroutine = StartCoroutine(TiredSwayCoroutine(exhaustedTiredSwaySettings));
            _targetBlurIntensity = 25f;
            _targetFocusDistance = 0.5f;
            _targetChromaticAberration = 4f;
            //ApplyBlurEffect(blurIntensity: 25f, focusDistance: 0.5f);
            //SetChromaticAberrationIntensity(4);
        }

        private void HandleBreakdownEffects()
        {
        }

        private IEnumerator TiredSwayCoroutine(TiredSwaySettings settings)
        {
            
            float time = 0f;

            while (_currentMentalState != Types.PlayerMentalState.Normal)
            {
                time += Time.deltaTime * settings.speed;
                float verticalSway = 0f;
                float horizontalSway = 0f;

                // Slow, smooth sine wave for tired swaying
                if (settings.horizontalSin)
                {
                    horizontalSway = Mathf.Sin(time * settings.horizontalFrequency) * settings.intensity * (1f - settings.verticalBias);
                    _panTilt.PanAxis.Value += horizontalSway * Time.deltaTime;
                }
                else
                {
                    horizontalSway = settings.intensity * (1f - settings.verticalBias) * settings.speed * Time.deltaTime;
                    _panTilt.PanAxis.Value += horizontalSway;
                }

                // we either sin it, or treat it as a constant downward pull
                if (settings.verticalSin)
                {
                    verticalSway = Mathf.Sin(time * settings.verticalFrequency) * settings.intensity * settings.verticalBias;
                    _panTilt.TiltAxis.Value += verticalSway * Time.deltaTime;
                }
                else
                {
                    verticalSway = settings.intensity * settings.verticalBias * settings.speed * Time.deltaTime;
                    // possitive.. cause it just is lol
                    _panTilt.TiltAxis.Value += verticalSway;
                }

                yield return null;
            }
        }




        private void ApplyBlurEffect(float blurIntensity, float focusDistance)
        {
            if(!_postProcessVolume || !_depthOfField) { return; }
            
            
            // Configure blur settings
            _depthOfField.mode.value = DepthOfFieldMode.Bokeh; // Might try gaussian soon
            _depthOfField.focusDistance.value = focusDistance; // things close to this distance are in focus, anything else is blurred
            _depthOfField.aperture.value = Mathf.Lerp(32f, 0.1f, blurIntensity); // Higher aperture = more blur (I think 32 is the max value)
            _depthOfField.focalLength.value = 50f;
            _depthOfField.active = true;

        }

        private void SetChromaticAberrationIntensity(float amount)
        {
            if (!_postProcessVolume || !_chromaticAberration) { return; }
            _chromaticAberration.intensity.Override(amount);
        }

        private void RemoveBlurEffect()
        {
            _targetBlurIntensity = 0f;
            _targetFocusDistance = 10f;
            _currentBlurIntensity = 0f;
            _currentFocusDistance = 10f;
            
            if (_depthOfField != null)
            {
                _depthOfField.active = false;
            }
        }

        private void RemoveChromaticAberrationEffect()
        {
            _currentChromaticAberration = 0f;
            _targetChromaticAberration = 0f;
            if (_chromaticAberration != null)
            {
                _chromaticAberration.intensity.Override(0f);
            }

        }

        #region Utility Functions

        private void TrySetPostProcessing()
        {
            // Searches for a post process volume on this object, and creates one if none exists
            if (_postProcessVolume == null)
            {
                _postProcessVolume = GetComponent<Volume>();

                if (_postProcessVolume == null)
                {
                    _postProcessVolume = gameObject.AddComponent<Volume>();
                    _postProcessVolume.isGlobal = true;
                    _postProcessVolume.priority = 1;
                    _postProcessVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
                }
            }
        }
        
        private void TrySetChromaticAberration()
        {
            // tries to get the chromatic aberration effect from the post processing volume
            if (_postProcessVolume != null)
            {
                if (!_postProcessVolume.profile.TryGet(out _chromaticAberration))
                {
                    _chromaticAberration = _postProcessVolume.profile.Add<ChromaticAberration>(true);
                }
            }
        }

        private void TrySetFieldOfDepth()
        {
            // Tries to get the depth of field effect from the post processing volume
            if (!_postProcessVolume.profile.TryGet(out _depthOfField))
            {
                _depthOfField = _postProcessVolume.profile.Add<DepthOfField>(true);
            }
        }

        private void InitializePostProcessing()
        {
            // set the post processing volume on this object (or creates one if none exists)
            TrySetPostProcessing();
            // tries to set the chromatic aberration effect from the post processing volume
            TrySetChromaticAberration();
            // tries to set the depth of field effect from the post processing volume
            TrySetFieldOfDepth();
        }

        #endregion
    }
}