using System;
using Managers;
using UnityEngine;
using UnityEngine.UI;
using Types = System.Types;

namespace UI.PauseMenu
{
    public class PauseMenuController : Singleton<PauseMenuController>
    {
        public static bool paused = false;
        [SerializeField] private GameObject PauseMenuCanvas;

    
        // pause menu buttons
        private Button _continueButton;
        private Button _settingsButton;
        private Button _mainMenuButton;

    
        // Internal variables
        private Types.GameState _previousGameState;

        // Start is called before the first frame update
        void Start()
        {
            Time.timeScale = 1f;
            Button[] allButtons = GetComponentsInChildren<Button>(true);
            foreach (Button button in allButtons)
            {
                UI.UIButtonSfx.Ensure(button, enableHover: true, enableClick: true);

                if (button.name == "Continue")
                {
                    _continueButton = button;
                    _continueButton.onClick.AddListener(OnPlayerButtonClicked);
                }
                else if (button.name == "Settings")
                {
                    _settingsButton = button;
                    _settingsButton.onClick.AddListener(OnSettingsButtonClicked);
                }
                else if (button.name == "MainMenu")
                {
                    _mainMenuButton = button;
                    _mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
                }
            }
        }

        private void OnPlayerButtonClicked()
        {
            Play();
        }
        private void OnSettingsButtonClicked()
        {
            // hide these current settings
            PauseMenuCanvas.SetActive(false);
            SettingsController.Instance.OpenPauseSettings();
        }
        private void OnMainMenuButtonClicked()
        {
            // since we are returning to the main menu, we need to adjust time scale back to normal
            // this should probably becoem a function since we need to reuse it
            Time.timeScale = 1f;
            paused = false;
            ShowMenu(false);
            SettingsController.Instance.CloseSettings();
            // we also need to "reset" the cached paused state, and set it to cutscene, cause we know the game will always
            _previousGameState = Types.GameState.Cutscene;
            // resume into a cutscene from the main menu
            EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.MainMenu);
            SceneSwapper.Instance.SwapScene("MainMenu");

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
            SettingsController.Instance.CloseSettings();
        }

        public void ShowMenu(bool show)
        {
            if (PauseMenuCanvas != null){PauseMenuCanvas.SetActive(show);}
        }

    }
}
