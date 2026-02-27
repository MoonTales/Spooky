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
                    _playButton.enabled = true;
                }
                else if (button.name == "Settings")
                {
                    _settingsButton = button;
                    _settingsButton.onClick.AddListener(OnSettingsButtonClicked);
                    _settingsButton.enabled = true;
                }
                else if (button.name == "Quit")
                {
                    _quitButton = button;
                    _quitButton.onClick.AddListener(OnQuitButtonClicked);
                    _quitButton.enabled = true;
                }
            }
        }
        // Button connections
        private void OnPlayerButtonClicked()
        {
            // we will just disable all of the buttons, so that nothing can be clicked while we fade in
            _playButton.enabled = false;
            _playButton.interactable = false;
            _settingsButton.enabled = false;
            _settingsButton.interactable = false;
            _quitButton.enabled = false;
            _quitButton.interactable = false;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.TriggerMainMenuMusicTransition();
            }
            new Types.ScreenFadeData(3f, 1f, 3f, () => Debug.Log(""),SwapToGame).Send();
            
        }

        private void SwapToGame()
        {
            // close the main menu canvas
            mainMenuCanvas.SetActive(false);
            
            /// TEMP SAVE HANDLING ///
            // if the player has save data, we will want to load their data, and send them to the bedroom
            // if the player has no save data, we will send them to the tutorial like normal
            if (SaveSystem.Instance.DoesSaveGameExist())
            {
                SaveSystem.Instance.LoadGame();
            }
            else
            {
                //No save data, start like normal!
                EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Gameplay);
                SceneSwapper.Instance.SwapScene("Tutorial");
            }
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
