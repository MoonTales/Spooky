using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Types = System.Types;

namespace Player.Camera
{
    
    public class CameraMentalStateEffects : Singleton<CameraMentalStateEffects>
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
        private List<Volume> _postProcessVolumes = new List<Volume>();
        private List<DepthOfField> _depthOfFields = new List<DepthOfField>();
        private List<ChromaticAberration> _chromaticAberrations = new List<ChromaticAberration>();

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
        private float transitionSpeed = 5f;//0.25f; // lets make this settable aswell
        
        public void ResetEffects()
        {
            OnPlayerMentalStateChanged(Types.PlayerMentalState.Normal);
        }
        
        public void SetCustomVisualEffects(float blurIntensity, float focusDistance, float chromaticAberration, float newTransitionSpeed)
        {
            _targetBlurIntensity = blurIntensity;
            _targetFocusDistance = focusDistance;
            _targetChromaticAberration = chromaticAberration;
            transitionSpeed = newTransitionSpeed;
        }
        

        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(() => EventBroadcaster.OnPlayerHealthStateChanged += OnPlayerMentalStateChanged,
                () => EventBroadcaster.OnPlayerHealthStateChanged -= OnPlayerMentalStateChanged);
            TrackSubscription(() => EventBroadcaster.OnGameRestarted += HandleNormalStateEffects,
                () => EventBroadcaster.OnGameRestarted -= HandleNormalStateEffects);
            TrackSubscription(() => EventBroadcaster.OnWorldLocationChangedEvent += OnWorldLocationChanged,
                () => EventBroadcaster.OnWorldLocationChangedEvent -= OnWorldLocationChanged);
        }

        private void Start()
        {
            _cinemachineCamera = PlayerManager.Instance.GetCinemachineCamera();
            _panTilt = _cinemachineCamera.GetComponent<CinemachinePanTilt>();
            _currentMentalState = Types.PlayerMentalState.Normal;
            OnPlayerMentalStateChanged(_currentMentalState); // sets our initial state, incase we have "normal" logic
            InitializePostProcessing();
        }

        private void OnWorldLocationChanged(Types.WorldLocation newLocation)
        {
            // re-get the post processing volume, in case we have different ones in different scenes (like the bedroom)
            InitializePostProcessing();
        }
        
        

        private void Update()
        {
            // Smoothly lerp blur and chromatic aberration values
            _currentBlurIntensity = Mathf.Lerp(_currentBlurIntensity, _targetBlurIntensity, Time.deltaTime * transitionSpeed);
            _currentFocusDistance = Mathf.Lerp(_currentFocusDistance, _targetFocusDistance, Time.deltaTime * transitionSpeed);
            _currentChromaticAberration = Mathf.Lerp(_currentChromaticAberration, _targetChromaticAberration, Time.deltaTime * transitionSpeed);

            // Apply blur or disable depth of field
            if (_currentBlurIntensity > 0.01f)
            {
                ApplyBlurEffect(_currentBlurIntensity, _currentFocusDistance);
            }
            else
            {
                foreach (DepthOfField dof in _depthOfFields)
                {
                    if (dof != null && dof.active) {dof.active = false;}
                }
            }

            // Apply chromatic aberration or reset it
            if (_currentChromaticAberration > 0.01f)
            {
                SetChromaticAberrationIntensity(_currentChromaticAberration);
            }
            else
            {
                foreach (ChromaticAberration ca in _chromaticAberrations)
                {
                    if (ca != null){ ca.intensity.Override(0f);}
                }
            }
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
            // Immediately reset all values (no lerping)
            SetCustomVisualEffects(blurIntensity:0f, focusDistance:10f, chromaticAberration:0f, newTransitionSpeed:5f);

            // Immediately disable effects
            foreach (DepthOfField dof in _depthOfFields)
            {
                if (dof != null){ dof.active = false;}
            }

            foreach (ChromaticAberration ca in _chromaticAberrations)
            {
                if (ca != null){ ca.intensity.Override(0f);}
            }

            transitionSpeed = 2f;
        }

        private void HandleMildlyAnxiousEffects()
        {
            // unique effects
            SetCustomVisualEffects(blurIntensity:0f, focusDistance:10f, chromaticAberration:1f, newTransitionSpeed:5f);
            
        }

        private void HandleModeratelyAnxiousEffects()
        {
            // unique effects
            SetCustomVisualEffects(blurIntensity:0f, focusDistance:10f, chromaticAberration:2f, newTransitionSpeed:5f);
        }

        private void HandleSeverelyAnxiousEffects()
        {
            // unique effects
            SetCustomVisualEffects(blurIntensity:0f, focusDistance:10f, chromaticAberration:3f, newTransitionSpeed:5f);
        }

        private void HandlePanicEffects()
        {
            // unique effects
            SetCustomVisualEffects(blurIntensity:0f, focusDistance:10f, chromaticAberration:5f, newTransitionSpeed:5f);
        }

        private void HandleMildlySleepDeprivedEffects()
        {
            _activeSwayCoroutine = StartCoroutine(TiredSwayCoroutine(mildlyTiredSwaySettings));
            // Add blur effect
            SetCustomVisualEffects(blurIntensity: 10f, focusDistance: 3f, chromaticAberration: 1f, newTransitionSpeed: 0.5f);
        }

        private void HandleModeratelySleepDeprivedEffects()
        {
            _activeSwayCoroutine = StartCoroutine(TiredSwayCoroutine(moderatelyTiredSwaySettings));
            SetCustomVisualEffects(blurIntensity: 15f, focusDistance: 2f, chromaticAberration: 2f, newTransitionSpeed: 0.5f);
        }

        private void HandleSeverelySleepDeprivedEffects()
        {
            _activeSwayCoroutine = StartCoroutine(TiredSwayCoroutine(severelyTiredSwaySettings));
            SetCustomVisualEffects(blurIntensity: 20f, focusDistance: 1f, chromaticAberration: 3f, newTransitionSpeed: 0.5f);
        }


        private void HandleExhaustedEffects()
        {
            _activeSwayCoroutine = StartCoroutine(TiredSwayCoroutine(exhaustedTiredSwaySettings));
            SetCustomVisualEffects(blurIntensity: 25f, focusDistance: 0.5f, chromaticAberration: 4f, newTransitionSpeed: 0.5f);
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
            if (_postProcessVolumes.Count == 0 || _depthOfFields.Count == 0) { return; }

            foreach (DepthOfField dof in _depthOfFields)
            {
                if (dof == null) continue;

                dof.mode.value = DepthOfFieldMode.Bokeh;
                dof.focusDistance.value = focusDistance;
                dof.aperture.value = Mathf.Lerp(32f, 0.1f, blurIntensity);
                dof.focalLength.value = 50f;
                dof.active = true;
            }
        }

        private void SetChromaticAberrationIntensity(float amount)
        {
            if (_postProcessVolumes.Count == 0 || _chromaticAberrations.Count == 0)
            {
                Debug.LogWarning("Cannot set chromatic aberration intensity: PostProcessVolumes or ChromaticAberration effects are missing.");
                return;
            }

            //Debug.Log($"Setting chromatic aberration intensity to {amount} on {_chromaticAberrations.Count} volume(s)");

            foreach (ChromaticAberration ca in _chromaticAberrations)
            {
                if (ca == null) continue;
                ca.intensity.Override(amount);
            }
        }
        
        #region Utility Functions

        private void TrySetPostProcessing()
        {
            _postProcessVolumes.Clear();

            Volume[] foundVolumes = FindObjectsOfType<Volume>();

            if (foundVolumes.Length > 0)
            {
                _postProcessVolumes.AddRange(foundVolumes);
            }

            if (_postProcessVolumes.Count == 0)
            {
                Volume newVolume = gameObject.AddComponent<Volume>();
                newVolume.isGlobal = true;
                newVolume.priority = 1;
                newVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
                _postProcessVolumes.Add(newVolume);
            }
        }

        private void TrySetChromaticAberration()
        {
            _chromaticAberrations.Clear();

            foreach (Volume volume in _postProcessVolumes)
            {
                if (volume?.profile == null) continue;

                if (!volume.profile.TryGet(out ChromaticAberration ca))
                {
                    ca = volume.profile.Add<ChromaticAberration>(true);
                }
                _chromaticAberrations.Add(ca);
            }
        }

        private void TrySetFieldOfDepth()
        {
            _depthOfFields.Clear();

            foreach (Volume volume in _postProcessVolumes)
            {
                if (volume?.profile == null) continue;

                if (!volume.profile.TryGet(out DepthOfField dof))
                {
                    dof = volume.profile.Add<DepthOfField>(true);
                }
                _depthOfFields.Add(dof);
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