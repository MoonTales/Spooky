using System;
using System.Collections;
using Managers;
using Player;
using Unity.Cinemachine;
using UnityEngine;
using Types = System.Types;
public class ClockScript : MonoBehaviour
{
    private bool _isInspecting;
    public float ClockSpeed;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(Clock());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpeedUp()
    {
        //ClockSpeed = ClockSpeed * 10
    }


    private IEnumerator Clock()
    {


        _isInspecting = PlayerController.Instance.IsPlayerInspecting();

        yield return null;
    }
}



// public GameEventSO speedUpClock;

// public void Trigger() => speedUpClock.Raise();