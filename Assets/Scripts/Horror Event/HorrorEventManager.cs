using System;
using System.Collections;
using Horror_Event;
using UnityEngine;

public class HorrorEventManager : Singleton<HorrorEventManager>, ISaveSystemInterface<HorrorEventManager.HorrorEventSaveData>
{
    public struct HorrorEventSaveData
    {
        public Hashtable activeNotifications;
    }
    private Hashtable _activeNotifications = new Hashtable();

    
    protected override void RegisterSubscriptions()
    {
        base.RegisterSubscriptions();
        // Listen for the notification broadcast
        TrackSubscription(() => EventBroadcaster.OnHorrorEventTriggered += OnHorrorEventTriggered,
            () => EventBroadcaster.OnHorrorEventTriggered -= OnHorrorEventTriggered);
    }

    private void OnHorrorEventTriggered(HorrorEvent data)
    {
        // check if this object has already been fired, and in our hashtable, if it has, we should ignore it.
        // we will key based on the message override (for now)
        // if we have a valid text key, use the TextKey.id as they key
        string notificationKey = data.GetHorrorEventId();
        
        if (_activeNotifications.ContainsKey(notificationKey)) { return; }
        _activeNotifications.Add(notificationKey, true);
        DebugUtils.LogSuccess("HorrorEventManager registered a new horror event trigger: " + notificationKey);
    }
    
    public bool CheckIfHorrorEventTriggered(HorrorEvent horrorEvent)
    {
        string notificationKey = horrorEvent.GetHorrorEventId();
        return _activeNotifications.ContainsKey(notificationKey);
    }

    public string SaveId => "HorrorEventManager";
    public HorrorEventSaveData OnSave()
    {
        return new HorrorEventSaveData()
        {
            activeNotifications = _activeNotifications
        };
    }

    public void OnLoad(HorrorEventSaveData data)
    {
        _activeNotifications = data.activeNotifications;
    }
}
