using System;
using System.Collections;
using Managers;
using Player;
using Unity.Cinemachine;
using UnityEngine;
using Types = System.Types;
using Interaction;

public class Clock : EventSubscriberBase
{
    
    [SerializeField] private SceneField sceneName;


    //public float rotateSpeed;
    public GameObject minHand;
    public GameObject hourHand;
    private float hourHandDegPerSec;
    private float minuteHandDegPerSec;
    //private bool _isInspecting;
    private Types.GameState _currentGameState;
    private int _currentAct = 1;
    private float inspectTimeElapsed;
    private bool isInspecting = false;

    [SerializeField] private float timeToExit = 600f;
    [SerializeField] private float FastForwardSpeed = 20f;
    [SerializeField] public float elapsedTime;
    [SerializeField] private float ClockSpeed;
    [SerializeField] private float damagePerTick;
    [SerializeField] private float alottedInspectTime = 100; // 50 == 1 hr

    private bool _cachedKillPlayer = false;

    void Start()
    {
        // Initialize GameState
        _currentGameState = GameStateManager.Instance.GetCurrentGameState();
        _currentAct = GameStateManager.Instance.GetCurrentWorldClockHour();

        /*
        Deprecated time calculation for 14 hrs in 10 minutes
        14 hours = 14 * 30° = 420°
        10 minutes = 600 seconds // 8.4° per second minute hand   // 0.7° per second hour hand

        Time calculation for 10 hrs in 10 minutes
        1 minute = 6° per second, 1 hour = 6/12 = 0.5° per second

        New Time calculation for 12 hours in 10 minutes
        12 hours / 10 minutes  
        = (12 * 60) minutes / 10 minutes  
        = 720 minutes / 10 minutes  
        = 72× faster than normal
        and a normal clock is is 0.1° per second so 0.1°x72 = 7.2° per second minute hand, 7.2°/12 = 0.6° per second hour hand 
        */
        
        // we only wanna do any of this if we are NOT in act 4
        if (_currentAct >= 4) { return; }

        ClockSpeed = 7.2f;
        damagePerTick = 100/timeToExit;  // Normalized damage per second to player sanity
        minuteHandDegPerSec = ClockSpeed; // 7.2° per second for minute hand
        hourHandDegPerSec = minuteHandDegPerSec / 12; // 0.6° per second for hour hand
        AudioManager.Instance?.StartBedroomWallClock(transform);
        StartCoroutine(Timer());
        StartCoroutine(ClockTick());
        StartCoroutine(InspectCheck());

    }

    IEnumerator InspectCheck()
    {
        while (elapsedTime < 700)
        {
            // Pause for one frame
            yield return null;
            // Check if player is already inspecting when they inspect and if not, start a timer to lock the clock after 2hrs
            if (!isInspecting && _currentGameState == Types.GameState.Inspecting)
            {
                isInspecting = true;
                StartCoroutine(InspectTimer());
            }
        }
    }

    IEnumerator InspectTimer()
    {
        inspectTimeElapsed = 0;
        while (_currentGameState == Types.GameState.Inspecting)
        {
            // Pause for one second
            yield return new WaitForSeconds(1f);

            // Increment timer
            if (_currentGameState != Types.GameState.Paused && inspectTimeElapsed <= alottedInspectTime)
            {
                inspectTimeElapsed = inspectTimeElapsed + FastForwardSpeed;
            }
        }
        isInspecting = false;
    }

