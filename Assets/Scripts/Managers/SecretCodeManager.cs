using System;
using UnityEngine;
using Types = System.Types;

public class SecretCodeManager : Singleton<SecretCodeManager>
{
    private const int RequiredPresses = 10;
    private const float TimeWindow = 2f;

    private int _pressCount = 0;
    private float _windowStartTime = 0f;

    public void ButtonPressed()
    {
        Debug.Log("Button Pressed");

        if (Time.time - _windowStartTime > TimeWindow)
        {
            _pressCount = 0;
            _windowStartTime = Time.time;
        }

        _pressCount++;
        if (_pressCount >= RequiredPresses)
        {
            _pressCount = 0;
            _windowStartTime = 0f;
            SecretUnlocked();
        }
    }

    private void SecretUnlocked()
    {
        Types.NotificationData data = new(
            duration: 1, 
            messageKey: new TextKey { place = "prompt", id = "cant_sleep" },
            messageOverride: "Beep beep boop boop beeeeeeeeeeeeeeeeeeeeeeep! :3",
            shouldOnlyShowOnce:true
        );
        data.Send();
    }
    
}
