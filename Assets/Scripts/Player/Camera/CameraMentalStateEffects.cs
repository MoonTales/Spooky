using System;
using Unity.Cinemachine;
using Types = System.Types;

namespace Player.Camera
{
    /// <summary>
    /// Main class that will be incharge of handling and applying camera effects based on the player's mental state.
    /// Such as blurriness, shaking, color grading, etc.
    /// </summary>
    public class CameraMentalStateEffects : EventSubscriberBase
    {

        // internal variables to track the current mental state effects
        private CinemachineCamera _cinemachineCamera;
        private CinemachinePanTilt _panTilt;

        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(() => EventBroadcaster.OnPlayerHealthStateChanged += OnPlayerMentalStateChanged,
                () => EventBroadcaster.OnPlayerHealthStateChanged -= OnPlayerMentalStateChanged);
        }

        private void Start()
        {
            // initialize all of the "default" camera effects for mental states
            _cinemachineCamera = PlayerManager.Instance.GetCinemachineCamera();
            _panTilt = _cinemachineCamera.GetComponent<CinemachinePanTilt>();
        }

        private void OnPlayerMentalStateChanged(Types.PlayerMentalState newMentalState)
        {
            switch (newMentalState)
            {
                case Types.PlayerMentalState.Normal:
                    HandleNormalStateEffects();
                    break;
                case Types.PlayerMentalState.MildlyAnxious:
                    // logic for mildly anxious effects
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
                // Add cases for sleep deprivation and breakdown states as needed
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
        private void HandleMildlySleepDeprivedEffects() { }
        private void HandleModeratelySleepDeprivedEffects() { }
        private void HandleSeverelySleepDeprivedEffects() { }
        private void HandleExhaustedEffects() { }

        private void HandleBreakdownEffects() { }









    }
}