    IEnumerator Timer()
    {
        while (elapsedTime < 700)
        {
            // Pause for one second
            yield return new WaitForSeconds(1f);

            // Have we read the notes?
            if  (elapsedTime >= 450 &&
                GameStateManager.Instance.GetCurrentWorldClockHour() == 1 &&
                (!LetterManager.Instance.GetHasReadAct1ResearcherLetter() ||
                !LetterManager.Instance.GetHasReadAct1FriendLetter()) )
            {
                continue;
            }
            if  (elapsedTime >= 450 &&
                GameStateManager.Instance.GetCurrentWorldClockHour() == 2 &&
                (!LetterManager.Instance.GetHasReadAct2ResearcherLetter() ||
                !LetterManager.Instance.GetHasReadAct2FriendLetter()) )
            {
                continue;
            }
                if  (elapsedTime >= 450 &&
                GameStateManager.Instance.GetCurrentWorldClockHour() == 3 &&
                (!LetterManager.Instance.GetHasReadAct3ResearcherLetter() ||
                !LetterManager.Instance.GetHasReadAct3FriendLetter()) )
            {
                continue;
            }

            // Increment timer. If inspecting, increment * fast forward speed
            if (_currentGameState != Types.GameState.Paused)
            {
                if (isInspecting && inspectTimeElapsed <= alottedInspectTime)
                {
                    elapsedTime = elapsedTime + FastForwardSpeed;
                    float health = PlayerStats.Instance.GetPlayerStats().GetCurrentMentalHealth();
                    float damageToPlayer = damagePerTick*FastForwardSpeed;
                    if (health - damageToPlayer <= 0)
                    {
                        // we need to "pause the clock" here, and then wait for the player to finish inpsecting
                        _cachedKillPlayer = true;
                    }
                    else
                    {
                        EventBroadcaster.Broadcast_OnPlayerDamaged(damagePerTick*FastForwardSpeed);
                    }
                    
                }
                /*
                else
                {
                    elapsedTime++;
                    EventBroadcaster.Broadcast_OnPlayerDamaged(damagePerTick);
                }
                */
            }
        }
    }
    IEnumerator ClockTick()
    {
        
        while (elapsedTime < 700)
        {
            // Pause for 1 frame
            yield return null;

            // Have we read the notes?
            if  (elapsedTime >= 450 &&
                GameStateManager.Instance.GetCurrentWorldClockHour() == 1 &&
                (!LetterManager.Instance.GetHasReadAct1ResearcherLetter() ||
                !LetterManager.Instance.GetHasReadAct1FriendLetter()) )
            {
                continue;
            }
            if  (elapsedTime >= 450 &&
                GameStateManager.Instance.GetCurrentWorldClockHour() == 2 &&
                (!LetterManager.Instance.GetHasReadAct2ResearcherLetter() ||
                !LetterManager.Instance.GetHasReadAct2FriendLetter()) )
            {
                continue;
            }
                if  (elapsedTime >= 450 &&
                GameStateManager.Instance.GetCurrentWorldClockHour() == 3 &&
                (!LetterManager.Instance.GetHasReadAct3ResearcherLetter() ||
                !LetterManager.Instance.GetHasReadAct3FriendLetter()) )
            {
                continue;
            }

            //_isInspecting = PlayerController.Instance.IsPlayerInspecting();
            // Turn clock hand. If fast forwarding, turn it * fast forward speed
            _currentGameState = GameStateManager.Instance.GetCurrentGameState();
            if (_currentGameState != Types.GameState.Paused)
            {
                if (isInspecting && inspectTimeElapsed <= alottedInspectTime)
                {
                    minHand.transform.Rotate(0, 0, -minuteHandDegPerSec * Time.deltaTime * FastForwardSpeed, Space.Self);
                    hourHand.transform.Rotate(0, 0, -hourHandDegPerSec * Time.deltaTime * FastForwardSpeed, Space.Self);
                }
                /*
                else
                {
                    minHand.transform.Rotate(0, 0, -minuteHandDegPerSec * Time.deltaTime, Space.Self);
                    hourHand.transform.Rotate(0, 0, -hourHandDegPerSec * Time.deltaTime, Space.Self);
                }
                */
            }
            
        }
        // we are good to sleep!
        //SceneSwapper.Instance.SwapScene(sceneName);
    }
    
    
    // Stuff that was needed to fix this clock ---
    protected override void OnGameStateChanged(Types.GameState newState)
    {
        if (newState == Types.GameState.Gameplay && _cachedKillPlayer)
        {
            _cachedKillPlayer = false;
            if (GameStateManager.Instance.GetCurrentWorldLocation() == Types.WorldLocation.Bedroom)
            {
                EventBroadcaster.Broadcast_OnPlayerDamaged(PlayerStats.Instance.GetPlayerStats().GetCurrentMentalHealth());
            }
        }
    }
}
