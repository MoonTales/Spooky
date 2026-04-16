using System;
using System.Collections;
using System.Collections.Generic;
using Horror_Event;
using UnityEngine;

public class HorrorEventManager : Singleton<HorrorEventManager>, ISaveSystemInterface<HorrorEventManager.HorrorEventSaveData>
{
    public struct HorrorEventSaveData
    {
        public List<string> triggeredEventIds;
    }

    private HashSet<string> _activeNotifications = new HashSet<string>();

    
    protected override void RegisterSubscriptions()
    {
        base.RegisterSubscriptions();
        // Listen for the notification broadcast
        TrackSubscription(() => EventBroadcaster.OnHorrorEventTriggered += OnHorrorEventTriggered,
            () => EventBroadcaster.OnHorrorEventTriggered -= OnHorrorEventTriggered);
    }

    private void OnHorrorEventTriggered(HorrorEvent data)
    {
        string notificationKey = data.GetHorrorEventId();
        if (!_activeNotifications.Add(notificationKey)) { return; } 
        DebugUtils.LogSuccess("HorrorEventManager registered a new horror event trigger: " + notificationKey);
    }
    
    public bool CheckIfHorrorEventTriggered(HorrorEvent horrorEvent)
    {
        return _activeNotifications.Contains(horrorEvent.GetHorrorEventId());
    }

    public string SaveId => "HorrorEventManager";
    public HorrorEventSaveData OnSave()
    {
        return new HorrorEventSaveData { triggeredEventIds = new List<string>(_activeNotifications) };

    }

    public void OnLoad(HorrorEventSaveData data)
    {
        _activeNotifications = new HashSet<string>(data.triggeredEventIds);
    }
}
