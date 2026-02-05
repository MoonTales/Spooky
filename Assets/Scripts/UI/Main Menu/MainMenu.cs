using UnityEngine;
using System;
using Types = System.Types;
using UnityEngine.UI;


public class MainMenu : MonoBehaviour
{
    
    // Internal variables to link to all of the buttons on the main menu
    private Button _playButton;
    private Button _settingsButton;
    private Button _quitButton;
    private void Start()
    {
        // at the start of the game, get access to our buttons, and add listeners to them
        // the children may be in the children
        Button[] allButtons = GetComponentsInChildren<Button>();
        foreach (Button button in allButtons)
        { 
            if (button.name == "Play")
            {
                _playButton = button;
                _playButton.onClick.AddListener(OnPlayerButtonClicked);
            }
            else if (button.name == "Settings")
            {
                 _settingsButton = button;
                 _settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            }
            else if (button.name == "Quit")
            {
                _quitButton = button;
                _quitButton.onClick.AddListener(OnQuitButtonClicked);
            }
        }
    }
    // Button connections
    private void OnPlayerButtonClicked()
    {
        EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Gameplay);
        SceneSwapper.Instance.SwapScene("Bedroom");
    }

    private void OnSettingsButtonClicked()
    {
        SettingsController.Instance.OpenSettings();
    }

    private void OnQuitButtonClicked()
    {
        Application.Quit();
    }
}
