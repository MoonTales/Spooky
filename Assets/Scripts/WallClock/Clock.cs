using System;
using System.Collections;
using Managers;
using Player;
using Unity.Cinemachine;
using UnityEngine;
using Types = System.Types;
using Interaction;

public class Clock : MonoBehaviour
{
    
    [SerializeField] private SceneField sceneName;
    [SerializeField] private AudioManager.SfxId tickSfxId = AudioManager.SfxId.ClockTick;


    //public float rotateSpeed;
    public GameObject minHand;
    public GameObject hourHand;
    private float hourHandDegPerSec;
    private float minuteHandDegPerSec;
    //private bool _isInspecting;
    private Types.GameState _currentGameState;
    private int _currentAct = 1;
    [SerializeField] private float timeToExit = 600f;
    [SerializeField] private float FastForwardSpeed = 3f;
    [SerializeField] public float elapsedTime;
    [SerializeField] private float ClockSpeed;
    [SerializeField] private float damagePerTick;

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
        AudioManager.Instance?.PlaySfx(tickSfxId, transform);
        StartCoroutine(Timer());
        StartCoroutine(ClockTick());

    }

    IEnumerator Timer()
    {
        while (elapsedTime < 700)
        {
            yield return new WaitForSeconds(1f);
            if (_currentGameState != Types.GameState.Paused)
            {
                if (_currentGameState == Types.GameState.Inspecting)
                {
                    elapsedTime = elapsedTime + FastForwardSpeed;
                    EventBroadcaster.Broadcast_OnPlayerDamaged(damagePerTick*FastForwardSpeed);
                }
                else
                {
                    elapsedTime++;
                    EventBroadcaster.Broadcast_OnPlayerDamaged(damagePerTick);
                }
            }
        }
    }
    IEnumerator ClockTick()
    {
        
        while (elapsedTime < 700)
        {
            //_isInspecting = PlayerController.Instance.IsPlayerInspecting();
            _currentGameState = GameStateManager.Instance.GetCurrentGameState();
            if (_currentGameState != Types.GameState.Paused)
            {
                if (_currentGameState == Types.GameState.Inspecting)
                {
                    minHand.transform.Rotate(0, 0, -minuteHandDegPerSec * Time.deltaTime * FastForwardSpeed, Space.Self);
                    hourHand.transform.Rotate(0, 0, -hourHandDegPerSec * Time.deltaTime * FastForwardSpeed, Space.Self);
                }
                else
                {
                    minHand.transform.Rotate(0, 0, -minuteHandDegPerSec * Time.deltaTime, Space.Self);
                    hourHand.transform.Rotate(0, 0, -hourHandDegPerSec * Time.deltaTime, Space.Self);
                }
            }
            // Pause for 1 frame
            yield return null;
        }
        // we are good to sleep!
        //SceneSwapper.Instance.SwapScene(sceneName);
    }
}
