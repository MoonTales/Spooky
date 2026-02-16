using System;
using Managers;
using UI.PauseMenu;
using UnityEngine;
using UnityEngine.UI;
using Types = System.Types;


namespace UI.Main_Menu
{
    public class MainMenu : MonoBehaviour
    {
    
        [SerializeField] private GameObject mainMenuCanvas;
    
        // Internal variables to link to all of the buttons on the main menu
        private Button _playButton;
        private Button _settingsButton;
        private Button _quitButton;
        private void Start()
        {
            mainMenuCanvas.SetActive(true);
            // at the start of the game, get access to our buttons, and add listeners to them
            // the children may be in the children
            Button[] allButtons = GetComponentsInChildren<Button>();
            foreach (Button button in allButtons)
            { 
                UI.UIButtonSfx.Ensure(button, enableHover: true, enableClick: true);

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
            new Types.ScreenFadeData(3f, 1f, 3f,
                SwapToGame
            ).Send();
            
        }

        private void SwapToGame()
        {
            // close the main menu canvas
            mainMenuCanvas.SetActive(false);
            // Yes it snaps away, but this will be changed once the game has a fade away or anything to transition us into gameplay!
            EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Gameplay);
            SceneSwapper.Instance.SwapScene("Tutorial");
        }

        private void OnSettingsButtonClicked()
        {
            mainMenuCanvas.SetActive(false);
            SettingsController.Instance.OpenMainMenuSettings();
        }

        private void OnQuitButtonClicked()
        {
            Application.Quit();
        }
    
        public void MainMenuVisible()
        {
            mainMenuCanvas.SetActive(true);
        }

    }
}
