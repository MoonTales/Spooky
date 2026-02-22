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


    //public float rotateSpeed;
    public GameObject minHand;
    public GameObject hourHand;
    private float hourHandDegPerSec;
    private float minuteHandDegPerSec;
    //private bool _isInspecting;
    private Types.GameState _currentGameState;
    [SerializeField] private float timeToExit = 660f;
    [SerializeField] private float FastForwardSpeed = 3f;
    [SerializeField] private float elapsedTime;
    [SerializeField] private float ClockSpeed;
    [SerializeField] private float damagePerTick;

    void Start()
    {
        // Initialize GameState
        _currentGameState = GameStateManager.Instance.GetCurrentGameState();
        // Deprecated time calculation for 14 hrs.
        // 14 hours = 14 * 30° = 420°
        // 10 minutes = 600 seconds // 8.4° per second minute hand   // 0.7° per second hour hand
        // New time calculation for 12 hrs
        // 1 minute = 6° per second, 1 hour = 6/12 = 0.5° per second
        ClockSpeed = 6f;
        damagePerTick = 100/timeToExit;  // Normalized damage per second to player sanity
        minuteHandDegPerSec = ClockSpeed; // 6° per second for minute hand
        hourHandDegPerSec = minuteHandDegPerSec / 12; // 0.5° per second for hour hand
        StartCoroutine(Timer());
        StartCoroutine(ClockTick());

    }

    IEnumerator Timer()
    {
        while (elapsedTime < timeToExit)
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
        
        while (elapsedTime < timeToExit)
        {
            //_isInspecting = PlayerController.Instance.IsPlayerInspecting();
            _currentGameState = GameStateManager.Instance.GetCurrentGameState();
            if (_currentGameState != Types.GameState.Paused)
            {
                if (_currentGameState == Types.GameState.Inspecting)
                {
                    minHand.transform.Rotate(0, 0, -minuteHandDegPerSec * Time.deltaTime * FastForwardSpeed, Space.World);
                    hourHand.transform.Rotate(0, 0, -hourHandDegPerSec * Time.deltaTime * FastForwardSpeed, Space.World);
                }
                else
                {
                    minHand.transform.Rotate(0, 0, -minuteHandDegPerSec * Time.deltaTime, Space.World);
                    hourHand.transform.Rotate(0, 0, -hourHandDegPerSec * Time.deltaTime, Space.World);
                }
            }
            // Pause for 1 frame
            yield return null;
        }
        // we are good to sleep!
        SceneSwapper.Instance.SwapScene(sceneName);
    }
}