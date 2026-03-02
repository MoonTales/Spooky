using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "GameEventSO", menuName = "Scriptable Objects/Events/GameEventSO")]
public class GameEventSO : ScriptableObject
{
    private List<GameEventListener> listeners = new List<GameEventListener>();

    public void RegisterListener(GameEventListener listener)
    {
        listeners.Add(listener);
    }
    public void UnregisterListener(GameEventListener listener)
    {
        listeners.Remove(listener);
    }
    public void Raise()
    {
        for (int i = listeners.Count - 1; i >= 0; i--)
        {
        listeners[i].OnEventRaised();
        }
    }
}