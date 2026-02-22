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
    // Internal State
    private bool _isGoodWakeup  = false;
    private bool _isSleepTrackerActive = false; public bool GetIsSleepTrackerActive() { return _isSleepTrackerActive; }
    
    // Subscriptions
    protected override void RegisterSubscriptions()
    {
        base.RegisterSubscriptions();
        TrackSubscription(()=> EventBroadcaster.OnWorldLocationChangedEvent += OnWorldLocationChanged,
            () => EventBroadcaster.OnWorldLocationChangedEvent -= OnWorldLocationChanged);
    }

    private void OnWorldLocationChanged(Types.WorldLocation worldLocation)
    {
        if (worldLocation != Types.WorldLocation.Bedroom) { return; }

        // this will be called whenever the world location changed, and we will pull if it was a good or bad wakeup
        // ensure we are in the bedroom, as thats the only location of the sleeptracker is present
        DebugUtils.Log(_isGoodWakeup ? "Good Wakeup!" : "Bad Wakeup!");
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
        // Allow toggling on/off while in Bedroom gameplay.
        return GameStateManager.Instance != null
               && GameStateManager.Instance.GetCurrentGameState() == Types.GameState.Gameplay
               && GameStateManager.Instance.GetCurrentWorldLocation() == Types.WorldLocation.Bedroom;
    }

    public void Interact(Interactor interactor)
    {
        if (_isSleepTrackerActive)
        {
            TurnSleepTrackerOff();
            return;
        }

        TurnSleepTrackerOn();
    }
    /// <summary>
    /// Instantly stop all SleepTracker audio.
    /// </summary>
    public void TurnSleepTrackerOff()
    {
        DebugUtils.Log("Turning Sleep Tracker Off");
        _isSleepTrackerActive = false;
        BroadcastSleepTrackerAudioState();
    }

    /// <summary>
    /// Instantly Start all SleepTracker Audio
    /// </summary>
    public void TurnSleepTrackerOn()
    {
        DebugUtils.Log("Turning Sleep Tracker On");
        _isSleepTrackerActive = true;
        BroadcastSleepTrackerAudioState();
    }

    public void SetIsGoodWakeup(bool value)
    {
        _isGoodWakeup = value;
        BroadcastSleepTrackerAudioState();
    }

    private void BroadcastSleepTrackerAudioState()
    {
        EventBroadcaster.Broadcast_OnSleepTrackerAudioStateChanged(
            isActive: _isSleepTrackerActive,
            isGoodWakeup: _isGoodWakeup,
            sourceTransform: transform);
    }

}
