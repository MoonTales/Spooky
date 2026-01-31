using System;
using Player;
using TMPro;
using UnityEngine;
using Types = System.Types;
using Inspection;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PauseMenuController : MonoBehaviour
{
    public static bool paused = false;
    public GameObject PauseMenuCanvas;

    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 1f;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
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
        EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Paused);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
        paused = true;
        PauseMenuCanvas.SetActive(true);
    }

    public void Play()
    {
        EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Gameplay);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
        paused = false;
        PauseMenuCanvas.SetActive(false);
    }

    public void MainMenuButton()
    {
        EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.MainMenu);
        SceneSwapper.Instance.SwapScene("MainMenu");
    }
}