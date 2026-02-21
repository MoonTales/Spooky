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
    public float timeToExit = 600f;
    public float FastForwardSpeed = 3f;
    private float hourHandDegPerSec;
    private float minuteHandDegPerSec;
    private float elapsedTime;
    private bool _isInspecting;
    private float ClockSpeed;

    void Start()
    {
        // 14 hours = 14 * 30° = 420°
        // 10 minutes = 600 seconds    
        ClockSpeed = 420f / 600f * 2;    
        minuteHandDegPerSec = ClockSpeed * Time.deltaTime;  // 8.4° per second
        hourHandDegPerSec = minuteHandDegPerSec / 12;   // 0.7° per second   
        StartCoroutine(Timer());
        StartCoroutine(ClockTick());

    }
    void Update()
    {
        
    }

    IEnumerator Timer()
    {
        while (elapsedTime < timeToExit)
        {
            yield return new WaitForSeconds(1f);
            if (_isInspecting)
            {
                elapsedTime = elapsedTime + FastForwardSpeed;
            }
            else
            {
                elapsedTime++;
            }
            
        }
    }
    IEnumerator ClockTick()
    {
        
        while (elapsedTime < timeToExit)
        {
            _isInspecting = PlayerController.Instance.IsPlayerInspecting();
            if (_isInspecting)
            {
                minHand.transform.Rotate(0, 0, -minuteHandDegPerSec*FastForwardSpeed, Space.World);
                hourHand.transform.Rotate(0, 0, -hourHandDegPerSec*FastForwardSpeed, Space.World);
            }
            else
            {
                minHand.transform.Rotate(0, 0, -minuteHandDegPerSec, Space.World);
                hourHand.transform.Rotate(0, 0, -hourHandDegPerSec, Space.World);
            }
            yield return null;
        }
        // we are good to sleep!
        SceneSwapper.Instance.SwapScene(sceneName);
    }
}