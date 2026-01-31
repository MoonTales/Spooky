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
        private CinemachineCamera _cinemachineCamera;
        private CinemachinePanTilt _panTilt;
        private Volume _postProcessVolume;
        private DepthOfField _depthOfField;
        private ChromaticAberration _chromaticAberration;

        // Track current mental state and active coroutine
        private Types.PlayerMentalState _currentMentalState;
        private Coroutine _activeSwayCoroutine;


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
            InitializePostProcessing();
        }

        private void OnPlayerMentalStateChanged(Types.PlayerMentalState newMentalState)
        {
            // Stop any active sway effect when state changes
            if (_activeSwayCoroutine != null)
            {
                StopCoroutine(_activeSwayCoroutine);
                _activeSwayCoroutine = null;
            }

            RemoveBlurEffect();
            RemoveChromaticAberrationEffect();
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
            RemoveBlurEffect(); // Add this!
        }

        private void HandleMildlyAnxiousEffects()
        {
        }

        private void HandleModeratelyAnxiousEffects()
        {
        }

        private void HandleSeverelyAnxiousEffects()
        {
        }

        private void HandlePanicEffects()
        {
        }

        private void HandleMildlySleepDeprivedEffects()
        {
            _activeSwayCoroutine = StartCoroutine(TiredSwayCoroutine(
                intensity: 5.3f,
                speed: 0.25f,
                verticalBias: 0.7f,
                verticalSin: false,
                horizontalSin: true,
                verticalFrequency: 0.25f,
                horizontalFrequency: 0.75f
            ));
            // Add blur effect
            ApplyBlurEffect(blurIntensity: 10f, focusDistance: 3f);
            SetChromaticAberrationIntensity(1);
        }

        private void HandleModeratelySleepDeprivedEffects()
        {
            _activeSwayCoroutine = StartCoroutine(TiredSwayCoroutine(
                intensity: 6.3f,
                speed: 0.30f,
                verticalBias: 0.7f,
                verticalSin: false,
                horizontalSin: true,
                verticalFrequency: 0.5f,
                horizontalFrequency: 0.85f
            ));
            ApplyBlurEffect(blurIntensity: 15f, focusDistance: 2f);
            SetChromaticAberrationIntensity(2);
        }

        private void HandleSeverelySleepDeprivedEffects()
        {
            _activeSwayCoroutine = StartCoroutine(TiredSwayCoroutine(
                intensity: 8.3f,
                speed: 0.50f,
                verticalBias: 0.7f,
                verticalSin: false,
                horizontalSin: true,
                verticalFrequency: 0.7f,
                horizontalFrequency: 0.9f
            ));
            ApplyBlurEffect(blurIntensity: 20f, focusDistance: 1f);
            SetChromaticAberrationIntensity(3);
        }


        private void HandleExhaustedEffects()
        {
            _activeSwayCoroutine = StartCoroutine(TiredSwayCoroutine(
                intensity: 12.3f,
                speed: 0.75f,
                verticalBias: 0.75f,
                verticalSin: false,
                horizontalSin: true,
                verticalFrequency: 0.9f,
                horizontalFrequency: 1f
            ));
            ApplyBlurEffect(blurIntensity: 25f, focusDistance: 0.5f);
            SetChromaticAberrationIntensity(4);
        }

        private void HandleBreakdownEffects()
        {
        }

        private IEnumerator TiredSwayCoroutine(float intensity, float speed, float verticalBias, bool verticalSin,
            bool horizontalSin, float verticalFrequency = 0.7f, float horizontalFrequency = 1f)
        {
            float time = 0f;

            while (_currentMentalState != Types.PlayerMentalState.Normal)
            {
                time += Time.deltaTime * speed;
                float verticalSway = 0f;
                float horizontalSway = 0f;

                // Slow, smooth sine wave for tired swaying
                if (horizontalSin)
                {
                    horizontalSway = Mathf.Sin(time * horizontalFrequency) * intensity * (1f - verticalBias);
                    _panTilt.PanAxis.Value += horizontalSway * Time.deltaTime;
                }
                else
                {
                    horizontalSway = intensity * (1f - verticalBias) * speed * Time.deltaTime;
                    _panTilt.PanAxis.Value += horizontalSway;
                }

                // we either sin it, or treat it as a constant downward pull
                if (verticalSin)
                {
                    verticalSway = Mathf.Sin(time * verticalFrequency) * intensity * verticalBias;
                    _panTilt.TiltAxis.Value += verticalSway * Time.deltaTime;
                }
                else
                {
                    verticalSway = intensity * verticalBias * speed * Time.deltaTime;
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
            if (_depthOfField != null)
            {
                _depthOfField.active = false;
            }
        }

        private void RemoveChromaticAberrationEffect()
        {
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