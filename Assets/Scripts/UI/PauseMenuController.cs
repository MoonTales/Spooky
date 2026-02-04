using System;
using Player;
using TMPro;
using UnityEngine;
using Types = System.Types;
using Inspection;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Managers;

public class PauseMenuController : MonoBehaviour
{
    public static bool paused = false;
    [SerializeField] private GameObject PauseMenuCanvas;

    [SerializeField] private GameObject SettingsCanvas;

    [SerializeField] private GameObject SettingsSliders;
    
    // Internal variables
    private Types.GameState _previousGameState;

    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 1f;
    }

    // Update is called once per frame
    void Update()
    {
        
        // we should not be able to pause while on the main menu
        if (GameStateManager.Instance.GetCurrentGameState() == Types.GameState.MainMenu) { return; }
        // Added TAB as an option, cause sometimes ESC has weird behaviors
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
        {
            if (paused)
            {
                Play();
            }
            else
            {
                Stop();
            }
        }
    }

    void Stop()
    {
        
        // before anything, cache the previous game state
        _previousGameState = GameStateManager.Instance.GetCurrentGameState();
        EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Paused);
        // Removing these, cause the cursor itself handles this logic internall in the Cursor class
        //Cursor.lockState = CursorLockMode.None;
        //Cursor.visible = true;
        Time.timeScale = 0f;
        paused = true;
        PauseMenuCanvas.SetActive(true);
        
    }

    public void Play()
    {
        // we load in whatever our cached state was before we paused
        EventBroadcaster.Broadcast_GameStateChanged(_previousGameState);
        // again, these are handled automatically, since we dont know what state we are loading into
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
        Time.timeScale = 1f;
        paused = false;
        ShowMenu(false);
        SettingsCanvas.SetActive(false);
        SettingsSliders.SetActive(false);
    }

    public void ShowMenu(bool show)
        {
            if (PauseMenuCanvas != null){PauseMenuCanvas.SetActive(show);}
        }

    public void MainMenuButton()
    {
        EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.MainMenu);
        SceneSwapper.Instance.SwapScene("MainMenu");
    }
}