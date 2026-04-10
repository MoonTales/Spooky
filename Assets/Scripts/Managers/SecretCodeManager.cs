using System;
using UnityEngine;

public class SecretCodeManager : Singleton<SecretCodeManager>
{
    private const int RequiredPresses = 5;
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
        Debug.Log("Secret Unlocked!");
    }
    
}
