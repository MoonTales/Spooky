using System;
using UnityEngine;
using Types = System.Types;

public class CursorController : EventSubscriberBase
{
    
    
    protected override void OnGameStateChanged(Types.GameState newstate)
    {
        switch (newstate)
        {
            case Types.GameState.Gameplay:
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;
            case Types.GameState.Cutscene:
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;
            case Types.GameState.MainMenu:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
            case Types.GameState.Inspecting:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }
    }
}
