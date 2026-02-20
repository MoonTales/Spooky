using System;
using System.Collections;
using Types = System.Types;
using Managers;
using UnityEngine;


/// <summary>
/// The sleep tracker will be a singleton class, so it can persist across all the scenes, for the different styles of wakeup.
///
/// Audio is loaded from Resources and faded in/out via coroutines.
/// TODO: Connect to FMOD when ready.
/// </summary>
public class SleepTracker : Singleton<SleepTracker>, IInteractable
{


    // -------------------------------------------------------------------------
    // Internal State
    // -------------------------------------------------------------------------

    private bool _isGoodWakeup  = false; public void SetIsGoodWakeup(bool value) { _isGoodWakeup = value; }
    private bool _isSleepTrackerActive = false;
    
    private Coroutine   _fadeCoroutine;


    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------
    protected void Awake()
    {
        base.Awake();
        
    }

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
        if (_isGoodWakeup)
        {
            // HANDLE GOOD WAKEUP
            DebugUtils.Log("Good Wakeup!");
            // we want to fade in the alarm over some seconds... which will probably be called from the prior scene with
            // SleppTracker.Instance.StartSleepTrackerFadeIn(timeToFadeIn);
        }
        else
        {
            // HANDLE BAD WAKEUP
            DebugUtils.Log("Bad Wakeup!");
            // We want to just instantly Turn on the alarm, with no fade in
            TurnSleepTrackerOn();
        }
    }

    // -------------------------------------------------------------------------
    // IInteractable
    // -------------------------------------------------------------------------

    [Header("Text Keys (CSV row pointers)")]
    [SerializeField] private TextKey promptTextKey;
    public TextKey PromptKey => _isSleepTrackerActive? promptTextKey: TextKey.Empty; // only show the prompt if the sleep tracker is active, otherwise we will just show nothing when we interact with it

    public bool CanInteract(Interactor interactor)
    {
        // Interactable only during Gameplay while the alarm is running.
        return GameStateManager.Instance.GetCurrentGameState() == Types.GameState.Gameplay && _isSleepTrackerActive;
    }

    public void Interact(Interactor interactor)
    {
        TurnSleepTrackerOff();
    }
    

    /// <summary>
    /// Start Fading in the alarm sounds
    /// </summary>
    public void StartSleepTrackerFadeIn(float timeToFadeIn)
    {
        // FOR NOW, WE WILL JUST INSTANTLY TURN ON THE ALARM, BUT THIS WILL BE CHANGED ONCE WE HAVE THE AUDIO IMPLEMENTED
        DebugUtils.Log("Starting Sleep Tracker Fade In");
        TurnSleepTrackerOn();
    }

    /// <summary>
    /// Start Fading out the alarm sounds
    /// </summary>
    public void StartSleepTrackerFadeOut(float timeToFadeOut)
    {
        
    }

    /// <summary>
    /// Instantly stop all SleepTracker audio.
    /// </summary>
    public void TurnSleepTrackerOff()
    {
        DebugUtils.Log("Turning Sleep Tracker Off");
        AudioManager.Instance.PlaySfx(AudioManager.SfxId.Flashlight, transform);
        _isSleepTrackerActive = false;
        
    }

    /// <summary>
    /// Instantly Start all SleepTracker Audio
    /// </summary>
    public void TurnSleepTrackerOn()
    {
        DebugUtils.Log("Turning Sleep Tracker On");
        _isSleepTrackerActive = true;
    }
    
    // TEMPORARY !!!
    float _timer = 0;
    public void Update()
    {
        // as long as we are active, loop a sound every 1 second
        if (_isSleepTrackerActive)
        {
            //TODO: Replace with actual alarm sound, and remove this temporary code
            _timer += Time.deltaTime;
            if (_timer >= 0.25f)
            {
                _timer = 0f;
                AudioManager.Instance.PlaySfx(AudioManager.SfxId.Flashlight, transform);
            }
        }
    }
    

}