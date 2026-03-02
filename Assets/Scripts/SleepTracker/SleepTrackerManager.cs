using System;
using Types = System.Types;
using Managers;
using UnityEngine;


/// <summary>
/// The sleep tracker will be a singleton class, so it can persist across all the scenes, for the different styles of wakeup.
///
/// Audio is loaded from Resources and faded in/out via coroutines.
///
/// This will also look for an object in the scene with the SleepTracker tag
/// </summary>
public class SleepTrackerManager : Singleton<SleepTrackerManager>
{
    
    public struct SleepTrackerSaveData
    {
        public bool isGoodWakeup;
        public bool isSleepTrackerActive;
    }
    
    // Internal State
    private bool _isGoodWakeup  = false; public bool GetIsGoodWakeup() { return _isGoodWakeup; }
    private bool _isSleepTrackerActive = false; public bool GetIsSleepTrackerActive() { return _isSleepTrackerActive; }
    private Transform _sleepTrackerSourceTransform;
    
    // Subscriptions
    protected override void RegisterSubscriptions()
    {
        base.RegisterSubscriptions();
        TrackSubscription(()=> EventBroadcaster.OnWorldLocationChangedEvent += OnWorldLocationChanged,
            () => EventBroadcaster.OnWorldLocationChangedEvent -= OnWorldLocationChanged);
    }

    private void OnWorldLocationChanged(Types.WorldLocation worldLocation)
    {
        if (worldLocation != Types.WorldLocation.Bedroom)
        {
            _sleepTrackerSourceTransform = null;
            return;
        }

        // this will be called whenever the world location changed, and we will pull if it was a good or bad wakeup
        // ensure we are in the bedroom, as thats the only location of the sleeptracker is present
        //DebugUtils.Log(_isGoodWakeup ? "Good Wakeup!" : "Bad Wakeup!");
        TurnSleepTrackerOn();
    }

    // edge case for returning to the main menu
    protected override void OnGameRestarted()
    {
        TurnSleepTrackerOff();
    }
    // -------------------------------------------------------------------------
    // IInteractable
    // -------------------------------------------------------------------------
    public bool CanInteract(Interactor interactor)
    {
        // Current design: player can only turn the tracker OFF (not back ON manually).
        // Keep the active-state gate so inactive tracker does not present an interaction prompt.
        return GameStateManager.Instance != null
               && GameStateManager.Instance.GetCurrentGameState() == Types.GameState.Gameplay
               && GameStateManager.Instance.GetCurrentWorldLocation() == Types.WorldLocation.Bedroom
               && _isSleepTrackerActive;
    }

    public void Interact(Interactor interactor)
    {
        if (_isSleepTrackerActive)
        {
            TurnSleepTrackerOff();
            return;
        }

        // Intentionally disabled by design direction:
        // the player should not be able to turn the sleep tracker back ON.
        // Keep this branch commented for future design pivots.
        // TurnSleepTrackerOn();
    }
    /// <summary>
    /// Instantly stop all SleepTracker audio.
    /// </summary>
    public void TurnSleepTrackerOff()
    {
        _isSleepTrackerActive = false;
        BroadcastSleepTrackerAudioState();
    }

    /// <summary>
    /// Instantly Start all SleepTracker Audio
    /// </summary>
    public void TurnSleepTrackerOn()
    {
        _isSleepTrackerActive = true;
        BroadcastSleepTrackerAudioState();
    }

    public void SetIsGoodWakeup(bool value)
    {
        _isGoodWakeup = value;
        // Only broadcast variant changes when the tracker is already active.
        // This preserves the selected wakeup type without prematurely starting audio.
        if (_isSleepTrackerActive)
        {
            BroadcastSleepTrackerAudioState();
        }
    }

    public void RegisterSleepTrackerSourceTransform(Transform sourceTransform)
    {
        _sleepTrackerSourceTransform = sourceTransform;
        if (_isSleepTrackerActive)
        {
            BroadcastSleepTrackerAudioState();
        }
    }

    private void BroadcastSleepTrackerAudioState()
    {
        if (_sleepTrackerSourceTransform == null)
        {
            SleepTracker sleepTracker = FindObjectOfType<SleepTracker>();
            if (sleepTracker != null)
            {
                _sleepTrackerSourceTransform = sleepTracker.transform;
            }
        }

        EventBroadcaster.Broadcast_OnSleepTrackerAudioStateChanged(
            isActive: _isSleepTrackerActive,
            isGoodWakeup: _isGoodWakeup,
            sourceTransform: _sleepTrackerSourceTransform);
    }

}
