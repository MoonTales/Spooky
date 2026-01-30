using System;
using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using Types = System.Types;

namespace Player.Camera
{
    public class CameraMentalStateEffects : EventSubscriberBase
    {
        private CinemachineCamera _cinemachineCamera;
        private CinemachinePanTilt _panTilt;
        
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

        private void HandleNormalStateEffects() { }
        private void HandleMildlyAnxiousEffects() { }
        private void HandleModeratelyAnxiousEffects() { }
        private void HandleSeverelyAnxiousEffects() { }
        private void HandlePanicEffects() { }

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
        }

        private void HandleExhaustedEffects()
        {
            _activeSwayCoroutine = StartCoroutine(TiredSwayCoroutine(
                intensity: 10.3f, 
                speed: 0.60f, 
                verticalBias: 0.75f,
                verticalSin: false,
                horizontalSin: true,
                verticalFrequency: 0.9f,
                horizontalFrequency: 1f
            ));
        }
        private void HandleBreakdownEffects() { }

        private IEnumerator TiredSwayCoroutine(float intensity, float speed, float verticalBias, bool verticalSin, bool horizontalSin, float verticalFrequency = 0.7f, float horizontalFrequency = 1f)
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
                } else
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
                
                

                // Apply to pan/tilt as offset
                
                

                yield return null;
            }
        }
    }
}