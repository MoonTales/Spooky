using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameEventListener : MonoBehaviour
{
    public GameEventSO Event;
    public UnityEvent response;

    private void OnEnable()
    {
        Event.RegisterListener(this);
    }
    public void OnDisable()
    {
        Event.UnregisterListener(this);
    }
    public void OnEventRaised()
    {
        response.Invoke();
    }
}